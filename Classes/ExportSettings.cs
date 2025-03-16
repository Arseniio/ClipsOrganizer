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
using static System.Net.Mime.MediaTypeNames;
using System.Windows.Controls;
using MetadataExtractor.Formats.Exif;
using MetadataExtractor.Formats.Jpeg;
using MetadataExtractor;
using System.Windows.Forms;

namespace ClipsOrganizer.Settings {
    public class ExportFileInfo : Item {
        public string OutputPath { get; set; }
        public string OutputFormat { get; set; }
        public int Quality { get; set; } = 5;
        public ExportFileInfo() { }
        public ExportFileInfo(Item item) {
            if (item == null)
                throw new ArgumentNullException(nameof(item));
            this.Name = item.Name;
            this.Path = item.Path;
            this.Date = item.Date;
            this.Color = item.Color;
        }
    }

    public class ExportFileInfoVideo : ExportFileInfo {
        public VideoCodec Codec { get; set; } = VideoCodec.H264_NVENC;
        public async Task<IMediaInfo> GetMediaInfoAsync() {
            return await FFmpeg.GetMediaInfo(this.Path);
        }
        public async Task<string> GetVideoParams() {
            var mediaInfo = await this.GetMediaInfoAsync();
            var stringBuilder = new StringBuilder();

            stringBuilder.AppendLine($"Источник: {this.Name}");
            stringBuilder.AppendLine($"Папка: {System.IO.Path.GetDirectoryName(this.Path)}");
            // Параметры видео (берем первый видеопоток)
            var videoStream = mediaInfo.VideoStreams.FirstOrDefault();
            if (videoStream != null) {
                var resolution = $"{videoStream.Width}x{videoStream.Height}";
                var frameRate = videoStream.Framerate.ToString("F2");
                stringBuilder.AppendLine($"Параметры: {resolution}@{frameRate}fps");
            }
            stringBuilder.AppendLine($"Шаблон: {this.OutputFormat}");
            if (videoStream != null) {
                var aspectRatio = (double)videoStream.Width / videoStream.Height;
                stringBuilder.AppendLine($"Изображение: {aspectRatio:0.00} ({videoStream.Width}x{videoStream.Height})");
            }
            foreach (var stream in mediaInfo.VideoStreams) {
                stringBuilder.AppendLine($"Видео: {stream.Codec} {stream.Bitrate}kbps");
            }
            foreach (var stream in mediaInfo.AudioStreams) {
                stringBuilder.AppendLine($"Аудио: {stream.Channels}ch {stream.Codec} {stream.Bitrate}kbps");
            }
            return stringBuilder.ToString();
        }
        public ExportFileInfoVideo() : base() { }
        public ExportFileInfoVideo(Item item) : base(item) { }
    }

    public class ExportFileInfoImage : ExportFileInfo {
        public ImageFormat Codec { get; set; } = ImageFormat.JPEG;
        public IReadOnlyList<MetadataExtractor.Directory> GetDirectories() {
            IReadOnlyList<MetadataExtractor.Directory> directories;
            try {
                directories = ImageMetadataReader.ReadMetadata(this.Path);
            }
            catch (Exception ex) {
                throw ex;
            }
            return directories;
        }
        [JsonIgnore]
        public string RawMetadataDisplay {
            get {
                var directories = GetDirectories();
                if (directories == null)
                    return string.Empty;
                var sb = new StringBuilder();
                foreach (var directory in directories) {
                    sb.AppendLine(directory.Name);
                    foreach (var tag in directory.Tags) {
                        if (!tag.Name.Contains("Unknown"))
                            sb.AppendLine($"{tag.Name}: {tag.Description}");
                    }
                    sb.AppendLine(); // для разделения блоков
                }
                return sb.ToString();
            }
        }

