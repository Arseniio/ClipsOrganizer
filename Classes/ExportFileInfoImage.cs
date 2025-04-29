using ClipsOrganizer.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using System.Text;
using System.Threading.Tasks;
using MetadataExtractor.Formats.Exif;
using MetadataExtractor.Formats.Jpeg;
using MetadataExtractor;
using ClipsOrganizer.Settings;

namespace ClipsOrganizer.Model {
    public class ExportFileInfoImage : ExportFileInfoBase {
        public bool ProcessExport { get; set; } = false;
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
        private (int, int) GetResolutionInt() {
            if (Path == null) return (0, 0);
            var directories = ImageMetadataReader.ReadMetadata(Path);
            var jpegDir = directories.OfType<JpegDirectory>().FirstOrDefault();
            if (jpegDir != null) {
                return (jpegDir.GetImageWidth(), jpegDir.GetImageHeight());
            }

            var ifd0 = directories.OfType<ExifIfd0Directory>().FirstOrDefault();
            if (ifd0 != null &&
                ifd0.TryGetInt32(ExifDirectoryBase.TagImageWidth, out int width) &&
                ifd0.TryGetInt32(ExifDirectoryBase.TagImageHeight, out int height)) {
                return (width, height);
            }
            return (0, 0);
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
        public int ExportWidth { get; set; } = 1760;
        public int ExportHeight { get; set; } = 1080;
        public bool PreserveMetadata { get; set; } = true;
        public string ColorProfile { get; set; } = "sRGB"; // Варианты: sRGB, Adobe RGB, ProPhoto RGB
        public int CompressionLevel { get; set; } = 75; // Для PNG (1-100)
        public ExportFileInfoImage() : base() {
            ExportWidth = GetResolutionInt().Item1;
            ExportHeight = GetResolutionInt().Item2;
        }
        public ExportFileInfoImage(Item item) : base(item) {
            ExportWidth = GetResolutionInt().Item1;
            ExportHeight = GetResolutionInt().Item2;
            CompressionLevel = GlobalSettings.Instance.DefaultImageExport.CompressionLevel;
            ColorProfile = GlobalSettings.Instance.DefaultImageExport.ColorProfile;
            OutputFormat = GlobalSettings.Instance.DefaultImageExport.OutputFormat;
            ProcessExport = GlobalSettings.Instance.DefaultImageExport.ProcessExport;

        }
    }
}
