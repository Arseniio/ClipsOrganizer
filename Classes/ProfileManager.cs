using ClipsOrganizer.Collections;
using ClipsOrganizer.Settings;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ClipsOrganizer.Profiles {
    [Serializable]
    public class Profile {
        public string ProfileName { get; set; }
        public string ClipsFolder { get; set; }
        [JsonIgnore]
        public string ProfilePath {
            get {
                return $"./Profiles/{this.ProfileName}.json";
            }
        }
        [JsonIgnore]
        public int TotalFileCount {
            get {
                return Collections?.Sum(c => c.Files?.Count ?? 0) ?? 0;
            }
        }
        [JsonIgnore]
        public string ProfileBkpPath {
            get {
                return $"./Profiles/{this.ProfileName}_Bkp.json";
            }
        }
        public List<Collection> Collections { get; set; } = new List<Collection>();
    }
    public static class ProfileManager {
        public static string ProfilePath = "./Profiles/";
        public static List<string> LoadAllProfiles(string ProfilePath = "./Profiles/") {
            string[] allFiles = Directory.GetFiles(ProfilePath);
            List<string> profiles = new List<string>();
            foreach (string file in allFiles) {
                if (file.EndsWith("_bkp.json")) continue;
                try {
                    string fileContent = File.ReadAllText(file);
                    var jsonObject = JsonConvert.DeserializeObject<Profile>(fileContent);
                    if (jsonObject != null && jsonObject.ProfileName != null) {
                        profiles.Add(jsonObject.ProfileName);
                    }
                }
                catch (Exception ex) {
                    Log.Update($"Ошибка при обработке файла {file}: {ex.Message}");
                }
            }
            return profiles;
        }
    }
}
