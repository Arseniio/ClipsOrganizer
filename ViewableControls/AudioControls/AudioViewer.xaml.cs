using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

using ClipsOrganizer.Model;

namespace ClipsOrganizer.ViewableControls.AudioControls {
    /// <summary>
    /// Логика взаимодействия для AudioViewer.xaml
    /// </summary>
    public partial class AudioViewer : UserControl {

        private DispatcherTimer _positionTimer;
        private TimeSpan StartTime = TimeSpan.Zero;
        private TimeSpan EndTime = TimeSpan.Zero;
        private bool IsPlaying = true;
        public event Action<TimeSpan, TimeSpan?> SliderSelectionChanged;
        public event Action<Uri> UpdateFilename;
        MainWindow Owner = null;

        public AudioViewer(Item LoadedFile) {
            MediaPlayer = new MediaPlayer();
            InitializeComponent();
            WaveForm_Viewer.FilePath = LoadedFile.Path;
            MediaPlayer.Open(new Uri(LoadedFile.Path));
            Loaded += AudioViewer_Loaded;
            Unloaded += AudioViewer_Unloaded;
            
            // Инициализация таймера для обновления позиции
            _positionTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };
            _positionTimer.Tick += PositionTimer_Tick;
        }

        private void _Visual_OnElementClicked(object sender, TimeSpan e) {
            if (MediaPlayer != null) {
                MediaPlayer.Position = e;
            }
        }

        private void AudioViewer_Unloaded(object sender, RoutedEventArgs e) {
            ViewableController.FileLoaded -= ViewableController_FileLoaded;
            _positionTimer.Stop();
        }

        private void ViewableController_FileLoaded(object sender, FileLoadedEventArgs e) {
            WaveForm_Viewer.FilePath = e.Item.Path;
            WaveForm_Viewer.Resolution = 120;
            MediaPlayer.Open(new Uri(e.Item.Path));
        }

        private void AudioViewer_Loaded(object sender, RoutedEventArgs e) {
            ViewableController.FileLoaded += ViewableController_FileLoaded;
        }

        private void Button_Click(object sender, RoutedEventArgs e) {
            WaveForm_Viewer.loadWaveForm();
        }

        private void TB_Samplerate_LostFocus(object sender, RoutedEventArgs e) {
            WaveForm_Viewer.SamplesPerChunk = int.Parse((sender as TextBox).Text);
        }

        private void PositionTimer_Tick(object sender, EventArgs e) {
            if (MediaPlayer.NaturalDuration.HasTimeSpan) {
                var position = MediaPlayer.Position;
                var duration = MediaPlayer.NaturalDuration.TimeSpan;
                TB_length.Text = $"{position:hh\\:mm\\:ss} / {duration:hh\\:mm\\:ss}";
            }
        }

