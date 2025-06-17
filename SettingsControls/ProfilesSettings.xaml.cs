using ClipsOrganizer.FileUtils;
using ClipsOrganizer.Profiles;
using ClipsOrganizer.Properties;
using ClipsOrganizer.Settings;
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
using System.Windows.Navigation;
using System.Windows.Shapes;


namespace ClipsOrganizer.SettingsControls {
    /// <summary>
    /// Логика взаимодействия для ProfilesSettings.xaml
    /// </summary>
    /// 
    public partial class ProfilesSettings : UserControl {
        public ProfilesSettings() {
            InitializeComponent();
            LB_Profiles.ItemsSource = ProfileManager.LoadAllProfiles();
        }
        private Profile OldProfile { get; set; }
        private void LB_Profiles_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (LB_Profiles.SelectedItem == null) return;
            OldProfile = FileSerializer.ReadFile<Profile>($"{ProfileManager.ProfilePath}/{LB_Profiles.SelectedItem}.json");
            SP_ProfileData.DataContext = FileSerializer.ReadFile<Profile>($"{ProfileManager.ProfilePath}/{LB_Profiles.SelectedItem}.json");
        }

        private void Btn_AddProfile_Click(object sender, RoutedEventArgs e) {
            Profile NewProfile = new Profile();
            NewProfile.ProfileName = "Новый профиль";
            SP_ProfileData.DataContext = NewProfile;
            OldProfile = null;
        }

        private void Btn_DeleteProfile_Click(object sender, RoutedEventArgs e) {
            MessageBoxResult result = MessageBox.Show("Точно хотите удалить выбранный профиль?", "Предупреждение", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes) {
                //TODO: Fix Later 
                File.Delete((SP_ProfileData.DataContext as Profile).ProfilePath);
                File.Delete((SP_ProfileData.DataContext as Profile).ProfileBkpPath);
                LB_Profiles.ItemsSource = ProfileManager.LoadAllProfiles();
            }
        }

        private void Btn_SaveProfile_Click(object sender, RoutedEventArgs e) {
            Profile currentProfile = SP_ProfileData.DataContext as Profile;
            if (currentProfile == null) return;
            if (OldProfile != null) {
                File.Delete(OldProfile.ProfilePath);
                File.Delete(OldProfile.ProfileBkpPath);
            }
            if (!Directory.Exists(currentProfile.ClipsFolder)) {
                MessageBox.Show("Не найдена рабочая папка для профиля");
                return;
            }
            FileSerializer.WriteAndCreateBackupFile(currentProfile, currentProfile.ProfilePath);

            LB_Profiles.ItemsSource = ProfileManager.LoadAllProfiles();
        }
    }
}
