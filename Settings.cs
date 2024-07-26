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

namespace ClipsOrganizer.Settings {
    [Serializable]
    public class Settings {
        public string ClipsFolder { get; set; }
        public List<Collection> collections { get; set; }
        [JsonIgnore]
        virtual public SettingsFile SettingsFile { get; set; }
        public Settings(string ClipsFolder, string settingsPath = "./settings.json") {
            this.SettingsFile = this.SettingsFile ?? new SettingsFile(settingsPath, ClipsFolder, this);
            collections = collections ?? new List<Collection>();
            this.ClipsFolder = ClipsFolder;

            //SettingsFile.LoadSettings();
        }
        public void UpdateSettings(Settings ChangedSettings) {
            this.collections = ChangedSettings.collections;
            this.ClipsFolder = ChangedSettings.ClipsFolder;
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

        public bool WriteSettings() {
            foreach (var collection in this.Settings.collections){
                foreach (var file in collection.Files) {
                    if(file.FileIndexLow == null || file.FileIndexHigh == null) {
                        var utils = new FileUtils.FileUtils();
                        FileUtils.FileUtils.BY_HANDLE_FILE_INFORMATION FileInfo = utils.GetFileinfo(file.Path);
                        file.FileIndexHigh = FileInfo.FileIndexHigh;
                        file.FileIndexLow = FileInfo.FileIndexLow;
                    }
                }
            }
            string contents = JsonConvert.SerializeObject(this.Settings);
            File.WriteAllText("./settings.json", contents);
            return false;
        }


        public bool LoadSettings(string settingsFile = "./settings.json") {
            if (string.IsNullOrWhiteSpace(settingsFile)) {
                return false;
            }
            // parsing settings file
            var lines = File.ReadAllText(settingsFile);
            Settings.UpdateSettings(JsonConvert.DeserializeObject<Settings>(lines)); 
            return false;
        }
    }
}
