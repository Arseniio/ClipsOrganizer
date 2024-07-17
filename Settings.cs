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
        public Settings(string ClipsFolder, List<Collection> collections = null, string settingsPath = "./settings.json") {
            this.SettingsFile = this.SettingsFile ?? new SettingsFile(settingsPath, ClipsFolder, this);
            collections = collections ?? new List<Collection>();
            this.ClipsFolder = ClipsFolder;
            this.collections = collections;

            //SettingsFile.LoadSettings();
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
            this.Settings = JsonConvert.DeserializeObject<Settings>(lines); 
            return false;
        }
    }
}
