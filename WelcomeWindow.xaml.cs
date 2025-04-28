using ClipsOrganizer.Classes;
using SharpCompress.Archives;
using SharpCompress.Archives.SevenZip;
using SharpCompress.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace ClipsOrganizer {
    /// <summary>
    /// Логика взаимодействия для WelcomeWindow.xaml
    /// </summary>
    public partial class WelcomeWindow : Window, INotifyPropertyChanged {

        private bool _ffmpegFound;
        private bool _ffprobeFound;
        private bool _showManualPath;

        public bool ShowManualPath {
            get => _showManualPath;
            set {
                _showManualPath = value;
                OnPropertyChanged();
            }
        }

        public string ClipsPath { get; private set; }
        public string ProfileName { get; private set; }
        public string FfmpegPath => TbFfmpegPath.Text;

        public WelcomeWindow() {
            InitializeComponent();
            Loaded += async (s, e) => await CheckFfmpegPresence();
            DataContext = this;
        }

        private void UpdateStatus(bool ffmpegFound, bool ffprobeFound) {
            _ffmpegFound = ffmpegFound;
            _ffprobeFound = ffprobeFound;

            FfmpegStatus.Text = ffmpegFound ? "Найден" : "Не найден";
            FfmpegIcon.Text = ffmpegFound ? "✅" : "❌";
            FfmpegStatus.Foreground = ffmpegFound ? Brushes.Green : Brushes.Red;

            FfprobeStatus.Text = ffprobeFound ? "Найден" : "Не найден";
            FfprobeIcon.Text = ffprobeFound ? "✅" : "❌";
            FfprobeStatus.Foreground = ffprobeFound ? Brushes.Green : Brushes.Red;
        }

        private async void TbFfmpegPath_TextChanged(object sender, TextChangedEventArgs e) {
            if (Directory.Exists(TbFfmpegPath.Text)) {
                var (ffmpeg, ffprobe) = await FfmpegHelper.CheckCustomFfmpeg(TbFfmpegPath.Text);
                UpdateStatus(ffmpeg, ffprobe);
            }
            else {
                UpdateStatus(false, false);
            }
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e) {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                TbFfmpegPath.Text = dialog.SelectedPath;
            }
        }

        async Task<bool> DownloadFfmpeg(IProgress<double> progress) {
            string url = "https://www.gyan.dev/ffmpeg/builds/ffmpeg-git-essentials.7z";
            string destinationPath = "./ffmpeg/ffmpeg-git-essentials.7z";
            if (File.Exists(destinationPath)) {
                MessageBox.Show("Архив ffmpeg существует, попытка распаковать.");
                return true;
            }
            try {
                if (!Directory.Exists("./ffmpeg")) Directory.CreateDirectory("./ffmpeg");

                using (HttpClient client = new HttpClient())
                using (HttpResponseMessage response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead)) {
                    response.EnsureSuccessStatusCode();
                    long total = response.Content.Headers.ContentLength ?? -1L;
                    bool canReport = total != -1 && progress != null;
                    using (FileStream fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None))
                    using (Stream contentStream = await response.Content.ReadAsStreamAsync()) {
                        byte[] buffer = new byte[8192];
                        long totalRead = 0;
                        int read;
                        while ((read = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0) {
                            await fileStream.WriteAsync(buffer, 0, read);
                            totalRead += read;
                            if (canReport) {
                                progress.Report((double)totalRead / total * 100);
                            }
                        }
                    }
                }
            }
            catch (Exception ex) {
                var result = MessageBox.Show("Невозможно автоматически скачать ffmpeg, открыть сайт для скачивания?", "Ошибка", MessageBoxButton.YesNo);
                if (result == MessageBoxResult.Yes) {
                    Process.Start(new ProcessStartInfo("https://ffmpeg.org/download.html") { UseShellExecute = true });
                }
                return false;
            }
            MessageBox.Show("FFmpeg успешно загружен");
            return true;
        }

        async Task<bool> ExtractFfmpeg(string archivePath, string outputFolder, IProgress<double> progress) {
            try {
                using (var archive = SevenZipArchive.Open(archivePath)) {
                    var entries = archive.Entries.Where(entry =>
                        !entry.IsDirectory &&
                        (entry.Key.EndsWith("bin/ffmpeg.exe", StringComparison.OrdinalIgnoreCase) ||
                         entry.Key.EndsWith("bin/ffprobe.exe", StringComparison.OrdinalIgnoreCase))
                    ).ToList();

                    long totalFiles = entries.Count;
                    long extractedFiles = 0;

                    foreach (var entry in entries) {
                        string destinationPath = Path.Combine(outputFolder, Path.GetFileName(entry.Key));
                        await Task.Run(() => entry.WriteToFile(destinationPath, new ExtractionOptions { Overwrite = true }));
                        extractedFiles++;

                        progress.Report((double)extractedFiles / totalFiles * 100);
                    }
                }

                MessageBox.Show("Распаковка завершена.");
                return true;
            }
            catch (Exception ex) {
                Console.WriteLine($"Ошибка при распаковке: {ex.Message}");
                return false;
            }
        }

        Progress<double> progress = new Progress<double>();
        private async void DownloadButton_Click(object sender, RoutedEventArgs e) {
            progress.ProgressChanged += Progress_ProgressChanged;
            if (await DownloadFfmpeg(progress) && await ExtractFfmpeg("./ffmpeg/ffmpeg-git-essentials.7z", "./ffmpeg", progress)) {
                TbFfmpegPath.Text = Path.Combine(Environment.CurrentDirectory, "ffmpeg");
            }
        }

        private void Progress_ProgressChanged(object sender, double e) {
            // Обновляем прогресс в кнопке
            MaterialDesignThemes.Wpf.ButtonProgressAssist.SetValue(Btn_Download, e);
        }


        private void SelectFolder_Click(object sender, RoutedEventArgs e) {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                TbPath.Text = dialog.SelectedPath;
            }
        }

        private void BtnContinue_Click(object sender, RoutedEventArgs e) {
            if (!ValidateInputs()) return;

            DialogResult = true;
            Close();
        }

        private bool ValidateInputs() {
            var errors = new StringBuilder();

            if (!_ffmpegFound || !_ffprobeFound)
                errors.AppendLine("Требуются FFmpeg и FFprobe!");

            if (!Directory.Exists(TbPath.Text))
                errors.AppendLine("Неверная рабочая директория");

            if (string.IsNullOrWhiteSpace(TbProfile.Text))
                errors.AppendLine("Имя профиля обязательно");

            if (errors.Length > 0) {
                MessageBox.Show(errors.ToString(), "Ошибка проверки", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            ClipsPath = TbPath.Text;
            ProfileName = TbProfile.Text;
            return true;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        private async Task CheckFfmpegPresence() {
            var (ffmpegExists, ffprobeExists) = await FfmpegHelper.CheckSystemFfmpeg();

            UpdateStatus(ffmpegExists, ffprobeExists);
            ShowManualPath = true;
        }

        public static class FfmpegHelper {
            public static async Task<(bool ffmpegExists, bool ffprobeExists)> CheckSystemFfmpeg() {
                try {
                    var ffmpeg = await CheckToolExists("ffmpeg");
                    var ffprobe = await CheckToolExists("ffprobe");
                    return (ffmpeg, ffprobe);
                }
                catch {
                    return (false, false);
                }
            }
            public static async Task<(bool ffmpegExists, bool ffprobeExists)> CheckCustomFfmpeg(string directory) {
                var ffmpegPath = Path.Combine(directory, "ffmpeg.exe");
                var ffprobePath = Path.Combine(directory, "ffprobe.exe");

                return (File.Exists(ffmpegPath), File.Exists(ffprobePath));
            }
            private static async Task<bool> CheckToolExists(string toolName) {
                try {
                    return false;
                    using var process = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = toolName,
                            Arguments = "-version",
                            UseShellExecute = false,
                            CreateNoWindow = true,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true
                        }
                    };
                    process.Start();
                    await process.WaitForExitAsync();
                    return process.ExitCode == 0;
                }
                catch {
                    return false;
                }
            }
        }
    }
}
