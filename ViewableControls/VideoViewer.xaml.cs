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
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Windows.UI.ApplicationSettings;

namespace ClipsOrganizer.ViewableControls {
    /// <summary>
    /// Логика взаимодействия для VideoControls.xaml
    /// </summary>
    public partial class VideoViewer : UserControl {
        DispatcherTimer SliderTimer;
        TimeSpan StartTime = TimeSpan.Zero;
        public event Action<TimeSpan, TimeSpan?> SliderSelectionChanged;
        MainWindow Owner = null;
        public VideoViewer() {
            InitializeComponent();
            #region Slider timer init
            SliderTimer = new DispatcherTimer();
            SliderTimer.Interval = TimeSpan.FromMilliseconds(100);
            SliderTimer.Tick += VideoDurationUpdate;
            #endregion 
        }

        #region sliders events
        private void SL_duration_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e) {
            is_dragging = false;
            ME_main.Position = TimeSpan.FromSeconds((double)SL_duration.Value);
        }

        private void MainGrid_SizeChanged(object sender, SizeChangedEventArgs e) {
            ////-493
            //if (MainGrid.ActualWidth - 700 + 430 > 0) {
            //    SL_duration.Width = MainGrid.ActualWidth - 700 + 430;
            //}
        }

        bool is_dragging = false;
        private void SL_duration_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
            if (!is_dragging) ME_main.Position = TimeSpan.FromSeconds((double)SL_duration.Value);
        }

        private void SL_duration_DragStarted(object sender, System.Windows.Controls.Primitives.DragStartedEventArgs e) {
            is_dragging = true;
        }

        private void VideoDurationUpdate(object sender, EventArgs e) {
            if (!is_dragging)
                SL_duration.Value = ME_main.Position.TotalSeconds;
            if (SL_duration.IsSelectionRangeEnabled && App.Current.MainWindow.OwnedWindows.Count == 0)
                SL_duration.SelectionEnd = ME_main.Position.TotalSeconds;
        }
        #endregion

        #region player events
        private void Btn_Play_Click(object sender, RoutedEventArgs e) {
            ME_main.Play();
        }

        private void SL_volume_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
            ME_main.Volume = (double)e.NewValue;
        }

        private void ME_main_MediaOpened(object sender, RoutedEventArgs e) {
            if (ME_main.NaturalDuration.HasTimeSpan) {
                SL_duration.Maximum = ME_main.NaturalDuration.TimeSpan.TotalSeconds;
                SliderTimer.Start();
            }
        }

        private void Btn_Stop_Click(object sender, RoutedEventArgs e) {
            ME_main.Pause();
        }
        #endregion

        public void LoadVideoFile(string VideoPath) {
            Owner = Window.GetWindow(this) as MainWindow;
            RemoveSelection();
            if (VideoPath != null) {
                ME_main.Source = new Uri(VideoPath);
                ME_main.Play();
                return;
            }
        }

        private void RemoveSelection() {
            StartTime = TimeSpan.Zero;
            SL_duration.SelectionStart = 0;
            SL_duration.SelectionEnd = 0;
            SL_duration.IsSelectionRangeEnabled = false;
        }

        public void HandleKeyStroke(KeyEventArgs e) {

            #region Clips encoding binds
            if (e.Key == Key.S) {
                RemoveSelection();
            }
            if (e.Key == Key.C) {
                StartTime = ME_main.Position;
                Log.Update(string.Format("Обрезка с {0}", StartTime.TotalMilliseconds));
                SL_duration.IsSelectionRangeEnabled = true;
                SL_duration.SelectionStart = ME_main.Position.TotalSeconds;
                SliderSelectionChanged?.Invoke(StartTime, null);
            }
            if (e.Key == Key.C && Keyboard.Modifiers.HasFlag(ModifierKeys.Shift)) {
                Log.Update(string.Format("Обрезка с {0} до конца", StartTime.TotalMilliseconds));
                if (Application.Current.MainWindow.OwnedWindows.Count == 0) {
                    OpenRendererWindow(ME_main.Position, ME_main.NaturalDuration.TimeSpan);
                    SL_duration.SelectionEnd = ME_main.NaturalDuration.TimeSpan.TotalSeconds;
                    Owner.UpdateColors();
                }

            }
            if (e.Key == Key.E) {
                Log.Update(string.Format("Обрезка до {0}", ME_main.Position.TotalMilliseconds));
                SL_duration.IsSelectionRangeEnabled = true;
                if (StartTime == TimeSpan.Zero) SL_duration.SelectionStart = 0;
                SL_duration.SelectionEnd = ME_main.Position.TotalSeconds;
                if (App.Current.MainWindow.OwnedWindows.Count == 0) {
                    OpenRendererWindow(StartTime, ME_main.Position);
                    Owner.UpdateColors();
                }
                else {
                    SliderSelectionChanged?.Invoke(StartTime, ME_main.Position);
                }

            }
            if (e.Key == Key.D && App.Current.MainWindow.OwnedWindows.Count != 0) {
                OpenRendererWindow(null, ME_main.NaturalDuration.TimeSpan);
                Owner.UpdateColors();
            }

            void OpenRendererWindow(TimeSpan? StartTime, TimeSpan? EndTime) {
                RendererWindow rendererwindow = new RendererWindow(ME_main.Source, StartTime, EndTime) { Owner = Owner };
                ME_main.Pause();
                SliderSelectionChanged += rendererwindow.RendererWindow_SliderSelectionChanged;
                rendererwindow.Show();
            }
            #endregion
        }
    }
}