        private void SL_duration_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
            if (!this.IsLoaded || MediaPlayer == null) return;
            if (MediaPlayer.NaturalDuration.HasTimeSpan) {
                var duration = MediaPlayer.NaturalDuration.TimeSpan;
                var newPosition = TimeSpan.FromSeconds(e.NewValue * duration.TotalSeconds);
                MediaPlayer.Position = newPosition;
            }
        }

        private void SL_duration_DragStarted(object sender, System.Windows.Controls.Primitives.DragStartedEventArgs e) {
            _positionTimer.Stop();
        }

        private void SL_duration_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e) {
            _positionTimer.Start();
        }

        MediaPlayer MediaPlayer;
        private void Btn_Play_Click(object sender, RoutedEventArgs e) {
            MediaPlayer.Play();
            WaveForm_Viewer._Visual.SetupMediaPositionTracking(MediaPlayer);
            WaveForm_Viewer._Visual.OnElementClicked += _Visual_OnElementClicked;
            _positionTimer.Start();
        }

        private void Btn_Stop_Click(object sender, RoutedEventArgs e) {
            if (MediaPlayer.CanPause) {
                MediaPlayer.Pause();
                _positionTimer.Stop();
            }
        }

        private void SL_volume_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
            if (this.IsLoaded)
                MediaPlayer.Volume = (double)e.NewValue;
        }

        private void Btn_keyshortcuts_Click(object sender, RoutedEventArgs e) {
            string hotkeysInfo =
                "Горячие клавиши панели управления:\n\n" +
                "Стрелка влево – перемотка назад на 1 секунду\n" +
                "Стрелка вправо – перемотка вперед на 1 секунду\n" +
                "Ctrl + Стрелка вверх – увеличение громкости\n" +
                "Ctrl + Стрелка вниз – уменьшение громкости\n" +
                "S – очистка выделения\n" +
                "C – установка точки начала обрезки\n" +
                "E – установка точки завершения обрезки\n" +
                "Shift + C – обрезка от текущего момента до конца аудио\n" +
                "Пробел – переключение воспроизведения (пауза/проигрывание)";
            MessageBox.Show(hotkeysInfo, "Горячие клавиши", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public void HandleKeyStroke(KeyEventArgs e) {
            if (e.Key == Key.Left && !Keyboard.Modifiers.HasFlag(ModifierKeys.Control)) {
                var step = TimeSpan.FromSeconds(1);
                MediaPlayer.Position = MediaPlayer.Position - step;
                Log.Update($"Перемотка назад: {MediaPlayer.Position.TotalMilliseconds} мс");
            }
            else if (e.Key == Key.Right && !Keyboard.Modifiers.HasFlag(ModifierKeys.Control)) {
                var step = TimeSpan.FromSeconds(1);
                MediaPlayer.Position = MediaPlayer.Position + step;
                Log.Update($"Перемотка вперед: {MediaPlayer.Position.TotalMilliseconds} мс");
            }
            else if (e.Key == Key.Up && Keyboard.Modifiers.HasFlag(ModifierKeys.Control)) {
                double stepVolume = 0.1;
                MediaPlayer.Volume = Math.Min(MediaPlayer.Volume + stepVolume, 1.0);
                SL_volume.Value = MediaPlayer.Volume;
                Log.Update("Громкость увеличена");
            }
            else if (e.Key == Key.Down && Keyboard.Modifiers.HasFlag(ModifierKeys.Control)) {
                double stepVolume = 0.1;
                MediaPlayer.Volume = Math.Max(MediaPlayer.Volume - stepVolume, 0.0);
                SL_volume.Value = MediaPlayer.Volume;
                Log.Update("Громкость уменьшена");
            }

            #region Clips encoding binds
            if (e.Key == Key.S) {
                RemoveSelection();
            }
            if (e.Key == Key.C) {
                StartTime = MediaPlayer.Position;
                Log.Update(string.Format("Обрезка с {0}", StartTime.TotalMilliseconds));
                SliderSelectionChanged?.Invoke(StartTime, null);
                if (EndTime != TimeSpan.Zero && App.Current.MainWindow.OwnedWindows.Count == 0) {
                    OpenAudioRendererWindow(StartTime, EndTime);
                }
            }
            if (e.Key == Key.E) {
                EndTime = MediaPlayer.Position;
                Log.Update(string.Format("Обрезка до {0}", MediaPlayer.Position.TotalMilliseconds));
                if (StartTime != TimeSpan.Zero && App.Current.MainWindow.OwnedWindows.Count == 0) {
                    OpenAudioRendererWindow(StartTime, EndTime);
                }
                else {
                    SliderSelectionChanged?.Invoke(StartTime, EndTime);
                }
            }
            if (e.Key == Key.C && Keyboard.Modifiers.HasFlag(ModifierKeys.Shift)) {
                Log.Update(string.Format("Обрезка с {0} до конца", StartTime.TotalMilliseconds));
                if (Application.Current.MainWindow.OwnedWindows.Count == 0) {
                    OpenAudioRendererWindow(MediaPlayer.Position, MediaPlayer.NaturalDuration.TimeSpan);
                }
            }
            if (e.Key == Key.Space) SwapPlaying();

            void OpenAudioRendererWindow(TimeSpan? StartTime, TimeSpan? EndTime) {
                AudioRendererWindow rendererwindow = new AudioRendererWindow(MediaPlayer.Source, StartTime, EndTime) { Owner = Owner };
                MediaPlayer.Pause();
                IsPlaying = false;
                SliderSelectionChanged += rendererwindow.AudioRendererWindow_SliderSelectionChanged;
                UpdateFilename += rendererwindow.AudioRendererWindow_ChangeSelectedFile;
                rendererwindow.Show();
            }
            #endregion
        }

        private void Btn_ExportAudio_Click(object sender, RoutedEventArgs e)
        {
            var trimStart = WaveForm_Viewer.TrimStart ?? TimeSpan.Zero;
            var trimEnd = WaveForm_Viewer.TrimEnd ?? TimeSpan.Zero;
            var audioPath = WaveForm_Viewer.FilePath;
            var wnd = new ClipsOrganizer.AudioRendererWindow(new Uri(audioPath), trimStart, trimEnd);
            wnd.Show();
        }

        private void RemoveSelection() {
            StartTime = TimeSpan.Zero;
            EndTime = TimeSpan.Zero;
            SliderSelectionChanged?.Invoke(TimeSpan.Zero, null);
        }

        private void SwapPlaying() {
            Log.Update($"{IsPlaying} -> {!IsPlaying}");
            if (!IsPlaying) {
                IsPlaying = true;
                MediaPlayer.Play();
            }
            else {
                IsPlaying = false;
                MediaPlayer.Pause();
            }
        }
    }
}
