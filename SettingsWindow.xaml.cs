using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using ClipsOrganizer.Settings;

namespace ClipsOrganizer {
    /// <summary>
    /// Логика взаимодействия для SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window {
        public VideoCodec LastUsedCodec { get; set; }
        public string LastUsedQuality { get; set; }
        public string LastUsedEncoderPath { get; set; }
        public SettingsWindow(Settings.Settings settings) {
            InitializeComponent();
            //CB_codec.ItemsSource = Enum.GetValues(typeof(VideoCodec)).Cast<VideoCodec>();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) {

        }

    }
}
