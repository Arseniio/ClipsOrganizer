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


    public partial class CommonSettings : UserControl {
        public ExportFileInfoVideo defaultExportVideo = GlobalSettings.Instance.DefaultVideoExport;
        public ExportFileInfoImage defaultExportImage = GlobalSettings.Instance.DefaultImageExport;
        public CommonSettings() {
            InitializeComponent();
            CB_codec.ItemsSource = Enum.GetValues(typeof(VideoCodec)).Cast<VideoCodec>();
            ResolutionComboBox.SelectionChanged += (s, e) =>
            {
                CustomResolutionPanel.Visibility = defaultExportVideo.Resolution == ResolutionType.Custom
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            };
            ResolutionComboBox.ItemsSource = Enum.GetValues(typeof(ResolutionType)).Cast<ResolutionType>();
            AudioCodecComboBox.ItemsSource = Enum.GetValues(typeof(AudioCodec)).Cast<AudioCodec>();
            AudioCodecComboBox.SelectedIndex = 0;
        }

        private void ValidateNumberInput(object sender, RoutedEventArgs e) {
            if (sender is TextBox textBox) {
                InputValidator.SetUnderline((InputValidator.IsNumber(textBox.Text) || textBox.Text == "auto"), textBox);
            }
        }

        private void ValidateDoubleInput(object sender, RoutedEventArgs e) {
            if (sender is TextBox textBox) {
                InputValidator.SetUnderline((InputValidator.IsNumber(textBox.Text) || textBox.Text == "auto"), textBox);
            }
        }

        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (sender is TabControl tabControl && (tabControl.SelectedItem is TabItem tabItem)) {
                switch (tabItem.Name) {
                    case "TI_Video":
                        this.DataContext = defaultExportVideo;
                        break;
                    case "TI_Image":
                        //TODO: something broke with ImageExportSaving, fix
                        this.DataContext = GlobalSettings.Instance.DefaultImageExport;
                        break;
                }
            }
        }
    }
}
