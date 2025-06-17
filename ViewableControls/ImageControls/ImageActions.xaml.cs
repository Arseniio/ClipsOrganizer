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
            CB_format.SelectedValue = GlobalSettings.Instance.DefaultImageExport.Codec;
        }

        public ImageActions(ExportFileInfoImage exportInfo) {
            InitializeComponent();
            ExportInfo = exportInfo;
            DataContext = ExportInfo;
            SL_quality.Value = GlobalSettings.Instance.DefaultImageExport.CompressionLevel;
            CB_format.SelectedValue = exportInfo.Codec;
            Btn_AddToQueue.Visibility = Visibility.Hidden;
            Btn_ExportNow.Visibility = Visibility.Hidden;
        }

        private void ImageActions_Loaded(object sender, RoutedEventArgs e) {
            ViewableController.FileLoaded += ViewableController_FileLoaded;
            if (ExportInfo == null) {
                ExportInfo = new ExportFileInfoImage();
                DataContext = ExportInfo;
            }
            CB_format.SelectedValue = ExportInfo.Codec;
        }

        private void ImageActions_Unloaded(object sender, RoutedEventArgs e) {
            ViewableController.FileLoaded -= ViewableController_FileLoaded;
        }

        public void ViewableController_FileLoaded(object sender, FileLoadedEventArgs e) {
            if (e.FileType != SupportedFileTypes.Image) return;
            
            if (e.Item != null) {
                ExportInfo = new ExportFileInfoImage(e.Item);
                DataContext = ExportInfo;
                
                // Обновляем значения выходного файла
                if (ExportInfo.ExportWidth == -1) {
                    TB_ExportWidth.Text = "auto";
                } else {
                    TB_ExportWidth.Text = ExportInfo.ExportWidth.ToString();
                }
                
                if (ExportInfo.ExportHeight == -1) {
                    TB_ExportHeight.Text = "auto";
                } else {
                    TB_ExportHeight.Text = ExportInfo.ExportHeight.ToString();
                }
            }
        }
        private void Btn_AddToQueue_Click(object sender, RoutedEventArgs e) {
            if (ExportInfo == null)
                return;

            // Обновляем формат из ComboBox
            if (CB_format.SelectedValue != null) {
                ExportInfo.Codec = (ImageFormat)Enum.Parse(typeof(ImageFormat), CB_format.SelectedValue.ToString());
            }

            Log.Update($"Файл {ExportInfo.Name} добавлен в очередь");
            ExportQueue.Enqueue(ExportInfo);
        }
        private void Btn_ExportNow_Click(object sender, RoutedEventArgs e) {
            GlobalSettings.Instance.ExportSettings.ExportFile(ExportInfo, default);
        }
    }
}
