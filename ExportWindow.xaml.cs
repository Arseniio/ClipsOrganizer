using ClipsOrganizer.Properties;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ClipsOrganizer {
    /// <summary>
    /// Логика взаимодействия для ExportWindow.xaml
    /// </summary>
    public partial class ExportWindow : Window {
        public Settings.Settings Settings { get; set; }
        public ExportWindow(Settings.Settings settings) {
            InitializeComponent();
            if (settings.collections == null) {
                throw new System.ArgumentNullException();
            }
            Settings = settings;
        }

        private void Btn_export_Click(object sender, RoutedEventArgs e) {
            string ExportFolderPrefix = "./Exported/";
            foreach (var collection in Settings.collections) {
                //creating filestructure
                string ExportcollectionPath = ExportFolderPrefix + collection.CollectionTag;
                Directory.CreateDirectory(ExportcollectionPath);
                ExportcollectionPath += "/"; //ClipsOrganizer/bin/Debug/Exported/Collection/
                foreach (var item in collection) {
                    try {
                        //File.Move(item.Path, ExportcollectionPath + item.Name);
                        item.Path = ExportcollectionPath + item.Name;
                        Log.Text += "\nУспешно перемещён " + ExportcollectionPath + item.Name;
                    }
                    catch {
                        Log.Text += "\nНевозможно переместить " + ExportcollectionPath + item.Name;
                    }
                }

            }
        }
    }
}
