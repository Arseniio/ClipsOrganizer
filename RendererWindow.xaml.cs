using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace ClipsOrganizer {
    /// <summary>
    /// Логика взаимодействия для RendererWindow.xaml
    /// </summary>
    public partial class RendererWindow : Window {
        Uri VideoPath;
        public VideoCodec LastUsedCodec { get; set; }
        public string LastUsedQuality { get; set; }
        public string LastUsedEncoderPath { get; set; }
        private int progressValue;
        public int ProgressValue {
            get => progressValue;
            set {
                progressValue = value;
                Btn_Crop.GetBindingExpression(MaterialDesignThemes.Wpf.ButtonProgressAssist.ValueProperty)?.UpdateTarget();
            }
        }

        private string getNextFileName(string FilePath) {
            int lastFileSaved = 0;
            foreach (var item in Directory.EnumerateFiles(FilePath)) {
                if (int.TryParse(item.Split('_').Last().Split('.').First(), out int fileNumber) && fileNumber > lastFileSaved) {
                    lastFileSaved = fileNumber;
                }
            }
            lastFileSaved++;
            return System.IO.Path.Combine(System.IO.Path.GetDirectoryName(VideoPath.LocalPath), string.Format("exported_{0}.mp4", lastFileSaved));
        }

        public RendererWindow(Uri VideoPath, TimeSpan? Crop_From = null, TimeSpan? Crop_To = null) {
            InitializeComponent();
            ;
            this.VideoPath = VideoPath;
            CB_codec.ItemsSource = Enum.GetValues(typeof(VideoCodec)).Cast<VideoCodec>();
            CB_codec.SelectedIndex = 0; //TODO: change later
            TB_outputPath.Text = getNextFileName(Path.GetDirectoryName(VideoPath.LocalPath));
            if (Crop_From != null || Crop_To != null) {
                TB_Crop_From.Text = Crop_From.Value.ToString(@"hh\:mm\:ss\.fff") ?? TimeSpan.Zero.ToString(@"hh\:mm\:ss\.fff");
                TB_Crop_To.Text = Crop_To.Value.ToString(@"hh\:mm\:ss\.fff") ?? TimeSpan.Zero.ToString(@"hh\:mm\:ss\.fff");
            }
            if (Settings.GlobalSettings.Instance.LastUsedCodec != VideoCodec.Unknown && Settings.GlobalSettings.Instance.LastUsedQuality != null) {
                CB_codec.SelectedItem = Settings.GlobalSettings.Instance.LastUsedCodec;
                TB_Quality.Text = Settings.GlobalSettings.Instance.LastUsedQuality;
            }
            CB_OpenFolderAfterEncoding.IsChecked = Settings.GlobalSettings.Instance.OpenFolderAfterEncoding;
            if (Owner != null) {
                ViewableControls.ViewableController.VideoViewerInstance.SliderSelectionChanged += RendererWindow_SliderSelectionChanged;
            }
            Btn_Crop.DataContext = this;
            UpdateVideoSize();
        }

        private double CalculateVideoSize(double? bitrate, TimeSpan Duration) {
            if (bitrate == null) return 0;
            double fileSizeBytes = (bitrate.Value * 1000 * Duration.TotalSeconds) / 8;
            double filesize = fileSizeBytes / (1024 * 1024);
            return filesize < 0 ? 0 : filesize;
        }

        public void RendererWindow_SliderSelectionChanged(TimeSpan Start, TimeSpan? End) {
            TB_Crop_From.Text = Start.ToString(@"hh\:mm\:ss\.fff");
            if (End != null) TB_Crop_To.Text = End?.ToString(@"hh\:mm\:ss\.fff");
        }


        private bool _isDragging = false;
        private Point _lastMousePosition;
        private TextBox _lastTB;
        private void TimestampTextBox_PreviewMouseDown(object sender, MouseButtonEventArgs e) {
            if (sender is TextBox text) {
                _lastTB = sender as TextBox;
            }
            if (e.LeftButton == MouseButtonState.Pressed) {
                _isDragging = true;
                _lastMousePosition = e.GetPosition(this);
            }
        }

        private void TimestampTextBox_PreviewMouseMove(object sender, MouseEventArgs e) {
            if (_isDragging && e.LeftButton == MouseButtonState.Pressed) {
                var currentPosition = e.GetPosition(this);
                double deltaY = currentPosition.Y - _lastMousePosition.Y;
                if (Math.Abs(deltaY) > 4) {
                    if (_lastTB != null)
                        UpdateTimestamp(deltaY);
                    _lastMousePosition = currentPosition;
                    UpdateVideoSize();
                }
            }
        }

        private void TimestampTextBox_PreviewMouseUp(object sender, MouseButtonEventArgs e) {
            _isDragging = false;
        }

        private void UpdateVideoSize() {
            double.TryParse(TB_Quality.Text, out double bitrate);
            TB_filesize.Text = "~"+ CalculateVideoSize(bitrate, TimeSpan.Parse(TB_Crop_To.Text) - TimeSpan.Parse(TB_Crop_From.Text)).ToString("F2");
        }

        private void UpdateTimestamp(double deltaY) {
            if (TimeSpan.TryParseExact(_lastTB.Text, @"hh\:mm\:ss\.fff", CultureInfo.InvariantCulture, out var timestamp)) {
                if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control) && Keyboard.Modifiers.HasFlag(ModifierKeys.Shift)) {
                    timestamp = deltaY < 0 ? timestamp.Add(TimeSpan.FromMinutes(1)) : timestamp.Subtract(TimeSpan.FromMinutes(1));
                }
                else if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control)) {
                    timestamp = deltaY < 0 ? timestamp.Add(TimeSpan.FromMilliseconds(200)) : timestamp.Subtract(TimeSpan.FromMilliseconds(200));
                }
                else if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift)) {
                    timestamp = deltaY < 0 ? timestamp.Add(TimeSpan.FromSeconds(5)) : timestamp.Subtract(TimeSpan.FromSeconds(5));
                }
                else {
                    timestamp = deltaY < 0 ? timestamp.Add(TimeSpan.FromSeconds(1)) : timestamp.Subtract(TimeSpan.FromSeconds(1));
                }
                if (timestamp < TimeSpan.Zero) {
                    timestamp = TimeSpan.Zero;
                }

                _lastTB.Text = timestamp.ToString(@"hh\:mm\:ss\.fff");
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
            VideoCodec selectedCodec = (VideoCodec)CB_codec.SelectedItem;
            if (selectedCodec != VideoCodec.Unknown && int.TryParse(TB_Quality.Text, out int bitrate)) {
                Settings.GlobalSettings.Instance.LastUsedCodec = selectedCodec;
                Settings.GlobalSettings.Instance.LastUsedQuality = bitrate.ToString();
                Settings.GlobalSettings.Instance.OpenFolderAfterEncoding = CB_OpenFolderAfterEncoding.IsChecked.Value;
            }
        }

        private void Btn_Crop_Click(object sender, RoutedEventArgs e) {
            if (Settings.GlobalSettings.Instance.ffmpegManager.IsProcessRunning) return;
            VideoCodec selectedCodec = (VideoCodec)CB_codec.SelectedItem;
            if (CB_codec.SelectedItem != null && selectedCodec != VideoCodec.Unknown && !string.IsNullOrEmpty(TB_Quality.Text) && int.TryParse(TB_Quality.Text, out int bitrate)) {
                if (TimeSpan.TryParse(TB_Crop_From.Text, out TimeSpan startTime) && TimeSpan.TryParse(TB_Crop_To.Text, out TimeSpan endTime)) {
                    if (startTime == TimeSpan.Zero && startTime > endTime) {
                        MessageBox.Show("Невозможно обрезать клип в обратную сторону.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);

                    }
                    Settings.GlobalSettings.Instance.ffmpegManager.StartEncodingAsync(VideoPath.LocalPath, TB_outputPath.Text, selectedCodec, bitrate, startTime, endTime);
                    Settings.GlobalSettings.Instance.ffmpegManager.OnEncodeProgressChanged += UpdateProgressBar;
                }
                else {
                    Settings.GlobalSettings.Instance.ffmpegManager.StartEncodingAsync(VideoPath.LocalPath, TB_outputPath.Text, selectedCodec, bitrate);
                    Settings.GlobalSettings.Instance.ffmpegManager.OnEncodeProgressChanged += UpdateProgressBar;
                }
            }
            else {
                MessageBox.Show("Пожалуйста, убедитесь, что выбраны параметры кодека и указано значение качества.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        private void UpdateProgressBar(int Precent) {
            Dispatcher.Invoke(() =>
            {
                ProgressValue = Precent;
            });
            if (Precent == 100) {
                Dispatcher.Invoke(() =>
                {
                    if (CB_OpenFolderAfterEncoding.IsChecked == true)
                        Process.Start("explorer.exe", $"/select,\"{TB_outputPath.Text}\"");
                });
            }
        }

        private void TB_Crop_TextChanged(object sender, TextChangedEventArgs e) {
            if (TimeSpan.TryParse(TB_Crop_From.Text, out var startTime) &&
                TimeSpan.TryParse(TB_Crop_To.Text, out var endTime)) {
                if (Owner == null) return;
                ViewableControls.ViewableController.VideoViewerInstance.SL_duration.SelectionStart = startTime.TotalSeconds;
                ViewableControls.ViewableController.VideoViewerInstance.SL_duration.SelectionEnd = endTime.TotalSeconds;
            }
        }

        private void TB_Quality_TextChanged(object sender, TextChangedEventArgs e) {
            UpdateVideoSize();
        }
    }
}
