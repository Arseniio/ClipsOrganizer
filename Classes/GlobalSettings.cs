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

namespace ClipsOrganizer.Settings {
    [Serializable]
    public class GlobalSettings {
        //in order in json file
        public string FFmpegpath { get; set; }
        public string LastSelectedProfile { get; set; }
        public VideoCodec LastUsedCodec { get; set; }
        public string LastUsedQuality { get; set; }
        public string LastUsedEncoderPath { get; set; }
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

        public ExportFileInfoImage DefaultImageExport { get; set; } = new ExportFileInfoImage() { Codec = ImageFormat.JPEG, Quality = 5 };
        public ExportFileInfoVideo DefaultVideoExport { get; set; } = new ExportFileInfoVideo() { Codec = VideoCodec.H264_NVENC, Quality = 5000 };

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
        }


    }


}
