using ClipsOrganizer.Model;
using ClipsOrganizer.Settings;
using System;
using System.Text;
using System.Threading.Tasks;
using Xabe.FFmpeg;

namespace ClipsOrganizer.Model {
    public class ExportFileInfoVideo : ExportFileInfoBase {
        // Video Settings
        public VideoCodec VideoCodec { get; set; } = VideoCodec.Unknown;
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
        public ExportFileInfoVideo(Item item) : base(item) {
            if (GlobalSettings.Instance?.DefaultVideoExport != null) {
                var defaultExport = GlobalSettings.Instance.DefaultVideoExport;

                VideoCodec = defaultExport.VideoCodec;
                VideoBitrate = defaultExport.VideoBitrate;
                CRF = defaultExport.CRF;
                Resolution = defaultExport.Resolution;
                CustomResolution = defaultExport.CustomResolution;
                FrameRate = defaultExport.FrameRate;
                TwoPassEncoding = defaultExport.TwoPassEncoding;

                AudioCodec = defaultExport.AudioCodec;
                AudioBitrate = defaultExport.AudioBitrate;
                AudioChannels = defaultExport.AudioChannels;
                NormalizeAudio = defaultExport.NormalizeAudio;

                TrimStart = defaultExport.TrimStart;
                TrimEnd = defaultExport.TrimEnd;

                HardwareAcceleration = defaultExport.HardwareAcceleration;
                GPUDeviceId = defaultExport.GPUDeviceId;

                CopyMetadata = defaultExport.CopyMetadata;
            }
        }
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
}
