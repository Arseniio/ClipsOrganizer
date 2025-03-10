using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using ClipsOrganizer.Settings;

namespace ClipsOrganizer.ViewableControls {
    /// <summary>
    /// Логика взаимодействия для ImageViewer.xaml
    /// </summary>
    public partial class ImageViewer : UserControl {
        public ImageViewer() {
            InitializeComponent();
            ViewableController.FileLoaded += ViewableController_FileLoaded;
            this.Unloaded += ImageViewer_Unloaded;
        }

        private void ImageViewer_Unloaded(object sender, RoutedEventArgs e) {
            ViewableController.FileLoaded -= ViewableController_FileLoaded;
        }

        private void ViewableController_FileLoaded(object sender, FileLoadedEventArgs e) {
            BitmapImage bitmap = new BitmapImage(new Uri(e.Item.Path));
            Img_Displayed.Source = bitmap;
            Img_Border.Background = (SolidColorBrush)new BrushConverter().ConvertFromString(GlobalSettings.Instance.ImageBackgroundColor);
        }

    }


}