        public async Task<string> GetImageParams() {
            return await Task.Run(() =>
            {
                var stringBuilder = new StringBuilder();
                stringBuilder.AppendLine($"Источник: {this.Name}");
                stringBuilder.AppendLine($"Папка: {System.IO.Path.GetDirectoryName(this.Path)}");
                IReadOnlyList<MetadataExtractor.Directory> directories;
                try {
                    directories = ImageMetadataReader.ReadMetadata(this.Path);
                }
                catch (Exception ex) {
                    return $"Ошибка при чтении метаданных: {ex.Message}";
                }
                var fileTypeDir = directories.FirstOrDefault(d => d.Name.Contains("File Type"));
                string fileFormat = fileTypeDir?.GetDescription(1) ?? "Unknown";
                stringBuilder.AppendLine($"Формат файла: {fileFormat}");
                var jpegDir = directories.OfType<JpegDirectory>().FirstOrDefault();
                if (jpegDir != null) {
                    int? width = jpegDir.GetImageWidth();
                    int? height = jpegDir.GetImageHeight();
                    if (width.HasValue && height.HasValue)
                        stringBuilder.AppendLine($"Разрешение: {width}x{height}");
                }
                else {
                    var ifd0 = directories.OfType<ExifIfd0Directory>().FirstOrDefault();
                    if (ifd0 != null &&
                        ifd0.TryGetInt32(ExifDirectoryBase.TagImageWidth, out int width) &&
                        ifd0.TryGetInt32(ExifDirectoryBase.TagImageHeight, out int height)) {
                        stringBuilder.AppendLine($"Разрешение: {width}x{height}");
                    }
                }

                var exifSub = directories.OfType<ExifSubIfdDirectory>().FirstOrDefault();
                if (exifSub != null) {
                    if (exifSub.ContainsTag(ExifDirectoryBase.TagIsoEquivalent))
                        stringBuilder.AppendLine($"ISO: {exifSub.GetDescription(ExifDirectoryBase.TagIsoEquivalent)}");
                    if (exifSub.ContainsTag(ExifDirectoryBase.TagExposureTime))
                        stringBuilder.AppendLine($"Выдержка: {exifSub.GetDescription(ExifDirectoryBase.TagExposureTime)}");
                    if (exifSub.ContainsTag(ExifDirectoryBase.TagFNumber))
                        stringBuilder.AppendLine($"Диафрагма: {exifSub.GetDescription(ExifDirectoryBase.TagFNumber)}");
                    if (exifSub.ContainsTag(ExifDirectoryBase.TagFocalLength))
                        stringBuilder.AppendLine($"Фокусное расстояние: {exifSub.GetDescription(ExifDirectoryBase.TagFocalLength)}");
                    if (exifSub.ContainsTag(ExifDirectoryBase.TagWhiteBalance))
                        stringBuilder.AppendLine($"Баланс белого: {exifSub.GetDescription(ExifDirectoryBase.TagWhiteBalance)}");
                    if (exifSub.ContainsTag(ExifDirectoryBase.TagDateTimeOriginal))
                        stringBuilder.AppendLine($"Дата съемки: {exifSub.GetDescription(ExifDirectoryBase.TagDateTimeOriginal)}");
                }
                var exifIfd0 = directories.OfType<ExifIfd0Directory>().FirstOrDefault();
                if (exifIfd0 != null) {
                    if (exifIfd0.ContainsTag(ExifDirectoryBase.TagMake))
                        stringBuilder.AppendLine($"Производитель камеры: {exifIfd0.GetDescription(ExifDirectoryBase.TagMake)}");
                    if (exifIfd0.ContainsTag(ExifDirectoryBase.TagModel))
                        stringBuilder.AppendLine($"Модель камеры: {exifIfd0.GetDescription(ExifDirectoryBase.TagModel)}");
                }
                var gpsDir = directories.OfType<GpsDirectory>().FirstOrDefault();
                if (gpsDir != null) {
                    var location = gpsDir.GetGeoLocation();
                    if (location != null)
                        stringBuilder.AppendLine($"GPS: {location.Latitude:0.000000}, {location.Longitude:0.000000}");
                }
                return stringBuilder.ToString();
            });
        }
        public int ExportWidth { get; set; } = 1920;
        public int ExportHeight { get; set; } = 1080;
        public bool PreserveMetadata { get; set; } = true;
        public string ColorProfile { get; set; } = "sRGB"; // Варианты: sRGB, Adobe RGB, ProPhoto RGB
        public int CompressionLevel { get; set; } = 75; // Для PNG (1-100)
        public ExportFileInfoImage() : base() { }
        public ExportFileInfoImage(Item item) : base(item) { }
    }
    public static class ExportQueue {

        public static readonly List<ExportFileInfo> _queue = new List<ExportFileInfo>();
        private static readonly object _lock = new object();

        public static int Count {
            get {
                lock (_lock) {
                    return _queue.Count;
                }
            }
        }

        public static void Enqueue(ExportFileInfo item) {
            lock (_lock) {
                _queue.Add(item);
            }
        }

        public static ExportFileInfo Dequeue() {
            lock (_lock) {
                if (_queue.Count == 0)
                    throw new InvalidOperationException("Queue is empty");
                var item = _queue[0];
                _queue.RemoveAt(0);
                return item;
            }
        }

        public static ExportFileInfo Peek() {
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
