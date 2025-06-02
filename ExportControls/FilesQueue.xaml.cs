using ClipsOrganizer.Classes;
using ClipsOrganizer.Model;
using ClipsOrganizer.Settings;
using ClipsOrganizer.ViewableControls.ImageControls;
using ClipsOrganizer.ViewableControls.VideoControls;
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
using Windows.ApplicationModel.VoiceCommands;

namespace ClipsOrganizer.ExportControls {
    /// <summary>
    /// Логика взаимодействия для FilesQueue.xaml
    /// </summary>
    public partial class FilesQueue : UserControl {
        public FilesQueue() {
            InitializeComponent();
            LB_Queue.DataContext = ExportQueue._queue;
            LB_Queue.ItemsSource = ExportQueue._queue;
            TB_QueueLength.Text = $"Очередь: {ExportQueue._queue.Count} ожидающих заданий";
        }

        private void Btn_Clear_queue_Click(object sender, RoutedEventArgs e) {
            ExportQueue.Clear();
        }

        private async void LB_Queue_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (LB_Queue.SelectedItem is ExportFileInfoVideo selectedVideo) {
                var formattedText = await selectedVideo.GetVideoParams();
                TB_Data.Inlines.Clear();
                foreach (var line in formattedText.Split(new[] { Environment.NewLine }, StringSplitOptions.None)) {
                    if (line.Contains("🎥") || line.Contains("🔊") || line.Contains("📝")) {
                        TB_Data.Inlines.Add(new Run(line)
                        {
                            FontWeight = FontWeights.Bold,
                            Foreground = (Brush)FindResource("MaterialDesignBody")
                        });
                    }
                    else if (line.StartsWith("   ")) {
                        TB_Data.Inlines.Add(new Run(line)
                        {
                            Foreground = (Brush)FindResource("MaterialDesignBody")
                        });
                    }
                    else {
                        TB_Data.Inlines.Add(new Run(line)
                        {
                            Foreground = (Brush)FindResource("MaterialDesignBodyLight")
                        });
                    }
                    TB_Data.Inlines.Add(new LineBreak());
                }
                UC_Queue_Actions.Content = new VideoActions(selectedVideo);
            }
            else if (LB_Queue.SelectedItem is ExportFileInfoImage selectedImage) {
                var formattedText = await selectedImage.GetImageParams();
                TB_Data.Inlines.Clear();

                foreach (var line in formattedText.Split(new[] { Environment.NewLine }, StringSplitOptions.None)) {
                    if (line.Contains("🖼️") || line.Contains("📷") || line.Contains("📸") || line.Contains("🌍")) {
                        TB_Data.Inlines.Add(new Run(line)
                        {
                            FontWeight = FontWeights.Bold,
                            Foreground = (Brush)FindResource("MaterialDesignBody")
                        });
                    }
                    else if (line.StartsWith("   ") || line.StartsWith("   •") || line.StartsWith("   ◈")) {
                        TB_Data.Inlines.Add(new Run(line)
                        {
                            Foreground = (Brush)FindResource("MaterialDesignBody")
                        });
                    }
                    else {
                        TB_Data.Inlines.Add(new Run(line)
                        {
                            Foreground = (Brush)FindResource("MaterialDesignBodyLight")
                        });
                    }
                    TB_Data.Inlines.Add(new LineBreak());
                }

                UC_Queue_Actions.Content = new ImageActions(selectedImage);
            }
            else if (LB_Queue.SelectedItem is ExportFileInfoAudio selectedAudio) {
                var formattedText = await selectedAudio.GetAudioParams();
                TB_Data.Inlines.Clear();

                foreach (var line in formattedText.Split(new[] { Environment.NewLine }, StringSplitOptions.None)) {
                    if (line.Contains("🎵") || line.Contains("🔊") || line.Contains("🎧")) {
                        TB_Data.Inlines.Add(new Run(line)
                        {
                            FontWeight = FontWeights.Bold,
                            Foreground = (Brush)FindResource("MaterialDesignBody")
                        });
                    }
                    else if (line.StartsWith("   ") || line.StartsWith("   •") || line.StartsWith("   ◈")) {
                        TB_Data.Inlines.Add(new Run(line)
                        {
                            Foreground = (Brush)FindResource("MaterialDesignBody")
                        });
                    }
                    else {
                        TB_Data.Inlines.Add(new Run(line)
                        {
                            Foreground = (Brush)FindResource("MaterialDesignBodyLight")
                        });
                    }
                    TB_Data.Inlines.Add(new LineBreak());
                }

                //UC_Queue_Actions.Content = new AudioActions(selectedAudio);
            }
        }
        private CancellationTokenSource cancellationToken = new CancellationTokenSource();
        private async void Btn_Start_Export_Click(object sender, RoutedEventArgs e) {
            if (ExportQueue.Count == 0) {
                MessageBox.Show("Очередь экспорта пуста.", "Нет файлов для экспорта", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            ExportSettings exportSettings = GlobalSettings.Instance.ExportSettings;

            try {
                /*IsEnabled = false;*/
                /*LB_Queue.IsEnabled = false;*/
                Btn_Start_Export.Content = "Отмена";
                if (exportSettings.IsExporting) {
                    cancellationToken.Cancel();
                    Log.Update("Запрос отмены экспорта");
                    return;
                }
                cancellationToken = new CancellationTokenSource();
                bool success = false;
                try {
                    exportSettings.OnNextFileExport += ExportSettings_OnNextFileExport;
                    ExportSettings_OnNextFileExport(null, new ExportEventArgs { ExportedId = 0, TotalExportNum = ExportQueue.Count });
                    success = await exportSettings.DoExport(cancellationToken.Token);
                }
                catch (OperationCanceledException) {
                    Log.Update("Экспорт отменён пользователем");
                    TB_QueueLength.Text = "В процессе отмены экспорта";
                }
                finally {
                    cancellationToken.Dispose();
                    cancellationToken = null;
                    exportSettings.IsExporting = false;
                }
                if (success) {
                    TB_QueueLength.Text = "Экспорт успешно завершен!";
                    Log.Update("Экспорт успешно завершен!");

                    if (System.IO.Directory.Exists(exportSettings.TargetFolder)) {
                        if (exportSettings.TargetFolder.StartsWith(".")) System.Diagnostics.Process.Start("explorer.exe", Environment.CurrentDirectory + exportSettings.TargetFolder.Substring(1));

                        else System.Diagnostics.Process.Start("explorer.exe", exportSettings.TargetFolder.Replace("/", "\\"));
                    }
                }
                else {
                    TB_QueueLength.Text = "Экспорт завершен с ошибками.";
                    Log.Update("Экспорт завершен с ошибками.");
                }
            }
            catch (Exception ex) {
                TB_QueueLength.Text = "Ошибка при экспорте.";
                Log.Update($"Ошибка при экспорте: {ex.Message}");
                MessageBox.Show($"Произошла ошибка при экспорте: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally {
                if (!exportSettings.IsExporting) {
                    IsEnabled = true;
                    LB_Queue.IsEnabled = true;
                    LB_Queue.Items.Refresh();
                    Btn_Start_Export.Content = "Запустить очередь";
                    TB_QueueLength.Text = $"Очередь: {ExportQueue.Count} ожидающих заданий";
                    exportSettings.OnNextFileExport -= ExportSettings_OnNextFileExport;
                    TB_ExportNumText.Text = "Закончен";
                    PB_export.Value = 0;
                }

            }
        }

        private void ExportSettings_OnNextFileExport(object sender, ExportEventArgs e) {
            PB_export.Maximum = e.TotalExportNum;
            PB_export.Value = e.ExportedId;
            TB_ExportNumText.Text = $"{e.ExportedId} / {e.TotalExportNum}";
            LB_Queue.Items.Refresh();
            TB_QueueLength.Text = $"Очередь: {ExportQueue.Count} ожидающих заданий";
        }

        private void Btn_Delete_Click(object sender, RoutedEventArgs e) {
            if (LB_Queue.SelectedItem is ExportFileInfoBase selectedFile && !GlobalSettings.Instance.ExportSettings.IsExporting) {
                int deletedIndex = LB_Queue.SelectedIndex;
                ExportQueue.Dequeue(selectedFile);
            TB_QueueLength.Text = $"Очередь: {ExportQueue.Count} ожидающих заданий";
                LB_Queue.Items.Refresh();
                if (LB_Queue.Items.Count == 0) {
                    LB_Queue.SelectedIndex = -1;
                    return;
                }
                int newIndex = Math.Min(deletedIndex, LB_Queue.Items.Count - 1);
                LB_Queue.SelectedIndex = newIndex;
            }
        }
    }
}
