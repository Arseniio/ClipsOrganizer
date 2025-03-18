using ClipsOrganizer.Model;
using ClipsOrganizer.Settings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using System.Text;
using System.Threading.Tasks;
using Xabe.FFmpeg;

namespace ClipsOrganizer.Settings {

   
    public static class ExportQueue {

        public static readonly List<ExportFileInfoBase> _queue = new List<ExportFileInfoBase>();
        private static readonly object _lock = new object();

        public static int Count {
            get {
                lock (_lock) {
                    return _queue.Count;
                }
            }
        }

        public static void Enqueue(ExportFileInfoBase item) {
            lock (_lock) {
                _queue.Add(item);
            }
        }

        public static ExportFileInfoBase Dequeue() {
            lock (_lock) {
                if (_queue.Count == 0)
                    throw new InvalidOperationException("Queue is empty");
                var item = _queue[0];
                _queue.RemoveAt(0);
                return item;
            }
        }

        public static ExportFileInfoBase Peek() {
            lock (_lock) {
                return _queue.Count > 0 ? _queue[0] : null;
            }
        }

        public static void Clear() {
            lock (_lock) {
                _queue.Clear();
            }
        }
    }

    public class ExportSettings {
        //<ListBoxItem Tag = "ExportLocal" > Экспорт в папку</ListBoxItem>
        //<ListBoxItem Tag = "EncodeFiles" > Перекодирование файлов</ListBoxItem>
        //<ListBoxItem Tag = "UploadToCloud" > Выгрузка на файлообменник</ListBoxItem>
        //<ListBoxItem Tag = "CollectionSettings" > Настройки коллекции</ListBoxItem>
        //<ListBoxItem Tag = "FileSelection" > Выбор файлов для экспорта</ListBoxItem>

        //Export to cloud
        public bool UploadToCloud { get; set; }
        public string CloudService { get; set; }
        public string CloudFolderPath { get; set; }

        //Encode settings
        public bool EncodeEnabled { get; set; }
        public bool OverrideEncode { get; set; }
        public bool EnableParallelExport { get; set; }
        public int MaxParallelTasks { get; set; }
        public int MaxFFmpegThreads { get; set; }
        public VideoCodec EncodeFormat { get; set; }
        public int EncodeBitrate { get; set; }

        //Collection export settings
        [JsonIgnore]
        public List<string> FilesToExport { get; set; }
        public bool DeleteFilesAfterExport { get; set; }
        //General export settings
        public string TargetFolder { get; set; } = "./Temp";
        public bool EnableLogging { get; set; }
        public string LogFilePath { get; set; }
        public int MaxFileSizeMB { get; set; }
        [JsonIgnore]
        public string TotalFileSizeAfterExport {
            get {
                return (MainWindow.CurrentProfile.Collections
                    .Where(p => p.IsSelected)
                    .SelectMany(p => p.Files)
                    .Sum(a => File.Exists(a.Path) ? new FileInfo(a.Path).Length : 0) / (1024.0 * 1024.0)).ToString("F2");
            }
        }
        public string TotalFileSizeAfterExportWithEncoding {
            get {
                double totalSizeMb = MainWindow.CurrentProfile.Collections
                    .Where(p => p.IsSelected)
                    .SelectMany(p => p.Files)
                    .Sum(file =>
                    {
                        using (var shell = Microsoft.WindowsAPICodePack.Shell.ShellObject.FromParsingName(file.Path)) {
                            var durationProperty = shell.Properties.System.Media.Duration;
                            if (durationProperty?.Value != null) {
                                var duration = TimeSpan.FromTicks((long)(durationProperty.Value.Value));
                                double bitrate = this.EncodeBitrate;
                                double fileSizeBytes = (bitrate * 1000 * duration.TotalSeconds) / 8;
                                return fileSizeBytes / (1024 * 1024);
                            }
                        }
                        return 0;
                    });

                return $"{totalSizeMb:F2}";
            }
        }

        //Advanced general
        public bool UseRegex { get; set; }
        public string FileNameTemplate { get; set; }
        public string ExportFileNameTepmlate { get; set; }


        public async Task<bool> DoExport() {
            try {
                if (string.IsNullOrWhiteSpace(TargetFolder)) {
                    throw new InvalidOperationException("Не указана выходная папка.");
                }

                if (!System.IO.Directory.Exists(TargetFolder)) {
                    System.IO.Directory.CreateDirectory(TargetFolder);
                }

                // Лог начала процесса
                if (EnableLogging && !string.IsNullOrEmpty(LogFilePath)) {
                    File.AppendAllText(LogFilePath, $"Export started at {DateTime.Now}\n");
                    Log.Update($"Export started at {DateTime.Now}\n");
                }

                //Работа с файлами для экспорта
                //var filesToProcess = ExportAllFiles ? FilesToExport : FilesToExport?.Where(file => File.Exists(file)).ToList();

                //if (filesToProcess == null || filesToProcess.Count == 0) {
                //throw new InvalidOperationException("No files selected for export.");
                //}

                foreach (var ExportCollection in MainWindow.CurrentProfile.Collections) {
                    int filesCount = 0;
                    if (!ExportCollection.IsSelected) continue;
                    if (EncodeEnabled && EnableParallelExport) {
                        for (int offset = 0; offset < ExportCollection.Files.Count; offset += MaxParallelTasks) {
                            var count = Math.Min(MaxParallelTasks, ExportCollection.Files.Count - offset);

                            Log.Update($"Launching ffmpeg for batch starting at offset {offset}");
                            var encodingTasks = ExportCollection.Files
                                .GetRange(offset, count)
                                .Select(async p =>
                                {
                                    var FFmpegLocal = GlobalSettings.Instance.FFmpegInit();
                                    Log.Update($"File: {p.Path}");
                                    Log.Update($"Destination: {Path.Combine(TargetFolder, Path.GetFileName(p.Path))}");
                                    await FFmpegLocal.StartEncodingAsync(
                                        p.Path,
                                        Path.Combine(TargetFolder, Path.GetFileName(p.Path)),
                                        this.EncodeFormat,
                                        this.EncodeBitrate
                                    );
                                });
                            await Task.WhenAll(encodingTasks);
                        }
                    }

                    foreach (var file in ExportCollection.Files) {

                        var destinationPath = Path.Combine(TargetFolder, Path.GetFileName(file.Path));
                        if (EncodeEnabled && !EnableParallelExport) {
                            if (!GlobalSettings.Instance.ffmpegManager.StartEncodingAsync(file.Path, destinationPath.Replace("\\", "/"), this.EncodeFormat, this.EncodeBitrate).Result) {
                                throw new Exception($"Encoding failed for file: {file.Path}");
                            }
                        }
                        else if (!EncodeEnabled) {
                            File.Copy(file.Path, destinationPath, OverrideEncode);
                        }
                        if (DeleteFilesAfterExport) {
                            File.Delete(file.Path);
                        }
                        if (EnableLogging && !string.IsNullOrEmpty(LogFilePath)) {
                            File.AppendAllText(LogFilePath, $"Exported file: {file} to {destinationPath}\n");
                        }
                    }
                }

                // Логирование завершения
                if (EnableLogging && !string.IsNullOrEmpty(LogFilePath)) {
                    File.AppendAllText(LogFilePath, $"Export completed at {DateTime.Now}\n");
                }

                return true;
            }
            catch (Exception) {
                throw;
            }
        }
    }
}
