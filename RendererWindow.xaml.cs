using ClipsOrganizer.Model;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
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
                ViewableControls.ViewableController.VideoViewerInstance.UpdateFilename += RendererWindow_ChangeSelectedFile;
            }
            Btn_Crop.DataContext = this;
            UpdateVideoSize();
        }


        public void RendererWindow_SliderSelectionChanged(TimeSpan Start, TimeSpan? End) {
            TB_Crop_From.Text = Start.ToString(@"hh\:mm\:ss\.fff");
            if (End != null) TB_Crop_To.Text = End?.ToString(@"hh\:mm\:ss\.fff");
        }

        public void RendererWindow_ChangeSelectedFile(Uri Filename) {
            this.VideoPath = Filename;
            TB_outputPath.Text = getNextFileName(Path.GetDirectoryName(VideoPath.LocalPath));
            UpdateVideoSize();
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
        private async void UpdateVideoSize() {
            double.TryParse(TB_Quality.Text, out double bitrate);
            var Length = TimeSpan.Parse(TB_Crop_To.Text) - TimeSpan.Parse(TB_Crop_From.Text);
            var size = await FFmpegManager.CalculateVideoSizeAsync(VideoPath.OriginalString, overrideDuration: Length, overrideVideoBitrate: bitrate);
            TB_filesize.Text = "~" + size.ToString("F2");
            TB_Length.Text = Length.ToString(@"hh\:mm\:ss\.fff");
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
        private CancellationTokenSource cropCts = new CancellationTokenSource();
        private bool isCropping = false;
        private async void Btn_Crop_Click(object sender, RoutedEventArgs e) {
            if (isCropping) {
                cropCts?.Cancel();
                Log.Update("Отмена кодирования...");
                return;
            }

            if (CB_codec.SelectedItem == null || !(CB_codec.SelectedItem is VideoCodec selectedCodec) || selectedCodec == VideoCodec.Unknown) {
                Log.Update("Пожалуйста, выберите кодек.");
                return;
            }

            if (!int.TryParse(TB_Quality.Text, out int bitrate) || bitrate <= 0) {
                Log.Update("Пожалуйста, укажите корректное значение качества (битрейта).");
                return;
            }

            TimeSpan endTime = TimeSpan.Zero;
            bool isTrimmed = TimeSpan.TryParse(TB_Crop_From.Text, out TimeSpan startTime)
                             && TimeSpan.TryParse(TB_Crop_To.Text, out endTime);

            if (isTrimmed && startTime >= endTime) {
                Log.Update("Невозможно обрезать клип: начальное время должно быть меньше конечного.");
                return;
            }

            ExportFileInfoVideo exportInfo = new ExportFileInfoVideo
            {
                Path = VideoPath.LocalPath,
                OutputPath = TB_outputPath.Text,
                VideoCodec = selectedCodec,
                VideoBitrate = bitrate,
                TrimStart = isTrimmed ? startTime : TimeSpan.Zero,
                TrimEnd = isTrimmed ? endTime : TimeSpan.Zero
            };

            Settings.GlobalSettings.Instance.ffmpegManager.OnEncodeProgressChanged += UpdateProgressBar;

            cropCts = new CancellationTokenSource();
            isCropping = true;
            Btn_Crop.Content = "Отмена";

            try {
                bool result = await Settings.GlobalSettings.Instance.ffmpegManager
                    .StartEncodingAsync(exportInfo, TB_outputPath.Text, bitrate, cropCts.Token);

                if (result) {
                    Log.Update("Кодирование завершено успешно.");
                }
                else {
                    Log.Update("Ошибка при кодировании.");
                }
            }
            catch (OperationCanceledException) {
                Log.Update("Кодирование отменено.");
            }
            catch (Exception ex) {
                Log.Update($"Ошибка: {ex.Message}");
            }
            finally {
                Settings.GlobalSettings.Instance.ffmpegManager.OnEncodeProgressChanged -= UpdateProgressBar;

                if (CB_OpenFolderAfterEncoding.IsChecked == true && File.Exists(exportInfo.OutputPath)) {
                    Process.Start("explorer.exe", $"/select,\"{exportInfo.OutputPath}\"");
                }

                cropCts.Dispose();
                cropCts = null;
                progressValue = 0;
                isCropping = false;
                Btn_Crop.Content = "Обрезать";
            }
        }


        private void UpdateProgressBar(int Precent) {
            Dispatcher.Invoke(() =>
            {
                ProgressValue = Precent;
            });

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
