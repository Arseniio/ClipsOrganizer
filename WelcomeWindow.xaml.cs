using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;

namespace ClipsOrganizer {
    /// <summary>
    /// Логика взаимодействия для WelcomeWindow.xaml
    /// </summary>
    public partial class WelcomeWindow : Window {
        public WelcomeWindow() {
            InitializeComponent();
        }
        public string ClipsPath = string.Empty;
        public string ProfileName = string.Empty;
        public string ffmpegPath = string.Empty;
        private void Btn_Continue_Click(object sender, RoutedEventArgs e) {
            if (!TB_ffmpeg.Text.Contains("ffmpeg.exe")) {
                if (string.IsNullOrWhiteSpace(TB_ffmpeg.Text)) {
                    var result = MessageBox.Show("Вы не указали путь к ffmpeg, точно хотите продолжить?", "Ошибка", MessageBoxButton.YesNo);
                    if (result == MessageBoxResult.No) {
                        return;
                    }
                }
            }
            else ffmpegPath = TB_ffmpeg.Text;
            StringBuilder err = new StringBuilder();
            if (string.IsNullOrWhiteSpace(TB_Path.Text)) err.AppendLine("Невозможно создать профиль без названия");
            if (string.IsNullOrWhiteSpace(TB_Profile.Text)) err.AppendLine("Невозможно создать профиль без указания рабочей директории");
            DirectoryInfo directoryInfo = new DirectoryInfo(TB_Path.Text);
            if (!directoryInfo.Exists) err.AppendLine("Невозможно найти папку для рабочей директории");
            if (err.Length > 0) {
                MessageBox.Show(err.ToString());
                return;
            }
            ProfileName = TB_Profile.Text;
            ClipsPath = TB_Path.Text;
            this.DialogResult = true;
            this.Close();
        }
    }

}
