using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClipsOrganizer.Collections;
using System.Windows.Media.Animation;
using System.IO;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using ClipsOrganizer.Properties;
using System.Diagnostics.Contracts;
using System.Windows;
using System.Threading;
using System.Runtime;
using System.Diagnostics;
using Microsoft.WindowsAPICodePack.Shell;
using ClipsOrganizer.Model;
using ClipsOrganizer.Classes;

namespace ClipsOrganizer.Settings {
    [Serializable]
    public class GlobalSettings {
        //in order in json file
        public string FFmpegpath { get; set; }
        public string LastSelectedProfile { get; set; }
        public VideoCodec LastUsedCodec { get; set; }
        public string LastUsedQuality { get; set; }
        public string LastUsedEncoderPath { get; set; }
        public ExportAudioFormat LastUsedAudioFormat { get; set; }
        public int LastUsedAudioBitrate { get; set; }
        public bool OpenFolderAfterEncoding { get; set; }
        public Model.Sorts SortMethod { get; set; }
        // Image settings
        public string ImageBackgroundColor { get; set; } = "#999999";
        public double ImageZoomLevel { get; set; }
        // Video settings
        public double DefaultVolumeLevel { get; set; } = 0.5;
        public bool AutoPlay { get; set; } = false;
        public TimeSpan AutoPlayOffset { get; set; } = TimeSpan.Zero;

        public ExportSettings ExportSettings { get; set; } = new ExportSettings();

        public ExportFileInfoImage DefaultImageExport { get; set; } = new ExportFileInfoImage()
        {
            Codec = ImageFormat.JPEG,
            CompressionLevel = 50,
        };

        public ExportFileInfoAudio DefaultAudioExport { get; set; } = new ExportFileInfoAudio
        {
            AudioSampleRate = 44100,
            outputFormat = ExportAudioFormat.mp3,
            AudioBitrate = 128,
            AudioChannels = 2,
            NormalizeAudio = true,
            TrimStart = TimeSpan.Zero,
            TrimEnd = TimeSpan.Zero
        };

        public ExportFileInfoVideo DefaultVideoExport { get; set; } = new ExportFileInfoVideo
        {
            // Video Settings
            VideoBitrate = 5000, // Битрейт видео в kbps
            CRF = 23.0, // Качество (CRF)
            Resolution = ResolutionType.Original, // Разрешение
            CustomResolution = new CustomResolution { Width = 1920, Height = 1080, KeepAspectRatio = true }, // Кастомное разрешение
            FrameRate = null, // Частота кадров (null = исходная)
            TwoPassEncoding = false, // Двухпроходное кодирование

            // Audio Settings
            AudioCodec = AudioCodec.AAC, // Аудиокодек
            AudioBitrate = 128, // Битрейт аудио в kbps
            AudioChannels = 2, // Количество каналов
            NormalizeAudio = false, // Нормализация звука

            // Trim Settings
            TrimStart = TimeSpan.Zero, // Начало обрезки
            TrimEnd = TimeSpan.Zero, // Конец обрезки

            // Hardware Acceleration
            HardwareAcceleration = HardwareAccelerationType.NVENC, // Аппаратное ускорение
            GPUDeviceId = null, // ID GPU (если используется несколько)

            // Metadata
            CopyMetadata = true // Копирование метаданных
        };

        [JsonIgnore]
        public FFmpegManager ffmpegManager { get; set; }
        private static GlobalSettings _instance;
        public static GlobalSettings Instance {
            get {
                if (_instance == null) {
                    _instance = new GlobalSettings();
                }
                return _instance;
            }
            set {
                _instance = value; //yeah that's a singletron but that just easier...
            }
        }

        public static void Initialize(string ffmpegPath) {
            var instance = Instance;
            instance.FFmpegpath = ffmpegPath;
        }

        private GlobalSettings() {
        }

        public FFmpegManager FFmpegInit() {
            try {
                this.ffmpegManager = new FFmpegManager(this.FFmpegpath);
                return ffmpegManager;
            }
            catch (Exception e) {
                Log.Update(e.Message);
                return null;
            }
        }

        public void UpdateSettings(GlobalSettings ChangedSettings) {
            //TODO add more changed args after finishing settings window
            this.FFmpegpath = ChangedSettings.FFmpegpath;
            this.LastUsedCodec = ChangedSettings.LastUsedCodec;
            this.LastUsedEncoderPath = ChangedSettings.LastUsedEncoderPath;
            this.LastUsedQuality = ChangedSettings.LastUsedQuality;
            this.LastUsedAudioFormat = ChangedSettings.LastUsedAudioFormat;
            this.LastUsedAudioBitrate = ChangedSettings.LastUsedAudioBitrate;
        }
    }
}
