using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ClipsOrganizer {
    /// <summary>
    /// Логика взаимодействия для RendererWindow.xaml
    /// </summary>
    public partial class RendererWindow : Window {
        Settings.Settings settings;
        Uri VideoPath;
        public VideoCodec LastUsedCodec { get; set; }
        public string LastUsedQuality { get; set; }
        public string LastUsedEncoderPath { get; set; }

        public RendererWindow(Settings.Settings settings, TimeSpan Crop_From, TimeSpan Crop_To, Uri VideoPath) {
            InitializeComponent();
            this.settings = settings;
            this.VideoPath = VideoPath;
            CB_codec.ItemsSource = Enum.GetValues(typeof(VideoCodec)).Cast<VideoCodec>();
            TB_outputPath.Text = VideoPath.AbsolutePath + "/exported.mp4";
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
            ComboBoxItem selectedItem = CB_codec.SelectedItem as ComboBoxItem;
            if (selectedItem != null && selectedItem.Tag is VideoCodec codec) {
                LastUsedCodec = codec;
            }
            //LastUsedQuality = 
            //LastUsedEncoderPath =
        }

        private void Btn_Crop_Click(object sender, RoutedEventArgs e) {
            if (CB_codec.Tag != null || TB_Quality != null) {
                settings.ffmpegManager.ConvertVideo(VideoPath.AbsolutePath, TB_outputPath.Text, VideoCodec.H264_NVENC, int.Parse(TB_Quality.Text));
            }
        }
    }
}
