using ClipsOrganizer.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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
using System.Windows.Threading;
using ClipsOrganizer.Settings;
using ClipsOrganizer.Collections;
using System.Runtime.InteropServices;

namespace ClipsOrganizer {
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        DispatcherTimer timer;
        ItemProvider itemProvider = null;
        Settings.Settings settings = null;
        public MainWindow() {
            //constructing collection

            itemProvider = new ItemProvider();
            var items = itemProvider.GetItemsFromFolder("H:\\nrtesting");
            List<Collection> CL = new List<Collection>();
            foreach (DirectoryItem item in items) {
                CL.Add(new Collection(item));
            }
            settings = new Settings.Settings("H:\\nrtesting", CL);
            settings.SettingsFile.LoadSettings();

            var test = itemProvider.GetItemsFromCollections(settings.collections);

            //settings = new Settings.Settings("H:\\nrtesting");

            InitializeComponent();

            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(400);
            timer.Tick += VideoDurationUpdate;
            var test3 = new CollectionUIProvider();
            test3.collections = CL;
            TV_clips_collections.ItemsSource = test3;
            TV_clips.DataContext = items;

        }

        #region sliders events
        private void SL_duration_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e) {
            is_dragging = false;
            ME_main.Position = TimeSpan.FromSeconds((double)SL_duration.Value);
        }

        private void MainGrid_SizeChanged(object sender, SizeChangedEventArgs e) {
            if (MainGrid.ActualWidth - 700 > 0) {
                SL_duration.Width = MainGrid.ActualWidth - 700;
            }
        }
        bool is_dragging = false;
        private void SL_duration_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
            if (!is_dragging) ME_main.Position = TimeSpan.FromSeconds((double)SL_duration.Value);
        }

        private void SL_duration_DragStarted(object sender, System.Windows.Controls.Primitives.DragStartedEventArgs e) {
            is_dragging = true;
        }

        private void VideoDurationUpdate(object sender, EventArgs e) {
            if (!is_dragging)
                SL_duration.Value = ME_main.Position.TotalSeconds;
        }
        #endregion

        #region player events
        private void Btn_Play_Click(object sender, RoutedEventArgs e) {
            ME_main.Play();
        }

        private void SL_volume_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
            ME_main.Volume = (double)e.NewValue;
        }

        private void ME_main_MediaOpened(object sender, RoutedEventArgs e) {
            if (!ME_main.NaturalDuration.HasTimeSpan) {
                SL_duration.Maximum = ME_main.NaturalDuration.TimeSpan.TotalSeconds;
                timer.Start();
            }
        }

        private void Btn_Stop_Click(object sender, RoutedEventArgs e) {
            ME_main.Stop();
        }
        #endregion

        private void CB_ParsedFileName_Checked(object sender, RoutedEventArgs e) {
            TV_clips.DataContext = itemProvider.GetItemsFromFolder("H:\\nrtesting", true);
        }
        private void CB_ParsedFileName_Unchecked(object sender, RoutedEventArgs e) {
            TV_clips.DataContext = itemProvider.GetItemsFromFolder("H:\\nrtesting");
        }

        private void TV_clips_MouseDoubleClick(object sender, MouseButtonEventArgs e) {
            if (TV_clips.SelectedItem == null) return;
            if (TV_clips.SelectedItem.GetType() == typeof(FileItem)) {
                ME_main.Source = new Uri((TV_clips.SelectedItem as FileItem).Path);
            }
            if (TV_clips.SelectedItem.GetType() == typeof(DirectoryItem)) { }
        }

        private void CB_sortType_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (this.IsLoaded) {
                DataContext = itemProvider.GetItemsFromFolder("H:\\nrtesting", (bool)CB_ParsedFileName.IsChecked, (Sorts)(CB_sortType.SelectedItem as ComboBoxItem).Tag);
                TV_clips.Items.Refresh();
            }
        }
    }
}
