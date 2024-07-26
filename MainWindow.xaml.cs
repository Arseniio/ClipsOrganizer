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
using System.Runtime.Remoting.Channels;
using System.Windows.Controls.Primitives;
using System.IO;

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

            settings = new Settings.Settings("H:\\nrtesting");

            settings.SettingsFile.LoadSettings();


            InitializeComponent();
            if (settings.collections.Count == 0) {
                Btn_Mark.ContextMenu.Items.Add("No Collections");
            }
            else {
                settings.collections.ForEach(x =>
                {
                    var MI = new MenuItem { Header = x.CollectionTag };
                    MI.Tag = x;
                    MI.Click += MI_CT_mark_Click;
                    Btn_Mark.ContextMenu.Items.Add(MI);
                });
                }

            TV_clips_collections.ItemsSource = settings.collections;

            TV_clips.ItemsSource = items;

            #region slider timer init
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(400);
            timer.Tick += VideoDurationUpdate;
            #endregion
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
            TV_clips.ItemsSource = itemProvider.GetItemsFromFolder("H:\\nrtesting", true);
            TV_clips_collections.ItemsSource = itemProvider.GetItemsFromCollections(settings.collections, true);
        }
        private void CB_ParsedFileName_Unchecked(object sender, RoutedEventArgs e) {
            TV_clips.ItemsSource = itemProvider.GetItemsFromFolder("H:\\nrtesting");
            TV_clips_collections.ItemsSource = itemProvider.GetItemsFromCollections(settings.collections);

        }
        #region Clip selection
        private void TV_clips_MouseDoubleClick(object sender, MouseButtonEventArgs e) {
            TreeView tv = sender as TreeView;
            if (tv.SelectedItem == null) return;
            if (tv.SelectedItem.GetType() == typeof(CollectionFiles)) {
                ME_main.Source = new Uri((tv.SelectedItem as CollectionFiles).Path);
            }
            if (tv.SelectedItem.GetType() == typeof(FileItem)) {
                ME_main.Source = new Uri((tv.SelectedItem as FileItem).Path);
            }
            //TODO USE OR DELETE LATER
            if (tv.SelectedItem.GetType() == typeof(DirectoryItem)) { }
            if (tv.SelectedItem.GetType() == typeof(Collection)) { }
        }

        private void CB_sortType_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (this.IsLoaded) {
                DataContext = itemProvider.GetItemsFromFolder("H:\\nrtesting", (bool)CB_ParsedFileName.IsChecked, (Sorts)(CB_sortType.SelectedItem as ComboBoxItem).Tag);
                TV_clips.Items.Refresh();
            }
        }
        #endregion
        #region Marking clips
        private void Btn_Mark_Click(object sender, RoutedEventArgs e) {

        }

        private void MI_CT_mark_Click(object sender, RoutedEventArgs e) {
            var clickeditem = sender as MenuItem;
            //construct new collectionfile and write it to this file
            ME_main.Volume = 0;
            var utils = new FileUtils.FileUtils();

            //var fileinfo = utils.GetFileinfo(ME_main.Source.LocalPath);

            (clickeditem.Tag as Collection).Files.Add(new CollectionFiles
            {
                Date = new FileInfo(ME_main.Source.LocalPath).CreationTime,
                Path = ME_main.Source.LocalPath,
                FileIndexHigh = null,
                FileIndexLow = null,
                Name = ME_main.Source.AbsolutePath.Split('/')[0]
            });
        }
        #endregion
    }
}
