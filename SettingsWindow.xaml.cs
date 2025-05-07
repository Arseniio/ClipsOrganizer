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
using ClipsOrganizer.Profiles;
using ClipsOrganizer.Settings;
using ClipsOrganizer.SettingsControls;

namespace ClipsOrganizer {
    /// <summary>
    /// Логика взаимодействия для SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window {
        GlobalSettings settings;
        Profile Profile;
        public SettingsWindow(Settings.GlobalSettings settings, Profile profile) {
            InitializeComponent();
            this.settings = settings;
            this.Profile = profile;
            CC_Content.Content = new GeneralSettings();
        }
        private void LB_Menu_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (LB_Menu.SelectedItem is ListBoxItem selectedItem) {
                string tag = selectedItem.Tag as string;
                switch(tag){
                    case "General":
                        CC_Content.Content = new GeneralSettings();
                        break;
                    case "Profiles":
                        CC_Content.Content = new ProfilesSettings();
                        break;
                }
            }
        }
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
            ((MainWindow)App.Current.MainWindow).UpdateItems();
        }
    }
}
