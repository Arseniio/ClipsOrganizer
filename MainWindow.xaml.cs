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
        string ffmpegPath;

        private Log log = new Log();

        private TreeView _lastSelectedTreeView;
        private object _lastSelectedItem;
        private Collection _lastSelectedCollection;
        public MainWindow() {
            if (!File.Exists("./settings.json")) {
                Window window = new WelcomeWindow();
                if (window.ShowDialog() == true) {
                    clipsPath = (window as WelcomeWindow).ClipsPath;
                    ffmpegPath = (window as WelcomeWindow).ffmpegPath;
                }
            }
            itemProvider = new ItemProvider();
            settings = new Settings.Settings(clipsPath,ffmpegPath);

            settings.SettingsFile.LoadSettings();
            settings.ffmpegInit(); //maybe change it to init when cut was being made

            Items = itemProvider.GetItemsFromFolder(settings.ClipsFolder, collections: settings.collections);

            InitializeComponent();
            Log.TB_log = TB_log;
            CT_mark = new ContextMenu() { Name = "CT_mark" };
            if (settings.collections.Count == 0) {
                CT_mark.Items.Add("Нет коллекций");
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
                            UpdateCollectionsMI();
                            var sel_item = TV_clips_collections.SelectedItem as Item;
                            var MI = new MenuItem { Header = "Remove" };
                            MI.Tag = sel_item;
                            MI.Click += MI_CT_remove_Click;
                            CT_mark.Items.Add(MI);
                        }
                        if (TV_clips_collections.SelectedItem.GetType() == typeof(Collection)) {
                            UpdateCollectionsMI();
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
                UpdateCollectionsUI(TV_clips_collections);
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
            if (foundCollections.Count >= 2) {
                Window window = new CollectionDeletionWindow(foundCollections);
                bool? dialogResult = window.ShowDialog();
                if (dialogResult == true && (window as CollectionDeletionWindow).selectedCollections.Count > 0) {
                    foreach (var collection in (window as CollectionDeletionWindow).selectedCollections) {
                        item = null;
                        settings.collections.Find(p => p.CollectionTag == collection.CollectionTag).Files.RemoveAll(i => i.Name == ItemToDelete.Name);
                    }
                    UpdateCollectionsUI(TV_clips_collections);
                }
            }
            else {
                foreach (var collection in settings.collections) {
                    collection.Files.RemoveAll(p => p.Name == ItemToDelete.Name);
                }
            }
            UpdateCollectionsUI(TV_clips_collections);
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

        private void UpdateCollectionsUI(TreeView treeView) {
            var expandedItems = new List<object>();
            SaveExpandedItems(treeView.Items, expandedItems);

            treeView.Items.Refresh();

            RestoreExpandedItems(treeView.Items, expandedItems);
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
            Items = itemProvider.GetItemsFromFolder(settings.ClipsFolder, collections: settings.collections);
            //maybe wrong implementation
            TV_clips.ItemsSource = null;
            TV_clips.ItemsSource = Items;
            UpdateCollectionsUI(TV_clips);
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
            //TODO: since all TV uses FileItem so this code is redundand, need to delete it and test... but i'll do it later someday
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
            MessageBox.Show("Пока не работает :)");
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
            _lastSelectedCollection = clickeditem.Tag as Collection;
            UpdateCollectionsUI(TV_clips_collections);
            UpdateColors();
        }
        private void create_collection(object sender, RoutedEventArgs e) {
            Window window = new CollectionCreatorWindow(null);
            if (window.ShowDialog() == true) {
                Collection collection = (window as CollectionCreatorWindow).Collection;
                settings.collections.Add(collection);
                UpdateCollectionsMI();
                UpdateColors();
                TV_clips_collections.Items.Refresh();
            }
        }
        #endregion

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
            bool settingsChanged = settings.SettingsFile.CheckIfChanged();
            if (settingsChanged) {
                var result = MessageBox.Show("В коллекциях были изменены/добавлены файлы, хотите сохранить их?", "Подтверждение", MessageBoxButton.YesNoCancel);
                if (result == MessageBoxResult.Yes) {
                    this.settings.SettingsFile.WriteAndCreateBackupSettings();
                    e.Cancel = false;
                }
                if (result == MessageBoxResult.No) {
                    e.Cancel = false;
                }
                if (result == MessageBoxResult.Cancel) {
                    e.Cancel = true;
                }
            }
            else {
                e.Cancel = false;
            }
        }

        private void Btn_export_Click(object sender, RoutedEventArgs e) {
            Window window = new ExportWindow(this.settings);
            window.ShowDialog();
            Settings.Settings settingsAfterMoving = (window as ExportWindow).Settings;
            bool result = (window as ExportWindow).bresult;
            if (result == true) {
                settings.UpdateSettings(settingsAfterMoving);
                settings.SettingsFile.WriteAndCreateBackupSettings();
            }
            else if (result == false) { }
        }
        TimeSpan StartTime = TimeSpan.Zero;
        private void Window_KeyDown(object sender, KeyEventArgs e) {
            if (e.Key == Key.Enter) {
                if (_lastSelectedItem is Item) {
                    ME_main.Source = new Uri((_lastSelectedItem as Item).Path);
                }
            }
            if (e.Key == Key.M) {
                if (_lastSelectedItem is Item) {
                    if (_lastSelectedCollection != null) {
                        _lastSelectedCollection.Files.Add(_lastSelectedItem as Item);
                    }
                }
            }
            if (e.Key == Key.C) {
                StartTime = ME_main.Position;
                log.Update(string.Format("Cut from {0}", StartTime.TotalMilliseconds));
            }
            if (e.Key == Key.E) {
                log.Update("Cut dropped");
                if (StartTime != TimeSpan.Zero && ME_main.Position > StartTime) {
                    //MessageBox.Show(string.Format("Clip Will be cutted from {0} to {1}; {2}",StartTime.TotalMilliseconds, ME_main.Position.TotalMilliseconds , ME_main.Position));
                    log.Update(string.Format("Cut to {0}", ME_main.Position.TotalMilliseconds));

                }
            }
        }
        private void TV_UpdateLastUsed(object sender, RoutedPropertyChangedEventArgs<object> e) {
            _lastSelectedTreeView = sender as TreeView;
            _lastSelectedItem = e.NewValue;
        }

        private void Btn_settings_Click(object sender, RoutedEventArgs e) {
            Window window = new SettingsWindow(this.settings);
            if (window.ShowDialog() == true) {
            }
        }
    }
}
