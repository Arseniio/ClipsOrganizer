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
using System.Windows.Threading;

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
        DispatcherTimer timer;

        public RendererWindow(Settings.Settings settings, Uri VideoPath, TimeSpan? Crop_From = null, TimeSpan? Crop_To = null) {
            InitializeComponent();
            this.settings = settings;
            this.VideoPath = VideoPath;
            CB_codec.ItemsSource = Enum.GetValues(typeof(VideoCodec)).Cast<VideoCodec>();
            CB_codec.SelectedIndex = 0; //change later
            TB_outputPath.Text = getNextFileName(Path.GetDirectoryName(VideoPath.AbsolutePath));
            if (Crop_From != null || Crop_To != null) {
                TB_Crop_From.Text = Crop_From.ToString() ?? TimeSpan.Zero.ToString();
                TB_Crop_To.Text = Crop_To.ToString() ?? TimeSpan.Zero.ToString();
            }
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(400); //yeah timer updates every 400ms
            timer.Tick += FFmpegchecker;
        }
        //rewrite with regex parsing from output info from ffmpeg executable
        private void FFmpegchecker(object sender, System.EventArgs e) {
            if (this.settings.ffmpegManager.IsProcessRunning) {
                tb_status.Text = "В процессе обраотки видео";
            }
            else {
                tb_status.Text = "Закончено";
                timer.Stop();
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
            ComboBoxItem selectedItem = CB_codec.SelectedItem as ComboBoxItem;
            if (selectedItem != null && selectedItem.Tag is VideoCodec codec) {
                LastUsedCodec = codec;
            }
            this.DialogResult = true;
            //LastUsedQuality = 
            //LastUsedEncoderPath =
        }

        private void Btn_Crop_Click(object sender, RoutedEventArgs e) {
            VideoCodec selectedCodec = (VideoCodec)CB_codec.SelectedItem;
            if (CB_codec.SelectedItem != null && selectedCodec != VideoCodec.Unknown && !string.IsNullOrEmpty(TB_Quality.Text) && int.TryParse(TB_Quality.Text, out int bitrate)) {
                if (TimeSpan.TryParse(TB_Crop_From.Text, out TimeSpan startTime) && TimeSpan.TryParse(TB_Crop_To.Text, out TimeSpan endTime)) {
                    if (startTime == TimeSpan.Zero && startTime > endTime) {
                        MessageBox.Show("Невозможно обрезать клип в обратную сторону.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);

                    }
                    settings.ffmpegManager.StartEncodingAsync(VideoPath.LocalPath, TB_outputPath.Text, selectedCodec, bitrate, startTime, endTime);
                    timer.Start();
                }
                else {
                    settings.ffmpegManager.StartEncodingAsync(VideoPath.LocalPath, TB_outputPath.Text, selectedCodec, bitrate);
                    timer.Start();
                }
            }
            else {
                MessageBox.Show("Пожалуйста, убедитесь, что выбраны параметры кодека и указано значение качества.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}
