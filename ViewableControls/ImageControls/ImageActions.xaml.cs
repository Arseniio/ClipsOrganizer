using ClipsOrganizer.ExportControls;
using ClipsOrganizer.Model;
using ClipsOrganizer.Settings;

using MetadataExtractor;

using System;
using System.Windows;
using System.Windows.Controls;

namespace ClipsOrganizer.ViewableControls.ImageControls {
    /// <summary>
    /// Логика взаимодействия для ImageActions.xaml
    /// </summary>
    public partial class ImageActions : UserControl {
        public ExportFileInfoImage ExportInfo { get; set; }

        public ImageActions() {
            InitializeComponent();
            ViewableController.FileLoaded += ViewableController_FileLoaded;
            this.Unloaded += ImageActions_Unloaded;
            this.Loaded += ImageActions_Loaded;
            this.DataContext = GlobalSettings.Instance.DefaultImageExport;
            SL_quality.Value = GlobalSettings.Instance.DefaultImageExport.CompressionLevel;
        }

        public ImageActions(ExportFileInfoImage exportInfo) {
            InitializeComponent();
            ExportInfo = exportInfo;
            DataContext = ExportInfo;
            SL_quality.Value = GlobalSettings.Instance.DefaultImageExport.CompressionLevel;
            CB_format.SelectedItem = exportInfo.OutputFormat;
            Btn_AddToQueue.Visibility = Visibility.Hidden;
            Btn_ExportNow.Visibility = Visibility.Hidden;
        }

        private void ImageActions_Loaded(object sender, RoutedEventArgs e) {
            ViewableController.FileLoaded += ViewableController_FileLoaded;
        }

        private void ImageActions_Unloaded(object sender, RoutedEventArgs e) {
            ViewableController.FileLoaded -= ViewableController_FileLoaded;
        }

        public void ViewableController_FileLoaded(object sender, FileLoadedEventArgs e) {
            if (e.FileType != SupportedFileTypes.Image) return;
            
            if (e.Item != null) {
                ExportInfo = new ExportFileInfoImage(e.Item);
                DataContext = ExportInfo;
            }
        }
        private void Btn_AddToQueue_Click(object sender, RoutedEventArgs e) {
            if (ExportInfo == null)
                return;

            Log.Update($"Файл {ExportInfo.Name} добавлен в очередь");
            ExportQueue.Enqueue(ExportInfo);
        }
        private void Btn_ExportNow_Click(object sender, RoutedEventArgs e) {
            GlobalSettings.Instance.ExportSettings.ExportFile(ExportInfo, default);
        }
    }
}
