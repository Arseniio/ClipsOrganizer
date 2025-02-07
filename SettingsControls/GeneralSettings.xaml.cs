using ClipsOrganizer.Settings;
using System;
using System.Collections.Generic;
using System.Drawing;
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

namespace ClipsOrganizer.SettingsControls {
    /// <summary>
    /// Логика взаимодействия для GeneralSettings.xaml
    /// </summary>
    public partial class GeneralSettings : UserControl {
        public GeneralSettings() {
            InitializeComponent();
            this.DataContext = GlobalSettings.Instance;
            //SP_SortMethod.ItemsSource = ;
        }

        private void Btn_OpenColorDialog_Click(object sender, RoutedEventArgs e) {
            System.Windows.Forms.ColorDialog colorDialog = new System.Windows.Forms.ColorDialog();
            if (colorDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                var color = colorDialog.Color;
                // Обновляем цвет фона кнопки
                Btn_OpenColorDialog.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(color.R, color.G, color.B));
                // Обновляем текстовое поле с HEX значением
                TB_BGColor.Text = $"#{color.R:X2}{color.G:X2}{color.B:X2}";
            }
        }
    }
}
