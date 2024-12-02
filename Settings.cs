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
    public class Settings {
        //in order in json file
        public string ClipsFolder { get; set; }
        public string FFmpegpath { get; set; }
        public VideoCodec LastUsedCodec { get; set; }
        public string LastUsedQuality { get; set; }
        public string LastUsedEncoderPath { get; set; }



        public List<Collection> collections { get; set; }


        [JsonIgnore]
        virtual public SettingsFile SettingsFile { get; set; }
        [JsonIgnore]
        public ffmpegManager ffmpegManager { get; set; }

        public Settings(string ClipsFolder, string ffmpegpath, string settingsPath = "./settings.json") {
            this.SettingsFile = this.SettingsFile ?? new SettingsFile(settingsPath, ClipsFolder, this);
            collections = collections ?? new List<Collection>();
            this.ClipsFolder = ClipsFolder;
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

        public void UpdateSettings(Settings ChangedSettings) {
            //TODO add more changed args after finishing settings window
            this.collections = ChangedSettings.collections;
            this.ClipsFolder = ChangedSettings.ClipsFolder;
            this.FFmpegpath = ChangedSettings.FFmpegpath;
            this.LastUsedCodec = ChangedSettings.LastUsedCodec;
            this.LastUsedEncoderPath = ChangedSettings.LastUsedEncoderPath;
            this.LastUsedQuality = ChangedSettings.LastUsedQuality;
        }
        public bool Equals(Settings other) {
            if (other == null) return false;

            bool areCollectionsEqual = this.collections.SequenceEqual(other.collections);
            bool areFoldersEqual = this.ClipsFolder == other.ClipsFolder;

            return areCollectionsEqual && areFoldersEqual;
        }
        
    }
    public class SettingsFile {
        //settings file properties
        private string settingsFile = null;
        public virtual Settings Settings { get; set; }

        public SettingsFile(string settingsFile, string clipsFolder, Settings settings) {
            this.Settings = settings;
            this.settingsFile = settingsFile; //maybe delete later if not used
        }

        //public bool WriteSettings() {
        //    StringBuilder ErrorFiles = new StringBuilder();
        //    var errors = 0;
        //    foreach (var collection in this.Settings.collections) {
        //        foreach (var file in collection.Files) {
        //            if (file.FileIndexLow == null || file.FileIndexHigh == null) {
        //                var utils = new FileUtils.FileUtils();
        //                FileUtils.FileUtils.BY_HANDLE_FILE_INFORMATION? FileInfo = utils.GetFileinfo(file.Path);
        //                if (FileInfo == null) {
        //                    ErrorFiles.Append(string.Format("Unable to save file indexes for {0}", file.Name));
        //                    errors++;
        //                }
        //                else {
        //                    file.FileIndexHigh = FileInfo.Value.FileIndexHigh;
        //                    file.FileIndexLow = FileInfo.Value.FileIndexLow;
        //                }
        //            }
        //        }
        //    }
        //    if (errors > 0) {
        //        MessageBoxResult result = MessageBox.Show(string.Format("Found {0} errors on files\n {1} \n Continue saving without file ids?", errors, ErrorFiles.ToString()), "Error", MessageBoxButton.YesNo);
        //        if (MessageBoxResult.Yes == result) {
        //            string contents = JsonConvert.SerializeObject(this.Settings);
        //            File.WriteAllText("./settings.json", contents);
        //        }
        //    }
        //    return false;
        //}

        protected bool WriteSettings() {
            string contents = JsonConvert.SerializeObject(this.Settings);
            File.WriteAllText("./settings.json", contents);
            return false;
        }
        public void LoadSettings(string settingsFile = "./settings.json") {
            if (string.IsNullOrWhiteSpace(settingsFile)) {
                return;
            }
            // parsing settings file
            if (!File.Exists(settingsFile)) {
                (File.Create(settingsFile) as FileStream).Close();
                this.WriteSettings();
                return;
            }
            if (!File.OpenRead(settingsFile).CanRead) return;
            var lines = File.ReadAllText(settingsFile);
            if (string.IsNullOrWhiteSpace(lines)) return;
            var settings = JsonConvert.DeserializeObject<Settings>(lines);
            settings.collections.ForEach(s => s.Files.ForEach(f => f.Color = s.Color));

            Settings.UpdateSettings(settings);
        }
        public bool CheckIfChanged(string settingsFile = "./settings.json") {
            if (string.IsNullOrWhiteSpace(settingsFile)) {
                return false;
            }
            var lines = File.ReadAllText(settingsFile);
            var oldsettings = JsonConvert.DeserializeObject<Settings>(lines);
            oldsettings.collections.ForEach(s => s.Files.ForEach(f => f.Color = s.Color));

            bool changed = !this.Settings.Equals(oldsettings);

            return changed;
        }
        public void WriteAndCreateBackupSettings(string settingsFile = "./settings.json") {
            if (File.Exists("./settingsOld.json")) File.Delete("./settingsOld.json");
            File.Move(settingsFile, "./settingsOld.json");
            this.WriteSettings();
        }
    }
}
