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

namespace ClipsOrganizer.ExportControls
{
    /// <summary>
    /// Логика взаимодействия для EncodeFilesControl.xaml
    /// </summary>
    public partial class EncodeFilesControl : UserControl
    {
        public EncodeFilesControl()
        {
            InitializeComponent();
            CB_Codec.ItemsSource = Enum.GetValues(typeof(VideoCodec)).Cast<VideoCodec>();
            DataContext = GlobalSettings.Instance.ExportSettings;
            SP_EncodeEnable.IsEnabled = GlobalSettings.Instance.ExportSettings.EncodeEnabled;
            SL_MaxParallelTasks.IsEnabled = GlobalSettings.Instance.ExportSettings.EnableParallelExport;
        }

        private void TB_IsNumber_check(object sender, TextCompositionEventArgs e) {
            e.Handled = !InputValidator.IsNumber(e.Text,sender);
        }

        private void CB_EncodeEnabled_Checked_Changed(object sender, RoutedEventArgs e) {
            SP_EncodeEnable.IsEnabled = (sender as CheckBox).IsChecked == true;
        }

        private void CB_EnableParallelExport_Checked_Changed(object sender, RoutedEventArgs e) {
            SL_MaxParallelTasks.IsEnabled = (sender as CheckBox).IsChecked == true;
        }
    }
}
