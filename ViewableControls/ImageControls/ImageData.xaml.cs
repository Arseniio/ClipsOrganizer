using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;

namespace ClipsOrganizer.ViewableControls.ImageControls {
    /// <summary>
    /// Логика взаимодействия для ImageActions.xaml
    /// </summary>
    public partial class ImageData : UserControl {
        public class MetadataItem {
            public string Name { get; set; }
            public string Value { get; set; }
        }

        public ObservableCollection<MetadataItem> MetadataItems { get; set; }
        public ImageData() {
            InitializeComponent();
            MetadataItems = new ObservableCollection<MetadataItem>();
            LV_metadata.ItemsSource = MetadataItems;
            ViewableController.FileLoaded += ViewableController_FileLoaded;
        }

        private void ViewableController_FileLoaded(object sender, FileLoadedEventArgs e) {
            if (File.Exists(e.FilePath)) {
                ExtractMetadata(e.FilePath);
            }
        }
        private void ExtractMetadata(string filePath) {
            MetadataItems.Clear();

            try {
                var directories = ImageMetadataReader.ReadMetadata(filePath);
                var format = directories.FirstOrDefault(d => d.Name.Contains("File Type"))?.GetDescription(1) ?? "Unknown";

                if (!format.Contains("RAW", StringComparison.OrdinalIgnoreCase) &&
                    !format.Contains("DNG", StringComparison.OrdinalIgnoreCase) &&
                    !format.Contains("CR2", StringComparison.OrdinalIgnoreCase) &&
                    !format.Contains("EXIF", StringComparison.OrdinalIgnoreCase)) {
                    Log.Update("Выбранное изображение не является RAW-файлом");
                    return;
                }

                var exifDir = directories.OfType<ExifSubIfdDirectory>().FirstOrDefault();
                if (exifDir != null) {
                    AddMetadata("ISO", exifDir, ExifDirectoryBase.TagIsoEquivalent);
                    AddMetadata("Выдержка", exifDir, ExifDirectoryBase.TagExposureTime);
                    AddMetadata("Диафрагма", exifDir, ExifDirectoryBase.TagFNumber);
                    AddMetadata("Фокусное расстояние", exifDir, ExifDirectoryBase.TagFocalLength);
                    AddMetadata("Баланс белого", exifDir, ExifDirectoryBase.TagWhiteBalance);
                    AddMetadata("Модель камеры", exifDir, ExifDirectoryBase.TagModel);
                    AddMetadata("Производитель камеры", exifDir, ExifDirectoryBase.TagMake);
                    AddMetadata("Дата съемки", exifDir, ExifDirectoryBase.TagDateTimeOriginal);
                }
                Log.Update("Метаданные успешно загружены");
            }
            catch (Exception ex) {
                Log.Update($"Ошибка при анализе файла: {ex.Message}");
            }
        }

        private void AddMetadata(string name, ExifSubIfdDirectory directory, int tag) {
            if (directory.ContainsTag(tag)) {
                MetadataItems.Add(new MetadataItem { Name = name, Value = directory.GetDescription(tag) });
            }
        }
    }
}
