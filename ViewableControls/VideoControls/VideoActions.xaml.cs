using ClipsOrganizer.Model;
using ClipsOrganizer.Settings;
using DataValidation;
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

namespace ClipsOrganizer.ViewableControls.VideoControls {
    /// <summary>
    /// Логика взаимодействия для VideoActions.xaml
    /// </summary>
    /// 

    public partial class VideoActions : UserControl {
        public ExportFileInfoVideo ExportInfo { get; set; }
        public VideoActions() {
            InitializeComponent();
            InitializeControls();
            ViewableController.FileLoaded += ViewableController_FileLoaded;
            this.Unloaded += VideoActions_Unloaded;
            this.Loaded += VideoActions_Loaded;
        }
        private void InitializeControls() {
            // Video Codec
            CB_codec.ItemsSource = Enum.GetValues(typeof(VideoCodec)).Cast<VideoCodec>();

            // Resolution
            ResolutionComboBox.ItemsSource = Enum.GetValues(typeof(ResolutionType)).Cast<ResolutionType>();
            ResolutionComboBox.SelectionChanged += (s, e) =>
            {
                CustomResolutionPanel.Visibility = ExportInfo.Resolution == ResolutionType.Custom
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            };

            // Audio Codec
            AudioCodecComboBox.ItemsSource = Enum.GetValues(typeof(AudioCodec)).Cast<AudioCodec>();
            AudioCodecComboBox.SelectedIndex = 0;
        }


        public VideoActions(ExportFileInfoVideo exportInfo) {
            InitializeComponent();
            ExportInfo = exportInfo;
            DataContext = ExportInfo;
            CB_codec.ItemsSource = Enum.GetValues(typeof(VideoCodec)).Cast<VideoCodec>();
            CB_codec.SelectedIndex = 0; //TODO: change later
            Btn_AddToQueue.Visibility = Visibility.Hidden;
            Btn_ExportNow.Visibility = Visibility.Hidden;
        }

        private void VideoActions_Loaded(object sender, RoutedEventArgs e) {
            ViewableController.FileLoaded += ViewableController_FileLoaded;
            if (ExportInfo == null) {
                ExportInfo = new ExportFileInfoVideo();
                DataContext = ExportInfo;
            }
        }

        private void VideoActions_Unloaded(object sender, RoutedEventArgs e) {
            ViewableController.FileLoaded -= ViewableController_FileLoaded;
        }
        public void ViewableController_FileLoaded(object sender, FileLoadedEventArgs e) {
            if (e.Item != null) {
                ExportInfo = new ExportFileInfoVideo(e.Item);
                DataContext = ExportInfo;
            }
        }
        private void ValidateNumberInput(object sender, RoutedEventArgs e) {
            if (sender is TextBox textBox) {
                InputValidator.SetUnderline((InputValidator.IsNumber(textBox.Text) || textBox.Text == "auto"), textBox);
            }
        }

        private void ValidateDoubleInput(object sender, RoutedEventArgs e) {
            if (sender is TextBox textBox) {
                InputValidator.MatchesPattern(textBox.Text, @"^\d*\.?\d*$", textBox);
            }
        }
        private void Btn_AddToQueue_Click(object sender, RoutedEventArgs e) {
            if (ExportInfo == null)
                return;

            Log.Update($"Файл {ExportInfo.Name} добавлен в очередь");
            ExportQueue.Enqueue(ExportInfo);
        }

        private void Btn_ExportNow_Click(object sender, RoutedEventArgs e) {

        }

    }
}
