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

            if (EnableLogging) {
                Log.Update($"Начало экспорта файла: {Path.GetFileName(fileInfo.Path)}");
            }

            string destinationPath = fileInfo.OutputPath;
            if (string.IsNullOrEmpty(destinationPath)) {
                if (TargetFolder.StartsWith(".")) {
                    TargetFolder = Environment.CurrentDirectory + TargetFolder.Substring(1);
                }
                destinationPath = Path.Combine(TargetFolder, Path.GetFileName(fileInfo.Path));
            }

            try {
                // Определение типа файла
                var fileType = ViewableControls.ViewableController.FileTypeDetector.DetectFileType(fileInfo.Path);

                switch (fileType) {
                    case ViewableControls.SupportedFileTypes.Video:
                        // Обработка видео файла
                        if (fileInfo is ExportFileInfoVideo videoInfo) {
                            return await ExportVideoFile(videoInfo, destinationPath, cancellationToken);
                        }
                        else {
                            // Если файл видео, но передан обычный ExportFileInfoBase
                            return await ExportGenericVideoFile(fileInfo, destinationPath, cancellationToken);
                        }

                    case ViewableControls.SupportedFileTypes.Image:
                        // Обработка файла изображения
                        if (fileInfo is ExportFileInfoImage imageInfo) {
                            return await ExportImageFile(imageInfo, destinationPath, cancellationToken);
                        }
                        else {
                            // Если файл изображения, но передан обычный ExportFileInfoBase
                            return await ExportGenericImageFile(fileInfo, destinationPath, cancellationToken);
                        }

                    default:
                        // Обработка других типов файлов - просто копирование
                        Log.Update($"Копирование файла неизвестного типа: {Path.GetFileName(fileInfo.Path)}");
                        File.Copy(fileInfo.Path, destinationPath, OverrideEncode);
                        return true;
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
        }

        private async Task<bool> ExportVideoFile(ExportFileInfoVideo videoInfo, string destinationPath, CancellationToken cancellationToken) {
            Log.Update($"Экспорт видео: {Path.GetFileName(videoInfo.Path)}");

            try {
                var ffmpegManager = GlobalSettings.Instance.FFmpegInit();
                VideoCodec codec;
                // Настройка кодека и битрейта на основе пользовательских настроек
                if (videoInfo.VideoCodec != VideoCodec.Unknown) {
                    codec = videoInfo.VideoCodec;
                }
                else {
                    codec = this.EncodeFormat == VideoCodec.Unknown ? VideoCodec.H265_x265 : this.EncodeFormat;
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

                bool success = await ffmpegManager.StartEncodingAsync(
                    videoInfo.Path,
                    destinationPath,
                    codec,
                    bitrate,
                    startTime,
                    endTime
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

        private async Task<bool> ExportGenericVideoFile(ExportFileInfoBase fileInfo, string destinationPath, CancellationToken cancellationToken) {
            Log.Update($"Экспорт видео (общий): {Path.GetFileName(fileInfo.Path)}");
            if (EncodeEnabled) {
                try {
                    var ffmpegManager = GlobalSettings.Instance.FFmpegInit();
                    bool success = await ffmpegManager.StartEncodingAsync(
                        fileInfo.Path,
                        destinationPath,
                        this.EncodeFormat,
                        this.EncodeBitrate
                    );
                    if (success) {
                        Log.Update($"Видео успешно экспортировано: {Path.GetFileName(destinationPath)}");
                    }
                    else {
                        Log.Update($"Ошибка при экспорте видео: {Path.GetFileName(fileInfo.Path)}");
                    }
                    return success;
                }
                catch (Exception ex) {
                    Log.Update($"Ошибка экспорта видео: {ex.Message}");
                    return false;
                }
            }
            else {
                File.Copy(fileInfo.Path, destinationPath, OverrideEncode);
                Log.Update($"Видео скопировано без перекодирования: {Path.GetFileName(destinationPath)}");
                return true;
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

        private async Task<bool> ExportGenericImageFile(ExportFileInfoBase fileInfo, string destinationPath, CancellationToken cancellationToken) {
            Log.Update($"Экспорт изображения (общий): {Path.GetFileName(fileInfo.Path)}");

            try {
                File.Copy(fileInfo.Path, destinationPath, OverrideEncode);
                Log.Update($"Изображение экспортировано: {Path.GetFileName(destinationPath)}");
                return true;
            }
            catch (Exception ex) {
                Log.Update($"Ошибка экспорта изображения: {ex.Message}");
                return false;
            }
        }

        // Полностью переписанный метод DoExport для работы с очередью
        public async Task<bool> DoExport() {
            try {
                if (string.IsNullOrWhiteSpace(TargetFolder)) {
                    throw new InvalidOperationException("Не указана выходная папка.");
                }

                if (!Directory.Exists(TargetFolder)) {
                    Directory.CreateDirectory(TargetFolder);
                }

                if (EnableLogging && !string.IsNullOrEmpty(LogFilePath)) {
                    File.AppendAllText(LogFilePath, $"Export started at {DateTime.Now}\n");
                    Log.Update($"Начат экспорт в {DateTime.Now}");
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

                // Логирование завершения
                if (EnableLogging && !string.IsNullOrEmpty(LogFilePath)) {
                    File.AppendAllText(LogFilePath, $"Export completed at {DateTime.Now}\n");
                    Log.Update($"Экспорт завершен в {DateTime.Now}");
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
