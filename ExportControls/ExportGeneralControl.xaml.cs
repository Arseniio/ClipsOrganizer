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
using MaterialDesignThemes.Wpf;
using System.Windows.Shapes;
using MaterialDesignColors;
using System.IO;

namespace ClipsOrganizer.ExportControls {
    /// <summary>
    /// Логика взаимодействия для ExportGeneralControl.xaml
    /// </summary>
    public partial class ExportGeneralControl : UserControl {
        public ExportGeneralControl() {
            InitializeComponent();
            DataContext = GlobalSettings.Instance.ExportSettings;
            //SL_ExportFileNameTepmlate.IsEnabled = SL_FileNameTemplate.IsEnabled = GlobalSettings.Instance.ExportSettings.UseRegex;
        }

        private void CB_UseRegex_CheckedChanged(object sender, RoutedEventArgs e) {
            //if (sender is CheckBox checkBox) {
            //    SL_FileNameTemplate.IsEnabled = checkBox.IsChecked == true;
            //    SL_ExportFileNameTepmlate.IsEnabled = checkBox.IsChecked == true;
            //}

        }

        private void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e) {
            if (e.Text.Length > 0) {
                if (!InputValidator.IsNumber(e.Text, sender)) {
                    e.Handled = true;
                    return;
                }
                TextFieldAssist.SetSuffixText(sender as TextBox, "МБ");
            }
        }
        private void TextBox_PreviewKeyDown(object sender, KeyEventArgs e) {
            if (sender is TextBox textBox && (e.Key == Key.Back || e.Key == Key.Delete)) {
                string remainingText = textBox.Text.Length > 0 && textBox.SelectionStart > 0
                    ? textBox.Text.Remove(textBox.SelectionStart - 1, 1)
                    : textBox.Text;


                if (string.IsNullOrEmpty(remainingText)) {
                    TextFieldAssist.SetSuffixText(textBox, "");
                }
            }
        }

        private void CB_EnableLogging_CheckedChanged(object sender, RoutedEventArgs e) {
            //if (sender is CheckBox checkBox) {
            //    SP_LogPath.IsEnabled = checkBox.IsChecked == true;
            //}
        }
        private void TB_TargetFolder_PreviewKeyUp(object sender, KeyEventArgs e) {
            InputValidator.IsFolderExists((sender as TextBox).Text, sender);
        }

    }
}
