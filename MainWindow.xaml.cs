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
        string clipsPath;
        public MainWindow() {
            //constructing collection
            if (!File.Exists("./settings.json")) {
                Window window = new WelcomeWindow();
                if (window.ShowDialog() == true) {
                    clipsPath = (window as WelcomeWindow).ClipsPath;
                }
            }
            itemProvider = new ItemProvider();
            settings = new Settings.Settings(clipsPath);

            settings.SettingsFile.LoadSettings();

            Items = itemProvider.GetItemsFromFolder(settings.ClipsFolder, collections: settings.collections);


            InitializeComponent();
            CT_mark = new ContextMenu() { Name = "CT_mark" };
            if (settings.collections.Count == 0) {
                CT_mark.Items.Add("No Collections");
                AddNewCollectionMI();
            }
            else {
                UpdateCollectionsMI();
            }
            CT_mark.Opened += CT_mark_Opened;



            Btn_Mark.ContextMenu = CT_mark;
            TV_clips.ContextMenu = CT_mark;
            TV_clips_collections.ContextMenu = CT_mark;
            //Костыль?
            Btn_Mark.ContextMenu.Tag = Btn_Mark;
            TV_clips.ContextMenu.Tag = TV_clips;
            TV_clips_collections.Tag = TV_clips_collections;


            TV_clips_collections.ItemsSource = settings.collections;

            TV_clips.ItemsSource = Items;

            #region slider timer init
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(400);
            timer.Tick += VideoDurationUpdate;
            #endregion
        }

        private void CT_mark_Opened(object sender, RoutedEventArgs e) {
            if (sender is ContextMenu contextMenu) {
                if (contextMenu.PlacementTarget is FrameworkElement placementTarget) {
                    if (placementTarget.Tag != TV_clips_collections) {
                        UpdateCollectionsMI();
                        return;
                    }
                    if (!(TV_clips_collections.SelectedItem is null)) {
                        if (TV_clips_collections.SelectedItem.GetType() == typeof(Item)) {
                            var sel_item = TV_clips_collections.SelectedItem as Item;
                            var MI = new MenuItem { Header = "Remove" };

                            MI.Tag = sel_item;
                            MI.Click += MI_CT_remove_Click;
                            CT_mark.Items.Add(MI);
                        }
                        if (TV_clips_collections.SelectedItem.GetType() == typeof(Collection)) {
                            var sel_item = TV_clips_collections.SelectedItem as Collection;
                            var MI = new MenuItem { Header = "Edit" };

                            MI.Tag = sel_item;
                            MI.Click += MI_CT_edit_Click;
                            CT_mark.Items.Add(MI);
                        }
                    }
                }
            }
        }
        private void MI_CT_edit_Click(object sender, RoutedEventArgs e) {
            var ItemToEdit = (sender as MenuItem).Tag as Collection;
            var window = new CollectionCreatorWindow(ItemToEdit);
            window.ShowDialog();
            if (window.DialogResult.HasValue) {
                UpdateColors();
                UpdateCollectionsUI();
            }
        }

        private void MI_CT_remove_Click(object sender, RoutedEventArgs e) {
            var ItemToDelete = (sender as MenuItem).Tag as Item;
            Item item = new Item();
            List<Collection> foundCollections = new List<Collection>();
            foreach (var collection in settings.collections) {
                item = null;
                item = collection.Files.Find(p => p.Name == ItemToDelete.Name); //not sure if need to check only name,date or entire object
                if (!(item is null)) foundCollections.Add(collection);
            }
            if (foundCollections.Count > 2) {
                Window window = new CollectionDeletionWindow(foundCollections);
                bool? dialogResult = window.ShowDialog();
                if (dialogResult == true && (window as CollectionDeletionWindow).selectedCollections.Count > 0) {
                    foreach (var collection in (window as CollectionDeletionWindow).selectedCollections) {
                        item = null;
                        settings.collections.Find(p => p.CollectionTag == collection.CollectionTag).Files.RemoveAll(i => i.Name == ItemToDelete.Name);
                    }
                    UpdateCollectionsUI();
                }
            }
        }

        private void UpdateCollectionsMI() {
            CT_mark.Items.Clear();
            settings.collections.ForEach(x =>
            {
                var MI = new MenuItem { Header = x.CollectionTag };
                MI.Tag = x;
                MI.Click += MI_CT_mark_Click;
                CT_mark.Items.Add(MI);
            });
            AddNewCollectionMI();
        }

        private void AddNewCollectionMI() {
            var MI_create = new MenuItem { Header = "Create new collection" };
            MI_create.Click += create_collection;
            CT_mark.Items.Add(new Separator());
            CT_mark.Items.Add(MI_create);
        }

        private void UpdateCollectionsUI() {
            var expandedItems = new List<object>();
            SaveExpandedItems(TV_clips_collections.Items, expandedItems);

            TV_clips_collections.Items.Refresh();

            RestoreExpandedItems(TV_clips_collections.Items, expandedItems);
        }

        private void SaveExpandedItems(ItemCollection items, List<object> expandedItems) {
            foreach (var item in items) {
                var treeViewItem = TV_clips_collections.ItemContainerGenerator.ContainerFromItem(item) as TreeViewItem;
                if (treeViewItem != null && treeViewItem.IsExpanded) {
                    expandedItems.Add(item);
                    SaveExpandedItems(treeViewItem.Items, expandedItems);
                }
            }
        }

        private void RestoreExpandedItems(ItemCollection items, List<object> expandedItems) {
            foreach (var item in items) {
                if (expandedItems.Contains(item)) {
                    var treeViewItem = TV_clips_collections.ItemContainerGenerator.ContainerFromItem(item) as TreeViewItem;
                    if (treeViewItem != null) {
                        treeViewItem.IsExpanded = true;
                        RestoreExpandedItems(treeViewItem.Items, expandedItems);
                    }
                }
            }
        }

        private void UpdateColors() {
            Items = itemProvider.GetItemsFromFolder("H:\\nrtesting", collections: settings.collections);
            TV_clips.ItemsSource = null;
            TV_clips.ItemsSource = Items;
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
            if (ME_main.NaturalDuration.HasTimeSpan) {
                SL_duration.Maximum = ME_main.NaturalDuration.TimeSpan.TotalSeconds;
                timer.Start();
            }
        }

        private void Btn_Stop_Click(object sender, RoutedEventArgs e) {
            ME_main.Stop();
        }
        #endregion

        private void CB_ParsedFileName_Checked(object sender, RoutedEventArgs e) {
            //TV_clips.ItemsSource = itemProvider.GetItemsFromFolder("H:\\nrtesting");
            //TV_clips_collections.ItemsSource = itemProvider.GetItemsFromCollections(settings.collections);
            throw new NotImplementedException("NE");
        }
        private void CB_ParsedFileName_Unchecked(object sender, RoutedEventArgs e) {
            //TV_clips.ItemsSource = itemProvider.GetItemsFromFolder("H:\\nrtesting");
            //TV_clips_collections.ItemsSource = itemProvider.GetItemsFromCollections(settings.collections);
            throw new NotImplementedException("NE");
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
            //if (this.IsLoaded) {
            //    DataContext = itemProvider.GetItemsFromFolder("H:\\nrtesting", (bool)CB_ParsedFileName.IsChecked, (Sorts)(CB_sortType.SelectedItem as ComboBoxItem).Tag);
            //    TV_clips.Items.Refresh();
            //}
        }
        #endregion

        #region Marking clips
        private void Btn_Mark_Click(object sender, RoutedEventArgs e) {
            settings.SettingsFile.WriteSettings(); //PLACEHOLDER
        }

        private void MI_CT_mark_Click(object sender, RoutedEventArgs e) {
            var clickeditem = sender as MenuItem;
            var sourceElement = (clickeditem.Parent as ContextMenu)?.Tag as FrameworkElement;
            //construct new collectionfile and write it to this file
            ME_main.Volume = 0; //TODO: remove this, this is only for not get hear loss while debugging

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
            Window window = new CollectionCreatorWindow(null);
            if (window.ShowDialog() == true) {
                Collection collection = (window as CollectionCreatorWindow).Collection;
                settings.collections.Add(collection);
                UpdateCollectionsMI();
                TV_clips_collections.Items.Refresh();
                UpdateColors();
            }
        }
        #endregion

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
            if (!settings.SettingsFile.CheckIfChanged()) {
                e.Cancel = true;
            }
            else {
                e.Cancel = false;
            }
        }
    }
}
