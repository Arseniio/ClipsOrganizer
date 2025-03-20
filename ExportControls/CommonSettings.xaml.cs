using ClipsOrganizer.Model;
using ClipsOrganizer.Settings;
using DataValidation;
using System;
using System.Collections.Generic;
using System.Globalization;
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
    /// Логика взаимодействия для CommonSettings.xaml
    /// </summary>

    public class ExportAutoConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value is int intValue) {
                return intValue == -1 ? "auto" : intValue.ToString();
            }
            return "auto"; // Если значение null или не int
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value is string strValue) {
                return strValue == "auto" ? -1 : int.TryParse(strValue, out int result) ? result : -1;
            }
            return -1;
        }
    }

    public partial class CommonSettings : UserControl {
        public ExportFileInfoVideo defaultExportVideo = GlobalSettings.Instance.DefaultVideoExport;
        public ExportFileInfoImage defaultExportImage = GlobalSettings.Instance.DefaultImageExport;
        public CommonSettings() {
            InitializeComponent();
            CB_codec.ItemsSource = Enum.GetValues(typeof(VideoCodec)).Cast<VideoCodec>();
            ResolutionComboBox.ItemsSource = Enum.GetValues(typeof(ResolutionType)).Cast<ResolutionType>();
            ResolutionComboBox.SelectionChanged += (s, e) =>
            {
                CustomResolutionPanel.Visibility = defaultExportVideo.Resolution == ResolutionType.Custom
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            };
            AudioCodecComboBox.ItemsSource = Enum.GetValues(typeof(AudioCodec)).Cast<AudioCodec>();
            AudioCodecComboBox.SelectedIndex = 0;
        }

        private void ValidateNumberInput(object sender, RoutedEventArgs e) {
            if (sender is TextBox textBox) {
                InputValidator.IsNumber(textBox.Text, textBox);
            }
        }

        private void ValidateDoubleInput(object sender, RoutedEventArgs e) {
            if (sender is TextBox textBox) {
                InputValidator.MatchesPattern(textBox.Text, @"^\d*\.?\d*$", textBox);
            }
        }

        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (sender is TabControl tabControl && (tabControl.SelectedItem is TabItem tabItem)) {
                switch (tabItem.Name) {
                    case "TI_Video":
                        this.DataContext = defaultExportVideo;
                        break;
                    case "TI_Image":
                        this.DataContext = defaultExportImage;
                        break;
                }
            }
        }
    }
}
