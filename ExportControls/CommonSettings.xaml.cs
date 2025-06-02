using ClipsOrganizer.Classes;
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
        public ExportFileInfoAudio defaultExportAudio = GlobalSettings.Instance.DefaultAudioExport;
        public CommonSettings() {
            InitializeComponent();
            CB_codec.ItemsSource = Enum.GetValues(typeof(VideoCodec));
            CB_AudioFormat.ItemsSource = Enum.GetValues(typeof(ExportAudioFormat));
            ResolutionComboBox.ItemsSource = Enum.GetValues(typeof(ResolutionType));
            AudioCodecComboBox.ItemsSource = Enum.GetValues(typeof(AudioCodec));
            ResolutionComboBox.SelectionChanged += (s, e) =>
            {
                CustomResolutionPanel.Visibility = defaultExportVideo.Resolution == ResolutionType.Custom
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            };
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
            if (sender is TabControl tc && tc.SelectedItem is TabItem ti && ti.Content is FrameworkElement fe) {
                fe.DataContext = ti.Name switch
                {
                    "TI_Video" => defaultExportVideo,
                    "TI_Image" => defaultExportImage,
                    "TI_Audio" => defaultExportAudio,
                    _ => fe.DataContext
                };
                if (ti.Name == "TI_Image") {
                    CB_format.SelectedItem = defaultExportImage.OutputFormat;
                    SL_quality.Value = defaultExportImage.CompressionLevel;
                }
                else if (ti.Name == "TI_Audio") {
                    CB_AudioFormat.SelectedItem = defaultExportAudio.outputFormat;
                }
            }
        }

    }
}
