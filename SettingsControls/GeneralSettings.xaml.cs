using ClipsOrganizer.Model;
using ClipsOrganizer.Settings;
using DataValidation;
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
            foreach (ComboBoxItem item in SP_SortMethod.Items) {
                if (item.Tag.ToString() switch
                {
                    "Descending_date" => Sorts.Descending_date,
                    "Ascending_date" => Sorts.Ascending_date,
                    _ => Sorts.Default
                } == Settings.GlobalSettings.Instance.SortMethod) {
                    SP_SortMethod.SelectedItem = item;
                    break;
                }
            }
            Unloaded += GeneralSettings_Unloaded;
        }

        private void GeneralSettings_Unloaded(object sender, RoutedEventArgs e) {
            GlobalSettings.Instance.FFmpegpath = TB_ffmpegPath.Text;
        }

        private void Btn_OpenColorDialog_Click(object sender, RoutedEventArgs e) {
            System.Windows.Forms.ColorDialog colorDialog = new System.Windows.Forms.ColorDialog();
            if (colorDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                var color = colorDialog.Color;
                Btn_OpenColorDialog.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(color.R, color.G, color.B));
                TB_BGColor.Text = $"#{color.R:X2}{color.G:X2}{color.B:X2}";
                GlobalSettings.Instance.ImageBackgroundColor = $"#{color.R:X2}{color.G:X2}{color.B:X2}";
            }
        }

        private void TB_AutoPlayOffset_PreviewTextInput(object sender, TextCompositionEventArgs e) {
                if(InputValidator.MatchesExpectedBool(TimeSpan.TryParse(e.Text,out TimeSpan result), true, sender)){
                e.Handled = false;
            }
        }

        private void SP_SortMethod_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            Settings.GlobalSettings.Instance.SortMethod = (SP_SortMethod.SelectedItem as ComboBoxItem).Tag.ToString() switch
            {
                "Descending_date" => Sorts.Descending_date,
                "Ascending_date" => Sorts.Ascending_date,
                _ => Sorts.Default
            };
        }       
    }
}
