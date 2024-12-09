﻿using System;
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
        public static ExportSettings ExportSettings { get; set; }
        //public bool UseDefaultExportSettings { get; set; }
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
        public VideoCodec EncodeFormat { get; set; }
        public int EncodeBitrate { get; set; }

        //General export settings
        public string TargetFolder { get; set; } = "./Temp";
        public bool ExportAllFiles { get; set; }
        [JsonIgnore]
        public List<string> FilesToExport { get; set; }
        public bool DeleteFilesAfterExport { get; set; }
        public bool EnableLogging { get; set; }
        public string LogFilePath { get; set; }
        public int MaxFileSizeMB { get; set; }
        [JsonIgnore]
        public int TotalFileSizeAfterExport {  get; set; }
        
        //Advanced general
        public bool UseRegex { get; set; }
        public string FileNameTemplate { get; set; } 

        public bool DoExport() {
            new NotImplementedException();
            return true;
        }
    }
}
