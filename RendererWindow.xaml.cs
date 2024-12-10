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
        Settings.GlobalSettings settings;
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
                if (int.TryParse(item.Split('_').Last().Split('.').First(), out int fileNumber)) {
                    if (fileNumber > lastFileSaved) {
                        lastFileSaved = fileNumber;
                    }
                }
            }
            lastFileSaved++;
            return System.IO.Path.Combine(System.IO.Path.GetDirectoryName(VideoPath.LocalPath), string.Format("exported_{0}.mp4", lastFileSaved));
        }
        DispatcherTimer timer;

        public RendererWindow(Settings.GlobalSettings settings, Uri VideoPath, TimeSpan? Crop_From = null, TimeSpan? Crop_To = null) {
            InitializeComponent();
            this.settings = settings;
            this.VideoPath = VideoPath;
            CB_codec.ItemsSource = Enum.GetValues(typeof(VideoCodec)).Cast<VideoCodec>();
            CB_codec.SelectedIndex = 0; //TODO: change later
            TB_outputPath.Text = getNextFileName(Path.GetDirectoryName(VideoPath.LocalPath));
            if (Crop_From != null || Crop_To != null) {
                TB_Crop_From.Text = Crop_From.Value.ToString(@"hh\:mm\:ss\.fff") ?? TimeSpan.Zero.ToString(@"hh\:mm\:ss\.fff");
                TB_Crop_To.Text = Crop_To.Value.ToString(@"hh\:mm\:ss\.fff") ?? TimeSpan.Zero.ToString(@"hh\:mm\:ss\.fff");
            }
            if (settings.LastUsedCodec != VideoCodec.Unknown && settings.LastUsedQuality != null) {
                CB_codec.SelectedItem = settings.LastUsedCodec;
                TB_Quality.Text = settings.LastUsedQuality;
            }
            CB_OpenFolderAfterEncoding.IsChecked = settings.OpenFolderAfterEncoding;
            if (Owner != null) {
                (Owner as MainWindow).SliderSelectionChanged += RendererWindow_SliderSelectionChanged;
            }
            Btn_Crop.DataContext = this;
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
                }
            }
        }

        private void TimestampTextBox_PreviewMouseUp(object sender, MouseButtonEventArgs e) {
            _isDragging = false;
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
                settings.LastUsedCodec = selectedCodec;
                settings.LastUsedQuality = bitrate.ToString();
                settings.OpenFolderAfterEncoding = CB_OpenFolderAfterEncoding.IsChecked.Value;
            }
        }

        private void Btn_Crop_Click(object sender, RoutedEventArgs e) {
            if (settings.ffmpegManager.IsProcessRunning) return;
            VideoCodec selectedCodec = (VideoCodec)CB_codec.SelectedItem;
            if (CB_codec.SelectedItem != null && selectedCodec != VideoCodec.Unknown && !string.IsNullOrEmpty(TB_Quality.Text) && int.TryParse(TB_Quality.Text, out int bitrate)) {
                if (TimeSpan.TryParse(TB_Crop_From.Text, out TimeSpan startTime) && TimeSpan.TryParse(TB_Crop_To.Text, out TimeSpan endTime)) {
                    if (startTime == TimeSpan.Zero && startTime > endTime) {
                        MessageBox.Show("Невозможно обрезать клип в обратную сторону.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);

                    }
                    settings.ffmpegManager.StartEncodingAsync(VideoPath.LocalPath, TB_outputPath.Text, selectedCodec, bitrate, startTime, endTime);
                    settings.ffmpegManager.OnEncodeProgressChanged += UpdateProgressBar;
                }
                else {
                    settings.ffmpegManager.StartEncodingAsync(VideoPath.LocalPath, TB_outputPath.Text, selectedCodec, bitrate);
                    settings.ffmpegManager.OnEncodeProgressChanged += UpdateProgressBar;
                }
            }
            else {
                MessageBox.Show("Пожалуйста, убедитесь, что выбраны параметры кодека и указано значение качества.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            if (CB_OpenFolderAfterEncoding.IsChecked == true) {

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
                    Process.Start("explorer.exe", $"/select,\"{TB_outputPath.Text}\"");
                });
            }
        }

        private void TB_Crop_TextChanged(object sender, TextChangedEventArgs e) {
            if (TimeSpan.TryParse(TB_Crop_From.Text, out var startTime) &&
                TimeSpan.TryParse(TB_Crop_To.Text, out var endTime)) {
                if (Owner == null) return;
                (Owner as MainWindow).SL_duration.SelectionStart = startTime.TotalSeconds;
                (Owner as MainWindow).SL_duration.SelectionEnd = endTime.TotalSeconds;
            }
        }


    }
}
