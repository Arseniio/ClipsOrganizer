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
using System.Threading;
using System.Windows;
using ImageMagick;

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
        // Зарезервировано для будущей реализации выгрузки файлов в облако
        public bool UploadToCloud { get; set; }
        // Строка, определяющая сервис облачного хранения (например, "Dropbox", "GoogleDrive")
        public string CloudService { get; set; }
        // Путь к папке в облачном хранилище
        public string CloudFolderPath { get; set; }

        //Encode settings
        public bool EncodeEnabled { get; set; }
        public bool OverrideEncode { get; set; }
        public bool EnableParallelExport { get; set; }
        public int MaxParallelTasks { get; set; }
        // Количество потоков FFmpeg для каждой задачи кодирования (пока не используется)
        public int MaxFFmpegThreads { get; set; }
        //Общее и стандартное для всех файлов 
        public VideoCodec EncodeFormat { get; set; }
        public int EncodeBitrate { get; set; }

        //Collection export settings
        public bool DeleteFilesAfterExport { get; set; }
        //General export settings
        public string TargetFolder { get; set; } = "./Temp";
        // Максимальный размер экспортируемого файла в МБ (пока не используется)
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
        // Флаг использования регулярных выражений для имён файлов (пока не используется)
        public bool UseRegex { get; set; }
        // Шаблон имени файла при экспорте (пока не используется)
        public string FileNameTemplate { get; set; }
        // Шаблон имени экспортируемого файла (пока не используется, опечатка в названии)
        public string ExportFileNameTepmlate { get; set; }

        // Новый метод для экспорта одного файла
        public async Task<bool> ExportFile(ExportFileInfoBase fileInfo, CancellationToken cancellationToken = default) {
            if (fileInfo == null) {
                Log.Update("Ошибка: Файл для экспорта не задан");
                return false;
            }

            if (string.IsNullOrWhiteSpace(TargetFolder)) {
                Log.Update("Ошибка: Не указана выходная папка");
                return false;
            }

            if (!Directory.Exists(TargetFolder)) {
                Directory.CreateDirectory(TargetFolder);
                Log.Update($"Создана папка экспорта: {TargetFolder}");
            }
            Log.Update($"Начало экспорта файла: {Path.GetFileName(fileInfo.Path)}");

            string destinationPath = fileInfo.OutputPath;
            if (string.IsNullOrEmpty(destinationPath)) {
                if (TargetFolder.StartsWith(".")) {
                    TargetFolder = Environment.CurrentDirectory + TargetFolder.Substring(1);
                }
                destinationPath = Path.Combine(TargetFolder, Path.GetFileName(fileInfo.Path));
            }
            fileInfo.OutputPath = destinationPath;
            try {
                // Определение типа файла
                if (fileInfo is ExportFileInfoVideo videoInfo) {
                    return await ExportVideoFile(videoInfo, destinationPath, cancellationToken);
                }
                else if (fileInfo is ExportFileInfoImage imageInfo) {
                    return await ExportImageFile(imageInfo, destinationPath, cancellationToken);
                }
                else {
                    ExportGenericFile(fileInfo, destinationPath, cancellationToken);
                }
            }
            catch (Exception ex) {
                Log.Update($"Ошибка экспорта файла {Path.GetFileName(fileInfo.Path)}: {ex.Message}");
                return false;
            }
            finally {
                if (DeleteFilesAfterExport && File.Exists(fileInfo.Path)) {
                    try {
                        File.Delete(fileInfo.Path);
                        Log.Update($"Файл удален после экспорта: {Path.GetFileName(fileInfo.Path)}");
                    }
                    catch (Exception ex) {
                        Log.Update($"Ошибка при удалении файла после экспорта: {ex.Message}");
                    }
                }
            }
            return true;
        }

        private async Task<bool> ExportVideoFile(ExportFileInfoVideo videoInfo, string destinationPath, CancellationToken cancellationToken) {
            Log.Update($"Экспорт видео: {Path.GetFileName(videoInfo.Path)}");

            try {
                var ffmpegManager = GlobalSettings.Instance.FFmpegInit();
                VideoCodec codec;
                // Настройка кодека и битрейта на основе пользовательских настроек
                if (videoInfo.VideoCodec == VideoCodec.Unknown) {
                    videoInfo.VideoCodec = this.EncodeFormat;
                }
                int bitrate = videoInfo.VideoBitrate > 0 ? videoInfo.VideoBitrate : this.EncodeBitrate;

                // Настройка отрезка времени, если установлены TrimStart и TrimEnd
                TimeSpan? startTime = null;
                TimeSpan? endTime = null;

                if (videoInfo.TrimStart.TotalSeconds > 0) {
                    startTime = videoInfo.TrimStart;
                }

                if (videoInfo.TrimEnd.TotalSeconds > 0) {
                    endTime = videoInfo.TrimEnd;
                }

                //bool success = await ffmpegManager.StartEncodingAsync(
                //    videoInfo.Path,
                //    destinationPath,
                //    codec,
                //    bitrate,
                //    startTime,
                //    endTime
                //);
                bool success = await ffmpegManager.StartEncodingAsync(
                    videoInfo,destinationPath,bitrate
                );

                if (success) {
                    Log.Update($"Видео успешно экспортировано: {Path.GetFileName(destinationPath)}");
                }
                else {
                    Log.Update($"Ошибка при экспорте видео: {Path.GetFileName(videoInfo.Path)}");
                }

                return success;
            }
            catch (Exception ex) {
                Log.Update($"Ошибка экспорта видео: {ex.Message}");
                return false;
            }
        }


        private async Task<bool> ExportImageFile(ExportFileInfoImage imageInfo, string destinationPath, CancellationToken cancellationToken) {
            Log.Update($"Начало экспорта изображения: {Path.GetFileName(imageInfo.Path)}");

            try {
                await Task.Run(() =>
                {
                    // Создаём объект для работы с изображением
                    using (var image = new MagickImage(imageInfo.Path)) {
                        // Устанавливаем цветовой профиль, если он задан
                        if (!string.IsNullOrEmpty(imageInfo.ColorProfile)) {
                            switch (imageInfo.ColorProfile) {
                                case "sRGB":
                                    image.SetProfile(ColorProfile.SRGB);
                                    break;
                                case "Adobe RGB":
                                    image.SetProfile(ColorProfile.AdobeRGB1998);
                                    break;
                                case "ProPhoto RGB":
                                    // Для ProPhoto RGB может потребоваться отдельный файл профиля
                                    // Можно добавить его как ресурс или загрузить из файла
                                    if (File.Exists("./Profiles/ProPhotoRGB.icc")) {
                                        image.SetProfile(new ColorProfile("./Profiles/ProPhotoRGB.icc"));
                                    }
                                    break;
                            }
                        }
                        if (imageInfo.ExportWidth > 0 && imageInfo.ExportHeight > 0) {
                            if (image.Width != imageInfo.ExportWidth || image.Height != imageInfo.ExportHeight) {
                                var size = new MagickGeometry((uint)imageInfo.ExportWidth, (uint)imageInfo.ExportHeight);
                                size.IgnoreAspectRatio = false;
                                image.Resize(size);
                            }
                        }
                        MagickFormat outputFormat = MagickFormat.Jpeg;
                        switch (imageInfo.Codec) {
                            case ImageFormat.JPEG:
                                outputFormat = MagickFormat.Jpeg;
                                image.Quality = (uint)imageInfo.Quality;
                                break;
                            case ImageFormat.PNG:
                                outputFormat = MagickFormat.Png;
                                image.Quality = (uint)imageInfo.CompressionLevel;
                                break;
                            case ImageFormat.GIF:
                                outputFormat = MagickFormat.Gif;
                                break;
                            case ImageFormat.WEBP:
                                outputFormat = MagickFormat.WebP;
                                image.Quality = (uint)imageInfo.CompressionLevel;
                                break;
                            case ImageFormat.TIFF:
                                outputFormat = MagickFormat.Tiff;
                                break;
                            case ImageFormat.HEIF:
                                outputFormat = MagickFormat.Heif;
                                image.Quality = (uint)imageInfo.CompressionLevel;
                                break;
                            case ImageFormat.AVIF:
                                outputFormat = MagickFormat.Avif;
                                image.Quality = (uint)imageInfo.CompressionLevel;
                                break;
                            case ImageFormat.BMP:
                                outputFormat = MagickFormat.Bmp;
                                break;
                            default:
                                // По умолчанию используем JPEG
                                outputFormat = MagickFormat.Jpeg;
                                image.Quality = (uint)imageInfo.CompressionLevel;
                                break;
                        }

                        if (!imageInfo.PreserveMetadata) {
                            image.Strip();
                        }

                        image.Write(destinationPath, outputFormat);
                    }
                }, cancellationToken);

                return true;
            }
            catch (Exception ex) {
                Log.Update($"Ошибка при экспорте изображения: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> ExportGenericFile(ExportFileInfoBase fileInfo, string destinationPath, CancellationToken cancellationToken) {
            Log.Update($"Экспорт файла (общий без преобразования): {Path.GetFileName(fileInfo.Path)}");

            try {
                File.Copy(fileInfo.Path, destinationPath, OverrideEncode);
                Log.Update($"Файл экспортирован: {Path.GetFileName(destinationPath)}");
                return true;
            }
            catch (Exception ex) {
                Log.Update($"Ошибка экспорта файла: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> DoExport() {
            try {
                if (string.IsNullOrWhiteSpace(TargetFolder)) {
                    throw new InvalidOperationException("Не указана выходная папка.");
                }

                if (!Directory.Exists(TargetFolder)) {
                    Directory.CreateDirectory(TargetFolder);
                }

                int totalFilesCount = ExportQueue.Count;
                if (totalFilesCount == 0) {
                    Log.Update("Очередь экспорта пуста");
                    return true;
                }

                Log.Update($"В очереди на экспорт: {totalFilesCount} файлов");
                if (EnableParallelExport) {
                    var tasks = new List<Task<bool>>();
                    var cts = new CancellationTokenSource();

                    while (ExportQueue.Count > 0) {
                        if (tasks.Count >= MaxParallelTasks) {
                            var completedTask = await Task.WhenAny(tasks);
                            tasks.Remove(completedTask);

                            if (!await completedTask) {
                                Log.Update("Произошла ошибка при экспорте");
                            }
                        }
                        var fileToExport = ExportQueue.Dequeue();
                        tasks.Add(ExportFile(fileToExport, cts.Token));
                    }

                    var results = await Task.WhenAll(tasks);
                    bool allSucceeded = results.All(r => r);

                    if (!allSucceeded) {
                        Log.Update("Некоторые файлы не были успешно экспортированы");
                    }
                }
                else {
                    int currentFile = 0;
                    while (ExportQueue.Count > 0) {
                        currentFile++;
                        var fileToExport = ExportQueue.Dequeue();
                        Log.Update($"Экспорт файла {currentFile} из {totalFilesCount}: {Path.GetFileName(fileToExport.Path)}");

                        bool success = await ExportFile(fileToExport);
                        if (!success) {
                            Log.Update($"Ошибка при экспорте файла {Path.GetFileName(fileToExport.Path)}");
                        }
                    }
                }

                return true;
            }
            catch (Exception ex) {
                Log.Update($"Критическая ошибка экспорта: {ex.Message}");
                return false;
            }
        }
    }
}
