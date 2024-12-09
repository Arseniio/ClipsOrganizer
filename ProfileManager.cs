using ClipsOrganizer.Collections;
using ClipsOrganizer.Settings;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
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
        public List<Collection> Collections { get; set; } = new List<Collection>();
        //public string GetCurrentProfilePath() {
        //    return $"./Profiles/{this.ProfileName}.json";
        //}
    }
}
