using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using MetadataExtractor;



namespace ClipsOrganizer.ViewableControls {
    /// <summary>
    /// Логика взаимодействия для ImageViewer.xaml
    /// </summary>
    public partial class ImageViewer : UserControl {
        public ImageViewer() {
            InitializeComponent();
            Img_Border.Background = (SolidColorBrush)new BrushConverter().ConvertFromString(GlobalSettings.Instance.ImageBackgroundColor);
        }
        public void LoadImage(string filePath) {
            BitmapImage bitmap = new BitmapImage(new Uri(filePath));
            Img_Displayed.Source = bitmap;
        }
    }
}
