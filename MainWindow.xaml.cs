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
using System.Windows.Controls.Primitives;
using System.IO;
using System.Security.Principal;
using Gma.System.MouseKeyHook;
using ClipsOrganizer.Profiles;
using ClipsOrganizer.FileUtils;
using ClipsOrganizer.Properties;
using System.Diagnostics.Eventing.Reader;
using Newtonsoft.Json;

namespace ClipsOrganizer {
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        DispatcherTimer SliderTimer, AutoSaveTimer;
        ItemProvider itemProvider = null;
        ContextMenu CT_mark = null;
        List<Item> Items = null;
        Profile CurrentProfile = null;

        private string SettingsPath = "./settings.json";
        private string ProfilePath = "./Profiles/";


        private TreeView _lastSelectedTreeView;
        private object _lastSelectedItem;
        private Collection _lastSelectedCollection;
        public MainWindow() {
            //// Удаление файла settings.json, если он существует
            //if (File.Exists("./settings.json")) {
            //    File.Delete("./settings.json");
            //}

            //// Удаление папки Profiles, если она существует
            //if (Directory.Exists("./Profiles")) {
            //    Directory.Delete("./Profiles", true); // true удаляет папку с содержимым
            //}
            string clipsPath;
            string ffmpegPath;
            string Profilename;
            if (!Directory.Exists("./Profiles")) {
                WelcomeWindow window = new WelcomeWindow();
                if (window.ShowDialog() == true) {
                    clipsPath = window.ClipsPath;
                    ffmpegPath = window.ffmpegPath;
                    Profilename = window.ProfileName;
                    Directory.CreateDirectory("./Profiles");
                    CurrentProfile = new Profile() { ClipsFolder = clipsPath, ProfileName = Profilename };
                    FileSerializer.WriteAndCreateBackupFile(CurrentProfile, CurrentProfile.ProfilePath);
                    GlobalSettings.Initialize(ffmpegPath);
                    GlobalSettings.Instance.LastSelectedProfile = CurrentProfile.ProfileName;
                    FileSerializer.WriteAndCreateBackupFile(GlobalSettings.Instance, SettingsPath);
                }
            }
            GlobalSettings.Instance = FileSerializer.ReadFile<GlobalSettings>(SettingsPath);
            if (File.Exists($"./Profiles/{GlobalSettings.Instance.LastSelectedProfile}.json")) {
                CurrentProfile = FileSerializer.ReadFile<Profile>($"./Profiles/{GlobalSettings.Instance.LastSelectedProfile}.json");
            }
            else {
                CurrentProfile = FileSerializer.ReadFile<Profile>($"./Profiles/{ProfileManager.LoadAllProfiles().First()}.json");
            }
            itemProvider = new ItemProvider();

            GlobalSettings.Instance.ffmpegInit(); //maybe change it to init when cut was being made

            Items = itemProvider.GetItemsFromFolder(CurrentProfile.ClipsFolder, collections: CurrentProfile.Collections);

            InitializeComponent();

            Log.TB_log = TB_log;

            LoadGlobalKeyboardHook();

            #region Context Menu init
            CT_mark = new ContextMenu() { Name = "CT_mark" };
            if (CurrentProfile.Collections.Count == 0) {
                CT_mark.Items.Add("Нет коллекций");
                AddNewCollectionMI();
            }
            else {
                UpdateCollectionsMI();
            }
            CT_mark.Opened += CT_mark_Opened;

            CB_Profile.ItemsSource = ProfileManager.LoadAllProfiles();
            CB_Profile.SelectedItem = CurrentProfile.ProfileName;

            Btn_Mark.ContextMenu = CT_mark;
            TV_clips.ContextMenu = CT_mark;
            TV_clips_collections.ContextMenu = CT_mark;
            //Костыль?
            //ДА КОСТЫЛЬ ПОТОМ ПЕРЕПРОВЕРИТЬ И УБРАТЬ, В ТЕГЕ ОСТАЁТСЯ ТОЛЬКО ПОСЛЕДНЕЕ ПРИСВОЕННОЕ ЗНАЧЕНИЕ
            Btn_Mark.ContextMenu.Tag = Btn_Mark;
            TV_clips_collections.ContextMenu.Tag = TV_clips_collections;
            TV_clips.ContextMenu.Tag = TV_clips;
            #endregion

            TV_clips_collections.ItemsSource = CurrentProfile.Collections;

            TV_clips.ItemsSource = Items;

            #region Slider timer init
            SliderTimer = new DispatcherTimer();
            SliderTimer.Interval = TimeSpan.FromMilliseconds(100);
            SliderTimer.Tick += VideoDurationUpdate;
            #endregion 
            #region AutoSave timer init
            AutoSaveTimer = new DispatcherTimer();
            AutoSaveTimer.Interval = TimeSpan.FromMinutes(3);
            AutoSaveTimer.Tick += AutoSave;
            AutoSaveTimer.Start();
            #endregion
        }

