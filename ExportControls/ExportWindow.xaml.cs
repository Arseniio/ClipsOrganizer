using ClipsOrganizer.ExportControls;
using ClipsOrganizer.Profiles;
using ClipsOrganizer.Properties;
using ClipsOrganizer.Settings;
using ClipsOrganizer.SettingsControls;
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
        public Profile profile { get; set; }
        public ExportWindow(Profile profile) {
            InitializeComponent();
            if (profile.Collections.Count == 0) {
                //throw new System.ArgumentNullException();
            }
            this.profile = profile;
        }

        //public bool bresult = false;
        //private void Btn_export_Click(object sender, RoutedEventArgs e) {
        //    string ExportFolderPrefix = "./Exported/";
        //    Log.Text = "";
        //    var result = MessageBox.Show("Хотите ли вы сохранить пути для перемещённых файлов в коллекциях?\n(Не позволит далее работать с ними в программе (Относительные пути файлов))", "Подтверждение", MessageBoxButton.YesNo);
        //    if (result == MessageBoxResult.Yes) {
        //        bresult = true;
        //    }
        //    foreach (var collection in profile.Collections) {
        //        string ExportcollectionPath = ExportFolderPrefix + collection.CollectionTag;
        //        Directory.CreateDirectory(ExportcollectionPath);
        //        ExportcollectionPath += "/"; //ClipsOrganizer/bin/Debug/Exported/Collection/
        //        foreach (var item in collection) {
        //            try {
        //                File.Move(item.Path, ExportcollectionPath + item.Name);
        //                if (bresult)
        //                    item.Path = ExportcollectionPath + item.Name;
        //                Log.Text += "\nУспешно перемещён " + ExportcollectionPath + item.Name;
        //            }
        //            catch {
        //                Log.Text += "\nНевозможно переместить " + ExportcollectionPath + item.Name;
        //            }
        //        }
        //    }
        //    this.Btn_export.IsEnabled = false;
        //}

        private void LB_Menu_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (LB_Menu.SelectedItem is ListBoxItem selectedItem) {
                string tag = selectedItem.Tag as string;
                switch (tag) {
                    case "ExportLocal":
                        CC_Export.Content = new ExportLocalControl();
                        break;
                    case "FileSelection":
                        CC_Export.Content = new ExportCollectionsControl();
                        break;
                    case "FilesQueue":
                        CC_Export.Content = new FilesQueue();
                        break;
                    case "GeneralSettings":
                        CC_Export.Content = new ExportGeneralControl();
                        break;
                    case "EncodeFiles":
                        CC_Export.Content = new EncodeFilesControl();
                        break;
                }
            }
        }
        
        private void Btn_Export_Click(object sender, RoutedEventArgs e) {
            GlobalSettings.Instance.ExportSettings.DoExport();
        }

        private void Window_Closed(object sender, EventArgs e) {
            (Owner as MainWindow).TV_clips.IsEnabled = true;
            (Owner as MainWindow).TV_clips_collections.IsEnabled = true;
        }
    }
}
