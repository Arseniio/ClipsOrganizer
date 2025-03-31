using ClipsOrganizer.Collections;
using ClipsOrganizer.Model;
using ClipsOrganizer.Settings;
using ClipsOrganizer.ViewableControls;
using MetadataExtractor.Util;
using System;
using System.Collections.Generic;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ClipsOrganizer.ExportControls {
    /// <summary>
    /// Логика взаимодействия для ExportCollectionsControl.xaml
    /// </summary>
    public partial class ExportCollectionsControl : UserControl {
        public ExportCollectionsControl() {
            InitializeComponent();
            LV_Collection.ItemsSource = MainWindow.CurrentProfile.Collections;
        }



        private void Btn_Select_all_Click(object sender, RoutedEventArgs e) {
            var items = LV_Collection.Items.Cast<Collections.Collection>().ToList();
            bool allSelected = items.All(item => item.IsSelected);
            bool noneSelected = items.All(item => !item.IsSelected);

            bool newState = allSelected ? false : true;

            foreach (var item in items) {
                item.IsSelected = newState;
                Log.Update($"{item.CollectionTag} {item.IsSelected} newstate: {newState}");
            }

            LV_Collection.ItemsSource = null;
            LV_Collection.ItemsSource = MainWindow.CurrentProfile.Collections;
            LV_Collection.Items.Refresh();
        }


        private void CheckBox_Checked(object sender, RoutedEventArgs e) {
            foreach (var File in ((sender as CheckBox).DataContext as Collection)) {
                if (ExportQueue._queue.Contains(File)) continue;
                var fileType = ViewableController.FileTypeDetector.DetectFileType(File.Path);

                if (fileType == SupportedFileTypes.Image) {
                    var exportItem = new ExportFileInfoImage(File)
                    {
                        Codec = GlobalSettings.Instance.DefaultImageExport.Codec,
                        Quality = GlobalSettings.Instance.DefaultImageExport.Quality,
                        OutputPath = File.Path,
                        OutputFormat = "jpg"
                    };
                    ExportQueue.Enqueue(exportItem);
                }
                else if (fileType == SupportedFileTypes.Video) {
                    var exportItem = new ExportFileInfoVideo(File)
                    {
                        Quality = GlobalSettings.Instance.DefaultVideoExport.Quality,
                        OutputFormat = "mp4"
                    };
                    ExportQueue.Enqueue(exportItem);
                }
            }
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e) {
            foreach (var File in (sender as CheckBox).DataContext as Collection) {
                if (!ExportQueue._queue.Contains(File)) continue;
                else ExportQueue._queue.RemoveAll(p => p.Path == File.Path);
            }
        }
    }
}
