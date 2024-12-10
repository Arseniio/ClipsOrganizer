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

namespace ClipsOrganizer.Settings {
    [Serializable]
    public class GlobalSettings {
        //in order in json file
        //public string ClipsFolder { get; set; }
        public string FFmpegpath { get; set; }
        public string LastSelectedProfile { get; set; }
        public VideoCodec LastUsedCodec { get; set; }
        public string LastUsedQuality { get; set; }
        public string LastUsedEncoderPath { get; set; }
        public bool OpenFolderAfterEncoding { get; set; }

        [JsonIgnore]
        public ffmpegManager ffmpegManager { get; set; }

        public GlobalSettings(string ClipsFolder, string ffmpegpath, string settingsPath = "./settings.json") {
            this.FFmpegpath = ffmpegpath;
        }

        public void ffmpegInit() {
            try {
                this.ffmpegManager = new ffmpegManager(this.FFmpegpath);
            }
            catch (Exception e) {
                MessageBox.Show(e.Message);
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
