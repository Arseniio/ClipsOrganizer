using ClipsOrganizer.Collections;
using ClipsOrganizer.Settings;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;

namespace ClipsOrganizer.Model {

    public enum Sorts {
        Default = 0,
        Ascending_date = 1,
        Descending_date = 2
    }

    public class Item {
        public string Name { get; set; }
        public string Path { get; set; }
        public DateTime Date { get; set; }
        [JsonIgnore]
        public string Color { get; set; }
        public bool Equals(Item other) {
            if (other == null) return false;

            return this.Name == other.Name && this.Path == other.Path && this.Date == other.Date && this.Color == other.Color;
        }

        public override bool Equals(object obj) {
            if (obj is Item otherItem) {
                return Equals(otherItem);
            }
            return false;
        }
    }

    public class FileItem : Item {
    }
    public class DirectoryItem : Item {
        public List<Item> Items { get; set; }
        public DirectoryItem() {
            Items = new List<Item>();
        }
    }
    public class ItemProvider {
        public bool ParsedFileNames = false;

        public static FileInfo GetLastFile(string path,List<Item> OldItems) {
            var allFiles = Directory.GetFiles(path, "*", SearchOption.AllDirectories).Select(f => new FileInfo(f));
            var newFiles = allFiles.Where(file => !OldItems.Select(p=>p.Name).Contains(file.Name)).ToList();
            return newFiles.OrderByDescending(file => file.CreationTime).FirstOrDefault();
        }

        public List<Item> GetItemsFromFolder(string path, List<Collection> collections = null) {
            var items = new List<Item>();
            var dirInfo = new DirectoryInfo(path);
            if (!dirInfo.Exists) {
                Log.Update($"Не найдено рабочеей папки {path}");
                return null;
            }
            foreach (var directory in dirInfo.GetDirectories()) {
                var item = new DirectoryItem
                {
                    Name = directory.Name,
                    Path = directory.FullName,
                    Items = GetItemsFromFolder(directory.FullName, collections)
                };
                items.Add(item);
            }

            items.AddRange(GetFileItems(dirInfo, collections));

            items = SortItems(GlobalSettings.Instance.SortMethod, items);

            return items;
        }

        private static string PlaceholderColor = "#808080";

        private List<Item> GetParsedFileItems(DirectoryInfo dirInfo, List<Collection> collections = null) {
            var items = new List<Item>();
            foreach (var file in dirInfo.GetFiles()) {
                var splitfn = file.Name.Split(' ');
                string color = null;
                color = FindColorByCollections(collections ?? null, file.Name);
                items.Add(new FileItem
                {
                    Name = splitfn[0] + " " + splitfn[1],
                    Path = file.FullName,
                    Date = file.CreationTime,
                    Color = color ?? PlaceholderColor
                });
            }

            return items;
        }

        public string FindColorByCollections(List<Collection> collection, string filename) {
            if (collection == null) return null;
            foreach (var p in collection) {
                var file = p.Files.Find(x => x.Name == filename);
                if (file != null) {
                    return p.Color;
                }
            }
            return null;
        }
        public string FindColorByCollections(Collection collection, string filename) {
            if (collection == null) return null;
            var file = collection.Files.Find(x => x.Name == filename);
            if (file != null) {
                return collection.Color;
            }
            return null;
        }

        private List<Item> GetFileItems(DirectoryInfo dirInfo, List<Collection> collections = null) {
            var items = new List<Item>();
            foreach (var file in dirInfo.GetFiles()) {
                string color = null;
                color = FindColorByCollections(collections ?? null, file.Name);
                items.Add(new FileItem
                {
                    Name = file.Name,
                    Path = file.FullName,
                    Date = file.CreationTime,
                    Color = color ?? PlaceholderColor
                });
            }

            return items;
        }

        private static List<Item> SortItems(Sorts SortMethod, List<Item> items) {
            switch (SortMethod) {
                case Sorts.Default:
                    //do nothing :)
                    break;
                case Sorts.Ascending_date:
                    items = items.OrderBy(p => p.Date).ToList();
                    break;
                case Sorts.Descending_date:
                    items = items.OrderByDescending(p => p.Date).ToList();
                    break;
            }

            return items;
        }
    }

}
