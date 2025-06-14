using ClipsOrganizer.Classes;
using ClipsOrganizer.Model;
using ClipsOrganizer.Settings;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ClipsOrganizer {
    public partial class AudioRendererWindow : Window {
        private Uri AudioPath;
        private int progressValue;
        public int ProgressValue {
            get => progressValue;
            set {
                progressValue = value;
                Btn_Export.GetBindingExpression(MaterialDesignThemes.Wpf.ButtonProgressAssist.ValueProperty)?.UpdateTarget();
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
            return Path.Combine(Path.GetDirectoryName(AudioPath.LocalPath), string.Format("exported_{0}.mp3", lastFileSaved));
        }

        public AudioRendererWindow(Uri AudioPath, TimeSpan? Crop_From = null, TimeSpan? Crop_To = null) {
            InitializeComponent();
            this.AudioPath = AudioPath;
            
            // Инициализация комбобокса форматов
            CB_Format.ItemsSource = Enum.GetValues(typeof(ExportAudioFormat)).Cast<ExportAudioFormat>();
            
            // Загрузка настроек по умолчанию
            var defaultSettings = Settings.GlobalSettings.Instance.DefaultAudioExport;
            CB_Format.SelectedItem = defaultSettings.outputFormat;
            TB_Bitrate.Text = defaultSettings.AudioBitrate.ToString();
            TB_SampleRate.Text = defaultSettings.AudioSampleRate.ToString();
            TB_Channels.Text = defaultSettings.AudioChannels.ToString();
            CB_Normalize.IsChecked = defaultSettings.NormalizeAudio;

            // Установка путей обрезки
            if (Crop_From != null || Crop_To != null) {
                TB_Crop_From.Text = Crop_From?.ToString(@"hh\:mm\:ss\.fff") ?? TimeSpan.Zero.ToString(@"hh\:mm\:ss\.fff");
                TB_Crop_To.Text = Crop_To?.ToString(@"hh\:mm\:ss\.fff") ?? TimeSpan.Zero.ToString(@"hh\:mm\:ss\.fff");
            }
            else {
                TB_Crop_From.Text = defaultSettings.TrimStart.ToString(@"hh\:mm\:ss\.fff");
                TB_Crop_To.Text = defaultSettings.TrimEnd.ToString(@"hh\:mm\:ss\.fff");
            }

            // Загрузка последних использованных настроек
            if (Settings.GlobalSettings.Instance.LastUsedAudioFormat != null) {
                CB_Format.SelectedItem = Settings.GlobalSettings.Instance.LastUsedAudioFormat;
                TB_Bitrate.Text = Settings.GlobalSettings.Instance.LastUsedAudioBitrate.ToString();
            }

            CB_OpenFolderAfterEncoding.IsChecked = Settings.GlobalSettings.Instance.OpenFolderAfterEncoding;
            
            // Привязка контекста данных
            Btn_Export.DataContext = this;

            UpdateLength();
        }

        private void UpdateLength() {
            if (TimeSpan.TryParse(TB_Crop_From.Text, out TimeSpan startTime) &&
                TimeSpan.TryParse(TB_Crop_To.Text, out TimeSpan endTime)) {
                var length = endTime - startTime;
                TB_Length.Text = length.ToString(@"hh\:mm\:ss\.fff");
            }

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
                    UpdateLength();
                }
            }
        }

        private void UpdateTimestamp(double deltaY) {
            if (TimeSpan.TryParseExact(_lastTB.Text, @"hh\:mm\:ss\.fff", System.Globalization.CultureInfo.InvariantCulture, out var timestamp)) {
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

        private void TB_Crop_TextChanged(object sender, TextChangedEventArgs e) {
            UpdateLength();
        }

        private void TB_ValueChanged(object sender, TextChangedEventArgs e) {
            // Здесь будет обновление информации о файле
        }

        private void CB_Format_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            // Здесь будет обновление информации о файле
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
            if (CB_Format.SelectedItem != null && int.TryParse(TB_Bitrate.Text, out int bitrate)) {
                Settings.GlobalSettings.Instance.LastUsedAudioFormat = (ExportAudioFormat)CB_Format.SelectedItem;
                Settings.GlobalSettings.Instance.LastUsedAudioBitrate = bitrate;
                Settings.GlobalSettings.Instance.OpenFolderAfterEncoding = CB_OpenFolderAfterEncoding.IsChecked ?? false;
            }
        }

        private async void Btn_Export_Click(object sender, RoutedEventArgs e) {
            if (CB_Format.SelectedItem == null) {
                Log.Update("Пожалуйста, выберите формат экспорта.");
                return;
            }

            if (!int.TryParse(TB_Bitrate.Text, out int bitrate) || bitrate <= 0) {
                Log.Update("Пожалуйста, укажите корректное значение битрейта.");
                return;
            }

            if (!int.TryParse(TB_SampleRate.Text, out int sampleRate) || sampleRate <= 0) {
                Log.Update("Пожалуйста, укажите корректное значение частоты дискретизации.");
                return;
            }

            if (!int.TryParse(TB_Channels.Text, out int channels) || channels <= 0) {
                Log.Update("Пожалуйста, укажите корректное количество каналов.");
                return;
            }

            TimeSpan endTime = TimeSpan.Zero;
            bool isTrimmed = TimeSpan.TryParse(TB_Crop_From.Text, out TimeSpan startTime)
                             && TimeSpan.TryParse(TB_Crop_To.Text, out endTime);

            if (isTrimmed && startTime >= endTime) {
                Log.Update("Невозможно обрезать аудио: начальное время должно быть меньше конечного.");
                return;
            }

            ExportFileInfoAudio exportInfo = new ExportFileInfoAudio {
                Path = AudioPath.LocalPath,
                Name = Path.GetFileName(AudioPath.LocalPath),
                OutputPath = getNextFileName(Path.GetDirectoryName(AudioPath.LocalPath)),
                AudioBitrate = bitrate,
                AudioSampleRate = sampleRate,
                AudioChannels = channels,
                NormalizeAudio = CB_Normalize.IsChecked ?? true,
                outputFormat = (ExportAudioFormat)CB_Format.SelectedItem,
                TrimStart = isTrimmed ? startTime : TimeSpan.Zero,
                TrimEnd = isTrimmed ? endTime : TimeSpan.Zero
            };

            try {
                Btn_Export.IsEnabled = false;
                var ffmpegManager = GlobalSettings.Instance.FFmpegInit();
                bool success = await ffmpegManager.StartAudioEncodingAsync(exportInfo, exportInfo.OutputPath, CancellationToken.None);
                
                if (success) {
                    Log.Update($"Аудио успешно экспортировано: {Path.GetFileName(exportInfo.OutputPath)}");
                    if (CB_OpenFolderAfterEncoding.IsChecked ?? false) {
                        System.Diagnostics.Process.Start("explorer.exe", Path.GetDirectoryName(exportInfo.OutputPath));
                    }
                }
                else {
                    Log.Update($"Ошибка при экспорте аудио: {Path.GetFileName(AudioPath.LocalPath)}");
                }
            }
            catch (Exception ex) {
                Log.Update($"Ошибка при экспорте аудио: {ex.Message}");
                MessageBox.Show($"Произошла ошибка при экспорте: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally {
                Btn_Export.IsEnabled = true;
            }

            this.Close();
        }

        public void AudioRendererWindow_SliderSelectionChanged(TimeSpan start, TimeSpan? end) {
            if (start != TimeSpan.Zero) {
                TB_Crop_From.Text = start.ToString(@"hh\:mm\:ss\.fff");
            }
            if (end.HasValue) {
                TB_Crop_To.Text = end.Value.ToString(@"hh\:mm\:ss\.fff");
            }
            UpdateLength();
        }


    }
} 