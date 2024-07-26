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

    public class CollectionFiles : Item {
        public uint? FileIndexHigh { get; set; }
        public uint? FileIndexLow { get; set; }
    }

        [JsonObject]
    public class Collection {
        public string CollectionTag { get; set; }
        public Color Color { get; set; }
        public List<CollectionFiles> Files { get; set; }

        //for json parsing
        public Collection() {

        }

        public Collection(DirectoryItem directoryItem) {
            this.Color = Color.FromRgb(255, 255, 255);
            this.CollectionTag = "test1";
            Files = new List<CollectionFiles>();
            foreach (var item in directoryItem.Items) {
                var fileInfo = new FileUtils.FileUtils().GetFileinfo(item.Path);
                Files.Add(new CollectionFiles
                {
                    Name = item.Name,
                    Date = item.Date,
                    Path = item.Path,
                    FileIndexHigh = fileInfo.Value.FileIndexHigh,
                    FileIndexLow = fileInfo.Value.FileIndexLow,
                });
            }
        }

    }
    public class CollectionUIProvider : Collection {
        public List<Collection> collections { get; set; }
        public CollectionUIProvider() {
            collections = new List<Collection>();
        }
    }
}