        private void AutoSave(object sender, EventArgs e) {
            Log.Update($"Автосохранение профиля {CurrentProfile.ProfileName} выполнено в {DateTime.Now}");
            FileSerializer.WriteAndCreateBackupFile(CurrentProfile, CurrentProfile.ProfilePath);
        }

        private void LoadGlobalKeyboardHook() {
            Hook.GlobalEvents().Dispose();
            var CombinationDict = new Dictionary<Combination, Action>();
            foreach (var item in CurrentProfile.Collections) {
                if (item.KeyBinding != null) {
                    Action action = () =>
                    {
                        FileInfo fileInfo = ItemProvider.GetLastFile(CurrentProfile.ClipsFolder, Items);
                        Item newitem = new Item() { Name = fileInfo.Name, Path = fileInfo.FullName, Date = fileInfo.CreationTime };
                        item.SafeAddClip(newitem);
                        UpdateCollectionsUI(TV_clips_collections);
                    };
                    try {
                        CombinationDict.Add(Combination.FromString(item.KeyBinding.Replace("Ctrl", "Control")), action);
                    }
                    catch (Exception ex) {
                        MessageBox.Show($"Ошибка загрузки горячей клавиши для коллекции {item.CollectionTag}");
                    }
                }
            }
            Hook.GlobalEvents().OnCombination(CombinationDict);
        }

        private void CT_mark_Opened(object sender, RoutedEventArgs e) {
            if (sender is ContextMenu contextMenu) {
                if (contextMenu.PlacementTarget is FrameworkElement placementTarget) {
                    if (placementTarget == TV_clips) {
                        if (TV_clips.SelectedItem is DirectoryItem) {
                            CT_mark.IsOpen = false;
                            e.Handled = false;
                            return;
                        }
                        UpdateCollectionsMI();
                        return;
                    }
                    if (placementTarget == TV_clips_collections) {
                        if (!(TV_clips_collections.SelectedItem is null)) {
                            if (TV_clips_collections.SelectedItem.GetType() == typeof(Item)) {
                                UpdateCollectionsMI();
                                var sel_item = TV_clips_collections.SelectedItem as Item;
                                var MI = new MenuItem { Header = "Удалить" };
                                MI.Tag = sel_item;
                                MI.Click += MI_CT_remove_Click;
                                CT_mark.Items.Add(MI);
                            }
                            if (TV_clips_collections.SelectedItem.GetType() == typeof(Collection)) {
                                //UpdateCollectionsMI();
                                CT_mark.Items.Clear();
                                var sel_item = TV_clips_collections.SelectedItem as Collection;
                                var MI = new MenuItem { Header = "Удалить коллекцию" };
                                MI.Tag = sel_item;
                                MI.Click += MI_CT_DeleteCollection_Click;
                                CT_mark.Items.Add(MI);
                                MI = new MenuItem { Header = "Изменить" };
                                MI.Tag = sel_item;
                                MI.Click += MI_CT_edit_Click;
                                CT_mark.Items.Add(MI);
                            }
                        }
                    }
                }
            }
        }

        private void MI_CT_DeleteCollection_Click(object sender, RoutedEventArgs e) {
            var collection = (sender as MenuItem).Tag as Collection;
            CurrentProfile.Collections.Remove(collection);
            UpdateCollectionsMI();
            UpdateCollectionsUI(TV_clips_collections);
        }

