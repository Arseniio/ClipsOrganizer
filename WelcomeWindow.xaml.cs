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
        public string ffmpegPath = string.Empty;
        private void Btn_Continue_Click(object sender, RoutedEventArgs e) {
            //if (TB_ffmpeg.Text.EndsWith("ffmpeg.exe")) {
            if (string.IsNullOrWhiteSpace(TB_ffmpeg.Text)) {
                var result = MessageBox.Show("Вы не указали путь к ffmpeg, точно хотите продолжить?", "Ошибка", MessageBoxButton.YesNo);
                if (result == MessageBoxResult.No) {
                    return;
                }
            }
            else ffmpegPath = TB_ffmpeg.Text;
            if (!string.IsNullOrWhiteSpace(TB_Path.Text)) {
                ClipsPath = TB_Path.Text;
                this.DialogResult = true;
                this.Close();
            }
        }

        private void TB_Path_TextChanged(object sender, TextChangedEventArgs e) {

        }
    }
}
