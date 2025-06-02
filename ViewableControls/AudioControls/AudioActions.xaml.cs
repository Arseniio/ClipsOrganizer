using ClipsOrganizer.Classes;
using ClipsOrganizer.Model;
using ClipsOrganizer.Settings;

using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Linq;

namespace ClipsOrganizer.ViewableControls.AudioControls {
    public partial class AudioActions : UserControl {
        private Item currentItem;
        private ExportFileInfoAudio exportInfo;

        public AudioActions() {
            InitializeComponent();
            CB_Format.ItemsSource = Enum.GetValues(typeof(ExportAudioFormat));
        }

        public AudioActions(Item item) : this() {
            currentItem = item;
            InitializeExportInfo();
        }

        private void InitializeExportInfo() {
            if (currentItem == null) return;

            var defaultSettings = GlobalSettings.Instance.DefaultAudioExport;
            exportInfo = new ExportFileInfoAudio
            {
                Path = currentItem.Path,
                Name = System.IO.Path.GetFileNameWithoutExtension(currentItem.Path),
                AudioBitrate = defaultSettings.AudioBitrate,
                AudioSampleRate = defaultSettings.AudioSampleRate,
                AudioChannels = defaultSettings.AudioChannels,
                NormalizeAudio = defaultSettings.NormalizeAudio,
                outputFormat = defaultSettings.outputFormat,
                TrimStart = defaultSettings.TrimStart,
                TrimEnd = defaultSettings.TrimEnd
            };
            exportInfo.Name = $"{exportInfo.Name}.{exportInfo.outputFormat.ToString().ToLower()}";
            DataContext = exportInfo;
            CB_Format.SelectedItem = null;
            CB_Format.SelectedItem = exportInfo.outputFormat;
        }

        private void ValidateNumberInput(object sender, TextChangedEventArgs e) {
            if (sender is TextBox textBox) {
                if (!int.TryParse(textBox.Text, out int value) || value <= 0) {
                    textBox.Text = "1";
                }
            }
        }

        private void ValidateNumberInput(object sender, RoutedEventArgs e) {
            if (sender is TextBox textBox) {
                if (!int.TryParse(textBox.Text, out int value) || value <= 0) {
                    textBox.Text = "1";
                }
            }
        }

        private void ValidateTrimTime(object sender, TextChangedEventArgs e) {
            if (sender is TextBox textBox && exportInfo != null) {
                if (TimeSpan.TryParse(textBox.Text, out TimeSpan time)) {
                    if (textBox.Name.Contains("TrimStart")) {
                        if (exportInfo.TrimEnd > TimeSpan.Zero && time >= exportInfo.TrimEnd) {
                            textBox.Text = "00:00:00";
                            exportInfo.TrimStart = TimeSpan.Zero;
                        }
                    }
                    else if (textBox.Name.Contains("TrimEnd")) {
                        if (exportInfo.TrimStart > TimeSpan.Zero && time <= exportInfo.TrimStart) {
                            textBox.Text = "00:00:00";
                            exportInfo.TrimEnd = TimeSpan.Zero;
                        }
                    }
                }
            }
        }

        private void ValidateTrimTime(object sender, RoutedEventArgs e) {
            if (sender is TextBox textBox && exportInfo != null) {
                if (TimeSpan.TryParse(textBox.Text, out TimeSpan time)) {
                    if (textBox.Name.Contains("TrimStart")) {
                        if (exportInfo.TrimEnd > TimeSpan.Zero && time >= exportInfo.TrimEnd) {
                            textBox.Text = "00:00:00";
                            exportInfo.TrimStart = TimeSpan.Zero;
                        }
                    }
                    else if (textBox.Name.Contains("TrimEnd")) {
                        if (exportInfo.TrimStart > TimeSpan.Zero && time <= exportInfo.TrimStart) {
                            textBox.Text = "00:00:00";
                            exportInfo.TrimEnd = TimeSpan.Zero;
                        }
                    }
                }
            }
        }

        private void Btn_AddToQueue_Click(object sender, RoutedEventArgs e) {
            if (currentItem == null || exportInfo == null) return;

            ExportQueue.Enqueue(exportInfo);
            Log.Update($"Задача экспорта аудио добавлена в очередь: {Path.GetFileName(currentItem.Path)}");
        }

        private async void Btn_ExportNow_Click(object sender, RoutedEventArgs e) {
            if (currentItem == null || exportInfo == null) return;

            try {
                Btn_ExportNow.IsEnabled = false;
                var ffmpegManager = GlobalSettings.Instance.FFmpegInit();
                string outputPath = System.IO.Path.Combine(
                    System.IO.Path.GetDirectoryName(currentItem.Path),
                    $"exported_{DateTime.Now:yyyyMMdd_HHmmss}_{System.IO.Path.GetFileNameWithoutExtension(currentItem.Path)}.{exportInfo.outputFormat}"
                );

                bool success = await ffmpegManager.StartAudioEncodingAsync(exportInfo, outputPath, System.Threading.CancellationToken.None);

                if (success) {
                    Log.Update($"Аудио успешно экспортировано: {System.IO.Path.GetFileName(outputPath)}");
                    if (GlobalSettings.Instance.OpenFolderAfterEncoding) {
                        System.Diagnostics.Process.Start("explorer.exe", System.IO.Path.GetDirectoryName(outputPath));
                    }
                }
                else {
                    Log.Update($"Ошибка при экспорте аудио: {System.IO.Path.GetFileName(currentItem.Path)}");
                }
            }
            catch (Exception ex) {
                Log.Update($"Ошибка при экспорте аудио: {ex.Message}");
                MessageBox.Show($"Произошла ошибка при экспорте: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally {
                Btn_ExportNow.IsEnabled = true;
            }
        }

        private void CB_Format_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (exportInfo != null && sender is ComboBox comboBox && comboBox.SelectedItem is ExportAudioFormat format) {
                exportInfo.outputFormat = format;
                // Обновляем расширение файла при изменении формата
                exportInfo.Name = $"{System.IO.Path.GetFileNameWithoutExtension(exportInfo.Name)}.{format.ToString().ToLower()}";

                // Принудительно обновляем привязку
                var binding = comboBox.GetBindingExpression(ComboBox.SelectedItemProperty);
                if (binding != null) {
                    binding.UpdateTarget();
                }
            }
        }
    }
}