        private void MI_CT_edit_Click(object sender, RoutedEventArgs e) {
            var ItemToEdit = (sender as MenuItem).Tag as Collection;
            var window = new CollectionCreatorWindow(ItemToEdit);
            window.ShowDialog();
            if (window.DialogResult.HasValue) {
                LoadGlobalKeyboardHook();
                UpdateColors();
                UpdateCollectionsUI(TV_clips_collections);
            }
        }

        private void MI_CT_remove_Click(object sender, RoutedEventArgs e) {
            var ItemToDelete = (sender as MenuItem).Tag as Item;
            Item item = new Item();
            List<Collection> foundCollections = new List<Collection>();
            foreach (var collection in CurrentProfile.Collections) {
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
                        CurrentProfile.Collections.Find(p => p.CollectionTag == collection.CollectionTag).Files.RemoveAll(i => i.Name == ItemToDelete.Name);
                    }
                    UpdateCollectionsUI(TV_clips_collections);
                }
            }
            else {
                foreach (var collection in CurrentProfile.Collections) {
                    collection.Files.RemoveAll(p => p.Name == ItemToDelete.Name);
                }
            }
            UpdateCollectionsUI(TV_clips_collections);
        }

        private void UpdateCollectionsMI() {
            CT_mark.Items.Clear();
            LoadGlobalKeyboardHook();
            CurrentProfile.Collections.ForEach(x =>
            {
                var MI = new MenuItem { Header = x.CollectionTag };
                MI.Tag = x;
                MI.Click += MI_CT_mark_Click;
                CT_mark.Items.Add(MI);
            });

            AddNewCollectionMI();
        }

        private void AddNewCollectionMI() {
            var MI_create = new MenuItem { Header = "Создать новую коллекцию" };
            MI_create.Click += create_collection;
            CT_mark.Items.Add(new Separator());
            CT_mark.Items.Add(MI_create);
        }

        private void UpdateCollectionsUI(TreeView treeView) {
            var expandedItems = new List<object>();
            SaveExpandedItems(treeView.Items, expandedItems, treeView);

            treeView.Items.Refresh();

            RestoreExpandedItems(treeView.Items, expandedItems, treeView);
        }

        private void SaveExpandedItems(ItemCollection items, List<object> expandedItems, TreeView treeView) {
            foreach (var item in items) {
                var treeViewItem = treeView.ItemContainerGenerator.ContainerFromItem(item) as TreeViewItem;
                if (treeViewItem != null && treeViewItem.IsExpanded) {
                    expandedItems.Add(item);
                    SaveExpandedItems(treeViewItem.Items, expandedItems, treeView);
                }
            }
        }

        private void RestoreExpandedItems(ItemCollection items, List<object> expandedItems, TreeView treeView) {
            foreach (var item in items) {
                if (expandedItems.Contains(item)) {
                    var treeViewItem = treeView.ItemContainerGenerator.ContainerFromItem(item) as TreeViewItem;
                    if (treeViewItem != null) {
                        treeViewItem.IsExpanded = true;
                        RestoreExpandedItems(treeViewItem.Items, expandedItems, treeView);
                    }
                }
            }
        }

        //TODO: REFACTOR COLOR UPDATING LATER
        private void RefreshTreeViewWithColors(TreeView treeView, string basePath, List<Collection> collections) {
            var expandedItems = new List<object>();
            SaveExpandedItems(treeView.Items, expandedItems, treeView);

            var items = itemProvider.GetItemsFromFolder(basePath, collections: collections);
            treeView.ItemsSource = items;

            RestoreExpandedItems(treeView.Items, expandedItems, treeView);
        }
        private void UpdateColors() {
            RefreshTreeViewWithColors(TV_clips, CurrentProfile.ClipsFolder, CurrentProfile.Collections);
        }

        #region sliders events
        private void SL_duration_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e) {
            is_dragging = false;
            ME_main.Position = TimeSpan.FromSeconds((double)SL_duration.Value);
        }

        private void MainGrid_SizeChanged(object sender, SizeChangedEventArgs e) {
            //-493
            if (MainGrid.ActualWidth - 700 + 430 > 0) {
                SL_duration.Width = MainGrid.ActualWidth - 700 + 430;
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
            if (SL_duration.IsSelectionRangeEnabled && OwnedWindows.Count == 0)
                SL_duration.SelectionEnd = ME_main.Position.TotalSeconds;
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
                SliderTimer.Start();
            }
        }

        private void Btn_Stop_Click(object sender, RoutedEventArgs e) {
            ME_main.Pause();
        }
        #endregion

        private void CB_ParsedFileName_Checked(object sender, RoutedEventArgs e) {
            //TV_clips.ItemsSource = itemProvider.GetItemsFromFolder("H:\\nrtesting");
            //TV_clips_collections.ItemsSource = itemProvider.GetItemsFromCollections(CurrentProfile.Collections);
            throw new NotImplementedException("NE");
        }
        private void CB_ParsedFileName_Unchecked(object sender, RoutedEventArgs e) {
            //TV_clips.ItemsSource = itemProvider.GetItemsFromFolder("H:\\nrtesting");
            //TV_clips_collections.ItemsSource = itemProvider.GetItemsFromCollections(CurrentProfile.Collections);
            throw new NotImplementedException("NE");
        }

        #region Clip selection
        private void TV_clips_MouseDoubleClick(object sender, MouseButtonEventArgs e) {
            if (e.RightButton != MouseButtonState.Pressed)
                LoadNewVideoClip();
        }

        private void LoadNewVideoClip(Uri VideoPath = null) {
            RemoveSelection();
            if (VideoPath != null) {
                ME_main.Source = VideoPath;
                ME_main.Play();
                return;
            }
            if (_lastSelectedTreeView?.SelectedItem == null) return;
            if (_lastSelectedTreeView?.SelectedItem.GetType() == typeof(Item)) {
                ME_main.Source = new Uri((_lastSelectedTreeView?.SelectedItem as Item).Path);
            }
            if (_lastSelectedTreeView?.SelectedItem.GetType() == typeof(FileItem)) {
                ME_main.Source = new Uri((_lastSelectedTreeView?.SelectedItem as FileItem).Path);
            }
            //TODO USE OR DELETE LATER
            if (_lastSelectedTreeView?.SelectedItem.GetType() == typeof(DirectoryItem)) { }
            if (_lastSelectedTreeView?.SelectedItem.GetType() == typeof(Collection)) { }
            ME_main.Play();
            if (ME_main.Source?.LocalPath != null)
                this.Title = string.Format("ClipsOrganizer / {0}", System.IO.Path.GetFileName(ME_main.Source.LocalPath));
            TB_loading_info.Visibility = Visibility.Hidden;
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
            if ((clickeditem.Parent as ContextMenu).PlacementTarget is TreeView sourceElement) {
                //((clickeditem.Parent as ContextMenu)).Name

                ME_main.Volume = 0; //TODO: remove this, this is only for not get hear loss while debugging
                //TODO: redolater
                //if (sourceElement == Btn_Mark) {
                //    (clickeditem.Tag as Collection).Files.Add(new Item
                //    {
                //        Date = new FileInfo(ME_main.Source.LocalPath).CreationTime,
                //        Path = ME_main.Source.LocalPath,
                //        Color = itemProvider.FindColorByCollections(clickeditem.Tag as Collection, ME_main.Source.AbsolutePath.Split('/').Last()),
                //        Name = ME_main.Source.AbsolutePath.Split('/').Last()
                //    });
                //}
                if (sourceElement == TV_clips) {
                    if (TV_clips.SelectedItem == null) return;
                    FileItem SelectedItem = TV_clips.SelectedItem as FileItem;
                    (clickeditem.Tag as Collection).SafeAddClip(new Item
                    {
                        Path = SelectedItem.Path,
                        Date = SelectedItem.Date,
                        Name = SelectedItem.Name,
                        Color = (clickeditem.Tag as Collection).Color,
                    });
                }
            }
            _lastSelectedCollection = clickeditem.Tag as Collection;
            UpdateCollectionsUI(TV_clips_collections);
            UpdateColors();
        }
        private void create_collection(object sender, RoutedEventArgs e) {
            Window window = new CollectionCreatorWindow(null);
            if (window.ShowDialog() == true) {
                Collection collection = (window as CollectionCreatorWindow).Collection;
                if (CurrentProfile.Collections.Find(p => p.CollectionTag == collection.CollectionTag) == null) {
                    CurrentProfile.Collections.Add(collection);
                    UpdateCollectionsMI();
                    UpdateColors();
                    TV_clips_collections.Items.Refresh();
                }
                else {
                    MessageBox.Show("Коллекция с таким названием уже существует");
                    return;
                }
            }
        }
        #endregion
        //Если честно то скорее всего сохранение изменений можно так и оставить 
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
            //bool settingsChanged = settings.SettingsFile.CheckIfChanged();
            //if (settingsChanged) {
            //    var result = MessageBox.Show("В коллекциях были изменены/добавлены файлы, хотите сохранить их?", "Подтверждение", MessageBoxButton.YesNoCancel);
            //    if (result == MessageBoxResult.Yes) {
            Log.Update("Закрытие окна");
            GlobalSettings.Instance.LastSelectedProfile = CurrentProfile.ProfileName;
            FileSerializer.WriteAndCreateBackupFile(GlobalSettings.Instance, SettingsPath);
            FileSerializer.WriteAndCreateBackupFile(CurrentProfile, CurrentProfile.ProfilePath);
            //        e.Cancel = false;
            //    }
            //    if (result == MessageBoxResult.No) {
            //        e.Cancel = false;
            //    }
            //    if (result == MessageBoxResult.Cancel) {
            //        e.Cancel = true;
            //    }
            //}
            //else {
            //    e.Cancel = false;
            //}
        }
        private void Btn_export_Click(object sender, RoutedEventArgs e) {
            Window window = new ExportWindow(CurrentProfile);
            window.ShowDialog();
            //Profile ProfileAfterMoving = (window as ExportWindow).profile;
            //bool result = (window as ExportWindow).bresult;
            //if (result == true) {
            //    settings.UpdateSettings();
            //    FileSerializer.WriteAndCreateBackupFile(settings, SettingsPath);
            //}
            //else if (result == false) { }
        }
        TimeSpan StartTime = TimeSpan.Zero;
        public event Action<TimeSpan, TimeSpan?> SliderSelectionChanged;

        private void RemoveSelection() {
            StartTime = TimeSpan.Zero;
            SL_duration.SelectionStart = 0;
            SL_duration.SelectionEnd = 0;
            SL_duration.IsSelectionRangeEnabled = false;
        }

        private void Window_KeyDown(object sender, KeyEventArgs e) {
            if (e.Key == Key.Enter) {
                LoadNewVideoClip();
            }
            if (e.Key == Key.M) {
                if (_lastSelectedItem is Item && _lastSelectedItem.GetType() != typeof(DirectoryItem) && _lastSelectedCollection != null) {
                    _lastSelectedCollection.SafeAddClip(_lastSelectedItem as Item);
                    UpdateCollectionsUI(TV_clips_collections);
                    UpdateColors();
                }
            }
            if (e.Key == Key.S && Keyboard.Modifiers.HasFlag(ModifierKeys.Control)) {
                FileSerializer.WriteAndCreateBackupFile(GlobalSettings.Instance, SettingsPath);
                Log.Update("Настройки сохранены");
            }
            //почему то не работает
            //if (e.Key == Key.Left) {
            //    ME_main.Position.Subtract(TimeSpan.FromSeconds(10));
            //}

            #region Clips encoding binds
            if (e.Key == Key.S) {
                RemoveSelection();
            }
            if (e.Key == Key.C) {
                StartTime = ME_main.Position;
                Log.Update(string.Format("Обрезка с {0}", StartTime.TotalMilliseconds));
                SL_duration.IsSelectionRangeEnabled = true;
                SL_duration.SelectionStart = ME_main.Position.TotalSeconds;
                SliderSelectionChanged?.Invoke(StartTime, null);
            }
            if (e.Key == Key.C && Keyboard.Modifiers.HasFlag(ModifierKeys.Shift)) {
                Log.Update(string.Format("Обрезка с {0} до конца", StartTime.TotalMilliseconds));
                if (OwnedWindows.Count == 0) {
                    OpenRendererWindow(ME_main.Position, ME_main.NaturalDuration.TimeSpan);
                    SL_duration.SelectionEnd = ME_main.NaturalDuration.TimeSpan.TotalSeconds;
                    UpdateColors();
                }

            }
            if (e.Key == Key.E) {
                Log.Update(string.Format("Обрезка до {0}", ME_main.Position.TotalMilliseconds));
                SL_duration.IsSelectionRangeEnabled = true;
                if (StartTime == TimeSpan.Zero) SL_duration.SelectionStart = 0;
                SL_duration.SelectionEnd = ME_main.Position.TotalSeconds;
                if (OwnedWindows.Count == 0) {
                    OpenRendererWindow(StartTime, ME_main.Position);
                    UpdateColors();
                }
                else {
                    SliderSelectionChanged?.Invoke(StartTime, ME_main.Position);
                }

            }
            if (e.Key == Key.D && OwnedWindows.Count != 0) {
                OpenRendererWindow(null, ME_main.NaturalDuration.TimeSpan);
                UpdateColors();
            }

            void OpenRendererWindow(TimeSpan? StartTime, TimeSpan? EndTime) {
                RendererWindow rendererwindow = new RendererWindow(ME_main.Source, StartTime, EndTime) { Owner = this };
                ME_main.Pause();
                SliderSelectionChanged += rendererwindow.RendererWindow_SliderSelectionChanged;
                rendererwindow.Show();
            }
            #endregion
        }
        private void TV_UpdateLastUsed(object sender, RoutedPropertyChangedEventArgs<object> e) {
            _lastSelectedTreeView = sender as TreeView;
            _lastSelectedItem = e.NewValue;
        }

        private void Btn_settings_Click(object sender, RoutedEventArgs e) {
            Window window = new SettingsWindow(GlobalSettings.Instance, CurrentProfile);
            window.ShowDialog();
            if (!ProfileManager.LoadAllProfiles().Exists(p => p == CurrentProfile.ProfileName)) {
                CurrentProfile = FileSerializer.ReadFile<Profile>($"./Profiles/{ProfileManager.LoadAllProfiles().First()}.json");
                FileSerializer.WriteAndCreateBackupFile(CurrentProfile, CurrentProfile.ProfilePath);
                Items = itemProvider.GetItemsFromFolder(CurrentProfile.ClipsFolder, collections: CurrentProfile.Collections);
                TV_clips_collections.ItemsSource = CurrentProfile.Collections;
                TV_clips.ItemsSource = Items;
            }
            CB_Profile.SelectedItem = CurrentProfile.ProfileName;
            CB_Profile.ItemsSource = ProfileManager.LoadAllProfiles();
        }

        private void Window_DragEnter(object sender, DragEventArgs e) {
            e.Effects = new FileInfo((e.Data.GetData(DataFormats.FileDrop) as string[]).First()).Extension == ".mp4" ? DragDropEffects.Link : DragDropEffects.None;
        }

        private void Window_Drop(object sender, DragEventArgs e) {
            LoadNewVideoClip(new Uri((e.Data.GetData(DataFormats.FileDrop) as string[]).First()));
        }

        private void CB_Profile_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (!this.IsLoaded) return;
            string SelectedProfile = CB_Profile.SelectedItem as string;
            if (!File.Exists(ProfileManager.ProfilePath + $"/{SelectedProfile}.json")) Log.Update($"Не удалось найти {SelectedProfile} в папке с профилями {ProfileManager.ProfilePath}");
            else {
                FileSerializer.WriteAndCreateBackupFile(CurrentProfile, CurrentProfile.ProfilePath);
                CurrentProfile = FileSerializer.ReadFile<Profile>(ProfileManager.ProfilePath + $"/{SelectedProfile}.json");
                Items = itemProvider.GetItemsFromFolder(CurrentProfile.ClipsFolder, collections: CurrentProfile.Collections);
                TV_clips_collections.ItemsSource = CurrentProfile.Collections;
                TV_clips.ItemsSource = Items;
            }
        }

        private void TB_log_MouseDown(object sender, MouseButtonEventArgs e) {
            Window window = new LogWindow();
            window.Show();
        }
    }
}
