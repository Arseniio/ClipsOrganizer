using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
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

        private string getNextFileName(string FilePath) {
            int lastFileSaved = 0;
            foreach (var item in Directory.EnumerateFiles(FilePath)) {
                if (int.TryParse(item.Split('_').Last().Split('.').First(), out int fileNumber)) {
                    if (fileNumber > lastFileSaved) {
                        lastFileSaved = fileNumber;
                    }
                }
            }
            lastFileSaved++;
            return System.IO.Path.Combine(System.IO.Path.GetDirectoryName(VideoPath.AbsolutePath), string.Format("exported_{0}.mp4", lastFileSaved));
        }

        public RendererWindow(Settings.Settings settings, TimeSpan Crop_From, TimeSpan Crop_To, Uri VideoPath) {
            InitializeComponent();
            this.settings = settings;
            this.VideoPath = VideoPath;
            CB_codec.ItemsSource = Enum.GetValues(typeof(VideoCodec)).Cast<VideoCodec>();
            CB_codec.SelectedIndex = 0; //change later
            TB_outputPath.Text = getNextFileName(Path.GetDirectoryName(VideoPath.AbsolutePath));
            TB_Crop_From.Text = Crop_From.ToString();
            TB_Crop_To.Text = Crop_To.ToString();
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
            if (CB_codec.SelectedItem != null && !string.IsNullOrEmpty(TB_Quality.Text) && int.TryParse(TB_Quality.Text, out int bitrate)) {
                if (TimeSpan.TryParse(TB_Crop_From.Text, out TimeSpan startTime) && TimeSpan.TryParse(TB_Crop_To.Text, out TimeSpan endTime)) {
                    settings.ffmpegManager.StartEncodingAsync(VideoPath.LocalPath, TB_outputPath.Text, VideoCodec.H264_NVENC, bitrate, startTime, endTime);
                }
                else {
                    settings.ffmpegManager.StartEncodingAsync(VideoPath.LocalPath, TB_outputPath.Text, VideoCodec.H264_NVENC, bitrate);
                }
            }
            else {
                MessageBox.Show("Пожалуйста, убедитесь, что выбраны параметры кодека и указано значение качества.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

        }
    }
}
