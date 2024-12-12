using ClipsOrganizer.Settings;
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

namespace ClipsOrganizer.ExportControls {
    /// <summary>
    /// Логика взаимодействия для ExportGeneralControl.xaml
    /// </summary>
    public partial class ExportGeneralControl : UserControl {
        public ExportGeneralControl() {
            InitializeComponent();
            DataContext = GlobalSettings.Instance.ExportSettings;
        }

        private void TB_TargetFolder_PreviewTextInput(object sender, TextCompositionEventArgs e) {
            TB_FolderExists.Visibility = InputValidator.IsValidFolderPath(TB_FolderExists.Text) == true ? Visibility.Hidden : Visibility.Visible;
        }
    }
}
