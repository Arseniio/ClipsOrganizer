using ClipsOrganizer.FileUtils;
using ClipsOrganizer.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace ClipsOrganizer.ViewableControls {
    /// <summary>
    /// Логика взаимодействия для VideoControls.xaml
    /// </summary>
    public partial class VideoViewer : UserControl {
        DispatcherTimer SliderTimer;
        TimeSpan StartTime = TimeSpan.Zero;
        TimeSpan EndTime = TimeSpan.Zero;
        public event Action<TimeSpan, TimeSpan?> SliderSelectionChanged;
        public event Action<Uri> UpdateFilename;
        MainWindow Owner = null;
        public VideoViewer() {
            InitializeComponent();
            SL_volume.Value = GlobalSettings.Instance.DefaultVolumeLevel;
            #region Slider timer init
            SliderTimer = new DispatcherTimer();
            SliderTimer.Interval = TimeSpan.FromMilliseconds(100);
            SliderTimer.Tick += VideoDurationUpdate;
            #endregion 
            ViewableController.FileLoaded += ViewableController_FileLoaded;
            //this.Unloaded += VideoViewer_Unloaded;
        }

        private void VideoViewer_Unloaded(object sender, RoutedEventArgs e) {
            ViewableController.FileLoaded -= ViewableController_FileLoaded;
            SliderTimer.Stop();
        }

        private void ViewableController_FileLoaded(object sender, FileLoadedEventArgs e) {
            if (ViewableController.FileTypeDetector.DetectFileType(e.Item.Path) != SupportedFileTypes.Video) return;

            Owner = Window.GetWindow(this) as MainWindow;
            RemoveSelection();
            if (e.Item.Path != null) {
                ME_main.Source = new Uri(e.Item.Path);
                ME_main.Play();
            }
            if (Application.Current.MainWindow.OwnedWindows.Count != 0) UpdateFilename(new Uri(e.Item.Path));
        }

        #region sliders events
        private void SL_duration_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e) {
            is_dragging = false;
            ME_main.Position = TimeSpan.FromSeconds((double)SL_duration.Value);
        }

        bool is_dragging = false;
        private void SL_duration_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
            if (!is_dragging) ME_main.Position = TimeSpan.FromSeconds((double)SL_duration.Value);
            if (ME_main.NaturalDuration.HasTimeSpan)
                TB_length.Text = $"{TimeSpan.FromSeconds((double)SL_duration.Value).ToString(@"hh\:mm\:ss")} / {ME_main.NaturalDuration.TimeSpan.ToString(@"hh\:mm\:ss")}";
        }

        private void SL_duration_DragStarted(object sender, System.Windows.Controls.Primitives.DragStartedEventArgs e) {
            is_dragging = true;
        }

        private void VideoDurationUpdate(object sender, EventArgs e) {
            if (!ME_main.IsLoaded) return;
            if (!is_dragging)
                SL_duration.Value = ME_main.Position.TotalSeconds;

            if (SL_duration.IsSelectionRangeEnabled && StartTime != TimeSpan.Zero && App.Current.MainWindow.OwnedWindows.Count == 0)
                SL_duration.SelectionEnd = ME_main.Position.TotalSeconds;
        }
        #endregion

        #region player events
        private void Btn_Play_Click(object sender, RoutedEventArgs e) {
            ME_main.Play();
            Log.Update($"{IsPlaying} -> {!IsPlaying}");
            IsPlaying = true;
        }

        private void SL_volume_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
            ME_main.Volume = (double)e.NewValue;
        }

        private void ME_main_MediaOpened(object sender, RoutedEventArgs e) {
            if (ME_main.NaturalDuration.HasTimeSpan) {
                SL_duration.Maximum = ME_main.NaturalDuration.TimeSpan.TotalSeconds;
                SliderTimer.Start();
                TB_length.Text = $"{TimeSpan.FromSeconds((double)SL_duration.Value).ToString(@"hh\:mm\:ss")} / {ME_main.NaturalDuration.TimeSpan.ToString(@"hh\:mm\:ss")}";
            }
            if (GlobalSettings.Instance.AutoPlay && ME_main.NaturalDuration.HasTimeSpan) {
                if (GlobalSettings.Instance.AutoPlayOffset < ME_main.NaturalDuration.TimeSpan) {
                    ME_main.Position = GlobalSettings.Instance.AutoPlayOffset;
                }
                else if (GlobalSettings.Instance.AutoPlayOffset != TimeSpan.Zero) { Log.Update($"Автоплей выставлен на значение {GlobalSettings.Instance.AutoPlayOffset}, длинна клипа {ME_main.NaturalDuration}, невозможно применить переход, возврат к стандартному значению"); }
                Log.Update($"{IsPlaying} -> {!IsPlaying}");
            }
            else {
                ME_main.Pause();
                IsPlaying = false;
            }
        }

        private void Btn_Stop_Click(object sender, RoutedEventArgs e) {
            ME_main.Pause();
            Log.Update($"{IsPlaying} -> {!IsPlaying}");
            IsPlaying = false;
        }
        #endregion

        private void RemoveSelection() {
            StartTime = TimeSpan.Zero;
            EndTime = TimeSpan.Zero;
            SL_duration.SelectionStart = 0;
            SL_duration.SelectionEnd = 0;
            SL_duration.IsSelectionRangeEnabled = false;
        }
        bool IsPlaying = true;

        private void SwapPlaying() {
            Log.Update($"{IsPlaying} -> {!IsPlaying}");
            if (!IsPlaying) {
                IsPlaying = true;
                ME_main.Play();
            }
            else {
                IsPlaying = false;
                ME_main.Pause();
            }
        }

        public void HandleKeyStroke(KeyEventArgs e) {
            if (e.Key == Key.Left && !Keyboard.Modifiers.HasFlag(ModifierKeys.Control)) {
                var step = TimeSpan.FromSeconds(1);
                ME_main.Position = ME_main.Position - step;
                SL_duration.Value = ME_main.Position.TotalSeconds;
                Log.Update($"Перемотка назад: {ME_main.Position.TotalMilliseconds} мс");
            }
            else if (e.Key == Key.Right && !Keyboard.Modifiers.HasFlag(ModifierKeys.Control)) {
                var step = TimeSpan.FromSeconds(1);
                ME_main.Position = ME_main.Position + step;
                SL_duration.Value = ME_main.Position.TotalSeconds;
                Log.Update($"Перемотка вперед: {ME_main.Position.TotalMilliseconds} мс");
            }
            else if (e.Key == Key.Up && Keyboard.Modifiers.HasFlag(ModifierKeys.Control)) {
                double stepVolume = 0.1;
                ME_main.Volume = Math.Min(ME_main.Volume + stepVolume, 1.0);
                SL_volume.Value = ME_main.Volume;
                Log.Update("Громкость увеличена");
            }
            else if (e.Key == Key.Down && Keyboard.Modifiers.HasFlag(ModifierKeys.Control)) {
                double stepVolume = 0.1;
                ME_main.Volume = Math.Max(ME_main.Volume - stepVolume, 0.0);
                SL_volume.Value = ME_main.Volume;
                Log.Update("Громкость уменьшена");
            }

            #region Clips encoding binds
            if (e.Key == Key.S) {
                RemoveSelection();
            }
            if (e.Key == Key.C) {
                StartTime = ME_main.Position;
                Log.Update(string.Format("Обрезка с {0}", StartTime.TotalMilliseconds));
                SL_duration.IsSelectionRangeEnabled = true;
                SL_duration.SelectionStart = StartTime.TotalSeconds;
                SliderSelectionChanged?.Invoke(StartTime, null);
                if (EndTime != TimeSpan.Zero && App.Current.MainWindow.OwnedWindows.Count == 0) {
                    OpenRendererWindow(StartTime, EndTime);
                }
            }
            if (e.Key == Key.E) {
                EndTime = ME_main.Position;
                Log.Update(string.Format("Обрезка до {0}", ME_main.Position.TotalMilliseconds));
                SL_duration.IsSelectionRangeEnabled = true;
                SL_duration.SelectionEnd = EndTime.TotalSeconds;
                if (StartTime != TimeSpan.Zero && App.Current.MainWindow.OwnedWindows.Count == 0) {
                    OpenRendererWindow(StartTime, EndTime);
                }
                else {
                    SliderSelectionChanged?.Invoke(StartTime, EndTime);
                }

            }
            if (e.Key == Key.C && Keyboard.Modifiers.HasFlag(ModifierKeys.Shift)) {
                Log.Update(string.Format("Обрезка с {0} до конца", StartTime.TotalMilliseconds));
                if (Application.Current.MainWindow.OwnedWindows.Count == 0) {
                    OpenRendererWindow(ME_main.Position, ME_main.NaturalDuration.TimeSpan);
                    SL_duration.SelectionEnd = ME_main.NaturalDuration.TimeSpan.TotalSeconds;
                    Owner.UpdateColors();
                }

            }
            if (e.Key == Key.D && App.Current.MainWindow.OwnedWindows.Count == 0) {
                OpenRendererWindow(TimeSpan.Zero, ME_main.NaturalDuration.TimeSpan);
                Owner.UpdateColors();
            }
            if (e.Key == Key.Space) SwapPlaying();

            void OpenRendererWindow(TimeSpan? StartTime, TimeSpan? EndTime) {
                RendererWindow rendererwindow = new RendererWindow(ME_main.Source, StartTime, EndTime) { Owner = Owner };
                ME_main.Pause();
                IsPlaying = false;
                SliderSelectionChanged += rendererwindow.RendererWindow_SliderSelectionChanged;
                UpdateFilename += rendererwindow.RendererWindow_ChangeSelectedFile;
                rendererwindow.Show();
            }
            #endregion
        }

        private void Btn_keyshortcuts_Click(object sender, RoutedEventArgs e) {
            string hotkeysInfo =
                "Горячие клавиши панели управления:\n\n" +
                "Стрелка влево – перемотка назад на 1 секунду\n" +
                "Стрелка вправо – перемотка вперед на 1 секунду\n" +
                "Ctrl + Стрелка вверх – увеличение громкости\n" +
                "Ctrl + Стрелка вниз – уменьшение громкости\n" +
                "S – очистка выделения\n" +
                "D – открытие окна перекодирования для всего файла\n" +
                "C – установка точки начала обрезки\n" +
                "E – установка точки завершения обрезки\n" +
                "Shift + C – обрезка от текущего момента до конца видео\n" +
                "Пробел – переключение воспроизведения (пауза/проигрывание)";
            MessageBox.Show(hotkeysInfo, "Горячие клавиши", MessageBoxButton.OK, MessageBoxImage.Information);

        }
    }
}
