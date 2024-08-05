using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using ClipsOrganizer.FileUtils;
using ClipsOrganizer.Model;
using Newtonsoft.Json;

namespace ClipsOrganizer.Collections {

    [JsonObject]
    public class Collection {
        public string CollectionTag { get; set; }
        public string Color { get; set; }
        public List<Item> Files { get; set; }

        //for json parsing
        public Collection() {
            this.Files = new List<Item>();
        }

        public Collection(DirectoryItem directoryItem) {
            //placeholders
            this.Color = "#000000";
            this.CollectionTag = "test1";
            Files = new List<Item>();
            foreach (var item in directoryItem.Items) {
                var fileInfo = new FileUtils.FileUtils().GetFileinfo(item.Path);
                Files.Add(new Item
                {
                    Name = item.Name,
                    Date = item.Date,
                    Path = item.Path
                });
            }
        }
        public bool Equals(Collection other) {
            if (other == null) return false;

            bool areFilesEqual = this.Files.SequenceEqual(other.Files);
            bool areTagsEqual = this.CollectionTag == other.CollectionTag;
            bool areColorsEqual = this.Color == other.Color;

            return areFilesEqual && areTagsEqual && areColorsEqual;
        }

        public override bool Equals(object obj) {
            if (obj is Collection otherCollection) {
                return Equals(otherCollection);
            }
            return false;
        }

    }

}

