using ClipsOrganizer.Collections;
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
        Descending_date = 2,
        MarkedGrouping = 3
    }

    public class Item {
        public string Name { get; set; }
        public string Path { get; set; }
        public DateTime Date { get; set; }
        [JsonIgnore]
        public string Color { get; set; }
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
        public List<Item> GetItemsFromCollections(List<Collections.Collection> collections, bool parsedFileNames = false, Sorts sortMethod = Sorts.Default) {
            var items = new List<Item>();
            //foreach (Collection collectiontest in collections)
            //    items.Add(new DirectoryItem()
            //    {
            //        Date = DateTime.MinValue,
            //        Items = collectiontest.Files,
            //    });

            return items;
        }
        public List<Item> GetItemsFromFolder(string path, bool parsedFileNames = false, Sorts sortMethod = Sorts.Default, List<Collection> collections = null) {
            var items = new List<Item>();
            var dirInfo = new DirectoryInfo(path);

            foreach (var directory in dirInfo.GetDirectories()) {
                var item = new DirectoryItem
                {
                    Name = directory.Name,
                    Path = directory.FullName,
                    Items = GetItemsFromFolder(directory.FullName, parsedFileNames, sortMethod, collections)
                };
                items.Add(item);
            }

            if (parsedFileNames) {
                items.AddRange(GetParsedFileItems(dirInfo, collections));
            }
            else {
                items.AddRange(GetFileItems(dirInfo, collections));
            }

            items = SortItems(sortMethod, items);
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
                case Sorts.MarkedGrouping:
                    //
                    break;
            }

            return items;
        }
    }

}
