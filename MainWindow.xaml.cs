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
using System.Security.Principal;

namespace ClipsOrganizer {
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        DispatcherTimer timer;
        ItemProvider itemProvider = null;
        Settings.Settings settings = null;
        ContextMenu CT_mark = null;
        List<Item> Items = null;
        public MainWindow() {
            //constructing collection

            itemProvider = new ItemProvider();
            settings = new Settings.Settings("H:\\nrtesting");

            settings.SettingsFile.LoadSettings();

            Items = itemProvider.GetItemsFromFolder("H:\\nrtesting", collections: settings.collections);


            InitializeComponent();
            CT_mark = new ContextMenu() { Name = "CT_mark" };

            if (settings.collections.Count == 0) {
                CT_mark.Items.Add("No Collections");
            }
            else {
                UpdateCollections();
            }


            Btn_Mark.ContextMenu = CT_mark;
            TV_clips.ContextMenu = CT_mark;
            //Костыль?
            Btn_Mark.ContextMenu.Tag = Btn_Mark;
            TV_clips.ContextMenu.Tag = TV_clips;

            TV_clips_collections.ItemsSource = settings.collections;

            TV_clips.ItemsSource = Items;

            #region slider timer init
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(400);
            timer.Tick += VideoDurationUpdate;
            #endregion
        }

        private void UpdateCollections() {
            CT_mark.Items.Clear();
            settings.collections.ForEach(x =>
            {
                var MI = new MenuItem { Header = x.CollectionTag };
                MI.Tag = x;
                MI.Click += MI_CT_mark_Click;
                CT_mark.Items.Add(MI);
            });
            var MI_create = new MenuItem { Header = "Create new collection" };
            MI_create.Click += create_collection;
            CT_mark.Items.Add(new Separator());
            CT_mark.Items.Add(MI_create);
        }

        private void UpdateColors() {
            Items = itemProvider.GetItemsFromFolder("H:\\nrtesting", collections: settings.collections);
            TV_clips.Items.Refresh();
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
            if (tv.SelectedItem.GetType() == typeof(Item)) {
                ME_main.Source = new Uri((tv.SelectedItem as Item).Path);
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
            settings.SettingsFile.WriteSettings();
        }

        private void MI_CT_mark_Click(object sender, RoutedEventArgs e) {
            var clickeditem = sender as MenuItem;
            var clickedItem = sender as MenuItem;
            var sourceElement = (clickedItem.Parent as ContextMenu)?.Tag as FrameworkElement;
            //construct new collectionfile and write it to this file
            ME_main.Volume = 0;
            var utils = new FileUtils.FileUtils();
            if (sourceElement == Btn_Mark) {
                (clickeditem.Tag as Collection).Files.Add(new Item
                {
                    Date = new FileInfo(ME_main.Source.LocalPath).CreationTime,
                    Path = ME_main.Source.LocalPath,
                    Color = itemProvider.FindColorByCollections(clickeditem.Tag as Collection, ME_main.Source.AbsolutePath.Split('/').Last()),
                    Name = ME_main.Source.AbsolutePath.Split('/').Last()
                });
            }
            else if (sourceElement == TV_clips) {
                if (TV_clips.SelectedItem == null) return;
                FileItem SelectedItem = TV_clips.SelectedItem as FileItem;
                (clickeditem.Tag as Collection).Files.Add(new Item
                {
                    Path = SelectedItem.Path,
                    Date = SelectedItem.Date,
                    Name = SelectedItem.Name,
                    Color = (clickeditem.Tag as Collection).Color,
                });
            }
            TV_clips_collections.Items.Refresh();
            UpdateColors();
        }
        private void create_collection(object sender, RoutedEventArgs e) {
            Window window = new CollectionCreatorWindow();
            if (window.ShowDialog() == true) {
                Collection collection = (window as CollectionCreatorWindow).Collection;
                settings.collections.Add(collection);
                UpdateCollections();
                TV_clips_collections.Items.Refresh();
            }
        }
        #endregion

    }
}
