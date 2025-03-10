using ClipsOrganizer.ExportControls;
using ClipsOrganizer.Model;
using ClipsOrganizer.Settings;
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

namespace ClipsOrganizer.ViewableControls.ImageControls {
    /// <summary>
    /// Логика взаимодействия для ImageActions.xaml
    /// </summary>
    public partial class ImageActions : UserControl {
        public ImageActions() {
            InitializeComponent();
            ViewableController.FileLoaded += ViewableController_FileLoaded;
            this.Unloaded += ImageActions_Unloaded;
        }

        private void ImageActions_Unloaded(object sender, RoutedEventArgs e) {
            ViewableController.FileLoaded -= ViewableController_FileLoaded;
        }

        Item CurrentItem { get; set; }
        private void ViewableController_FileLoaded(object sender, FileLoadedEventArgs e) {
            CurrentItem = e.Item;

        }

        private void Btn_AddToQueue_Click(object sender, RoutedEventArgs e) {
            ExportQueue.Enqueue(new ExportFileInfoImage(CurrentItem) { Quality = 5, OutputPath = GlobalSettings.Instance.ExportSettings.TargetFolder, OutputFormat = ".jpg", Codec = ImageFormat.JPEG });
        }
    }
}
