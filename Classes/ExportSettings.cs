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
        // Video Settings
        public int VideoBitrate { get; set; } = 0; // 0 = auto
        public double CRF { get; set; } = 23.0; // For quality-based encoding
        public ResolutionType Resolution { get; set; } = ResolutionType.Original;
        public CustomResolution CustomResolution { get; set; }
        public double? FrameRate { get; set; } // null = source fps
        public bool TwoPassEncoding { get; set; }

        // Audio Settings
        public AudioCodec AudioCodec { get; set; } = AudioCodec.AAC;
        public int AudioBitrate { get; set; } = 128; // kbps
        public int AudioChannels { get; set; } = 2;
        public bool NormalizeAudio { get; set; }

        public TimeSpan TrimStart { get; set; }
        public TimeSpan TrimEnd { get; set; }

        // Hardware Acceleration
        public HardwareAccelerationType HardwareAcceleration { get; set; }
        public string GPUDeviceId { get; set; } // For multi-GPU systems

        // Advanced
        public bool CopyMetadata { get; set; } = true;

        // Добавьте методы для работы с Xabe.FFmpeg
        public async Task<IMediaInfo> GetMediaInfoAsync() {
            return await FFmpeg.GetMediaInfo(Path);
        }
        public ExportFileInfoVideo() { }
        public ExportFileInfoVideo(Item item) : base(item) { }
        public async Task<string> GetVideoParams() {
            var mediaInfo = await GetMediaInfoAsync();
            var stringBuilder = new StringBuilder();

            // Основная информация
            stringBuilder.AppendLine($"📁 Файл: {System.IO.Path.GetFileName(Path)}");
            stringBuilder.AppendLine($"📂 Папка: {System.IO.Path.GetDirectoryName(Path)}");
            stringBuilder.AppendLine($"📅 Дата: {Date:dd.MM.yyyy HH:mm}");
            stringBuilder.AppendLine("───────────────────────");

            // Видеопотоки
            foreach (var stream in mediaInfo.VideoStreams) {
                stringBuilder.AppendLine($"🎥 Видео #{stream.Index}");
                stringBuilder.AppendLine($"   Кодек: {stream.Codec}");
                stringBuilder.AppendLine($"   Разрешение: {stream.Width}x{stream.Height}");
                stringBuilder.AppendLine($"   Частота кадров: {stream.Framerate:F2} fps");
                stringBuilder.AppendLine($"   Битрейт: {stream.Bitrate / 1000} kbps");
                stringBuilder.AppendLine($"   Длительность: {stream.Duration:h\\:mm\\:ss}");
                stringBuilder.AppendLine("───────────────────────");
            }

            // Аудиопотоки
            foreach (var stream in mediaInfo.AudioStreams) {
                stringBuilder.AppendLine($"🔊 Аудио #{stream.Index}");
                stringBuilder.AppendLine($"   Кодек: {stream.Codec}");
                stringBuilder.AppendLine($"   Каналы: {stream.Channels}");
                stringBuilder.AppendLine($"   Битрейт: {stream.Bitrate / 1000} kbps");
                stringBuilder.AppendLine($"   Язык: {stream.Language ?? "Не указан"}");
                stringBuilder.AppendLine("───────────────────────");
            }

            return stringBuilder.ToString();
        }

    }

    // Вспомогательные типы
    public enum AudioCodec { AAC, MP3, Opus, FLAC }
    public enum ResolutionType { Original, _480p, _720p, _1080p, _4K, Custom }

    public class CustomResolution {
        public int Width { get; set; }
        public int Height { get; set; }
        public bool KeepAspectRatio { get; set; } = true;
    }

    public class CropSettings {
        public bool AutoCrop { get; set; }
        public int Left { get; set; }
        public int Right { get; set; }
        public int Top { get; set; }
        public int Bottom { get; set; }
    }

    public enum HardwareAccelerationType { None, NVENC, QSV, AMF }

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
                var sb = new StringBuilder();
                sb.AppendLine($"🖼️ Файл: {System.IO.Path.GetFileName(Path)}");
                sb.AppendLine($"📂 Папка: {System.IO.Path.GetDirectoryName(Path)}");
                sb.AppendLine($"📅 Дата: {Date:dd.MM.yyyy HH:mm}");
                sb.AppendLine("───────────────────────");

                try {
                    var directories = ImageMetadataReader.ReadMetadata(Path);

                    // Основные характеристики
                    var fileTypeDir = directories.FirstOrDefault(d => d.Name.Contains("File Type"));
                    sb.AppendLine($"📝 Формат: {fileTypeDir?.GetDescription(1) ?? "Не определен"}");

                    // Разрешение изображения
                    var resolution = GetResolution(directories);
                    if (!string.IsNullOrEmpty(resolution))
                        sb.AppendLine($"🖥️ Разрешение: {resolution}");

                    // EXIF параметры
                    var exifData = GetExifData(directories);
                    if (exifData.Count > 0) {
                        sb.AppendLine("📷 Параметры съемки:");
                        foreach (var item in exifData) {
                            sb.AppendLine($"   • {item.Key}: {item.Value}");
                        }
                        sb.AppendLine("───────────────────────");
                    }

                    // Информация о камере
                    var cameraInfo = GetCameraInfo(directories);
                    if (cameraInfo.Count > 0) {
                        sb.AppendLine("📸 Камера:");
                        foreach (var item in cameraInfo) {
                            sb.AppendLine($"   ◈ {item.Key}: {item.Value}");
                        }
                        sb.AppendLine("───────────────────────");
                    }

                    // GPS координаты
                    var gps = GetGpsInfo(directories);
                    if (!string.IsNullOrEmpty(gps))
                        sb.AppendLine($"🌍 Координаты: {gps}");

                }
                catch (Exception ex) {
                    sb.AppendLine($"❌ Ошибка чтения метаданных: {ex.Message}");
                }

                return sb.ToString();
            });
        }

        private Dictionary<string, string> GetExifData(IEnumerable<MetadataExtractor.Directory> directories) {
            var result = new Dictionary<string, string>();
            var exifSub = directories.OfType<ExifSubIfdDirectory>().FirstOrDefault();

            if (exifSub != null) {
                AddIfExists(exifSub, ExifDirectoryBase.TagIsoEquivalent, "ISO", result);
                AddIfExists(exifSub, ExifDirectoryBase.TagExposureTime, "Выдержка", result);
                AddIfExists(exifSub, ExifDirectoryBase.TagFNumber, "Диафрагма", result);
                AddIfExists(exifSub, ExifDirectoryBase.TagFocalLength, "Фокусное расстояние", result);
                AddIfExists(exifSub, ExifDirectoryBase.TagWhiteBalance, "Баланс белого", result);
                AddIfExists(exifSub, ExifDirectoryBase.TagDateTimeOriginal, "Дата съемки", result);
            }

            return result;
        }

        private Dictionary<string, string> GetCameraInfo(IEnumerable<MetadataExtractor.Directory> directories) {
            var result = new Dictionary<string, string>();
            var exifIfd0 = directories.OfType<ExifIfd0Directory>().FirstOrDefault();

            if (exifIfd0 != null) {
                AddIfExists(exifIfd0, ExifDirectoryBase.TagMake, "Производитель", result);
                AddIfExists(exifIfd0, ExifDirectoryBase.TagModel, "Модель", result);
            }

            return result;
        }

        private string GetResolution(IEnumerable<MetadataExtractor.Directory> directories) {
            var jpegDir = directories.OfType<JpegDirectory>().FirstOrDefault();
            if (jpegDir != null) {
                return $"{jpegDir.GetImageWidth()}x{jpegDir.GetImageHeight()}";
            }

            var ifd0 = directories.OfType<ExifIfd0Directory>().FirstOrDefault();
            if (ifd0 != null &&
                ifd0.TryGetInt32(ExifDirectoryBase.TagImageWidth, out int width) &&
                ifd0.TryGetInt32(ExifDirectoryBase.TagImageHeight, out int height)) {
                return $"{width}x{height}";
            }

            return null;
        }

        private string GetGpsInfo(IEnumerable<MetadataExtractor.Directory> directories) {
            var gpsDir = directories.OfType<GpsDirectory>().FirstOrDefault();
            var location = gpsDir?.GetGeoLocation();
            return location != null ?
                $"{location.Latitude:0.#####}°, {location.Longitude:0.#####}°" :
                null;
        }

        private void AddIfExists<T>(T directory, int tag, string name, Dictionary<string, string> dict)
            where T : MetadataExtractor.Directory {
            if (directory?.ContainsTag(tag) == true) {
                dict[name] = directory.GetDescription(tag)
                    .Replace(" sec", "с")
                    .Replace(" mm", "мм")
                    .Replace(" f/", "f/");
            }
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
