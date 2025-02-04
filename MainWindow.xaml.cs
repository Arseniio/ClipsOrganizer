using ClipsOrganizer.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using ClipsOrganizer.Settings;
using ClipsOrganizer.Collections;
using System.IO;
using Gma.System.MouseKeyHook;
using ClipsOrganizer.Profiles;
using ClipsOrganizer.FileUtils;
using ClipsOrganizer.ViewableControls;
using MetadataExtractor.Util;
using Windows.ApplicationModel.Appointments;

namespace ClipsOrganizer {
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        DispatcherTimer SliderTimer, AutoSaveTimer;
        ItemProvider itemProvider = null;
        ContextMenuController contextMenuController;
        List<Item> Items = null;
        public static Profile CurrentProfile = null;

        private string SettingsPath = "./settings.json";
        private string ProfilePath = "./Profiles/";

        private TreeView _lastSelectedTreeView;
        private object _lastSelectedItem;
        private Collection _lastSelectedCollection;

        public MainWindow() {
            InitializeProfile();
            InitializeItemProvider();
            InitializeComponent();
            InitializeContextMenu();
            InitializeAutoSaveTimer();
            LoadGlobalKeyboardHook();
        }

        private void InitializeProfile() {
            if (!Directory.Exists("./Profiles")) {
                CreateNewProfile();
            }
            LoadProfileSettings();
        }

        private void CreateNewProfile() {
            WelcomeWindow window = new WelcomeWindow();
            if (window.ShowDialog() == true) {
                string clipsPath = window.ClipsPath;
                string ffmpegPath = window.ffmpegPath;
                string profileName = window.ProfileName;
                Directory.CreateDirectory("./Profiles");
                CurrentProfile = new Profile() { ClipsFolder = clipsPath, ProfileName = profileName };
                FileSerializer.WriteAndCreateBackupFile(CurrentProfile, CurrentProfile.ProfilePath);
                GlobalSettings.Initialize(ffmpegPath);
                GlobalSettings.Instance.LastSelectedProfile = CurrentProfile.ProfileName;
                FileSerializer.WriteAndCreateBackupFile(GlobalSettings.Instance, SettingsPath);
            }
        }

        private void LoadProfileSettings() {
            GlobalSettings.Instance = FileSerializer.ReadFile<GlobalSettings>(SettingsPath);
            string profilePath = $"./Profiles/{GlobalSettings.Instance.LastSelectedProfile}.json";
            if (File.Exists(profilePath)) {
                CurrentProfile = FileSerializer.ReadFile<Profile>(profilePath);
            } else {
                CurrentProfile = FileSerializer.ReadFile<Profile>($"./Profiles/{ProfileManager.LoadAllProfiles().First()}.json");
            }
        }

        private void InitializeItemProvider() {
            itemProvider = new ItemProvider();
            GlobalSettings.Instance.FFmpegInit();
            Items = itemProvider.GetItemsFromFolder(CurrentProfile.ClipsFolder, collections: CurrentProfile.Collections);
        }

        private void InitializeContextMenu() {
            contextMenuController = new ContextMenuController(CurrentProfile, TV_clips, TV_clips_collections);
        }

        private void InitializeAutoSaveTimer() {
            AutoSaveTimer = new DispatcherTimer();
            AutoSaveTimer.Interval = TimeSpan.FromMinutes(3);
            AutoSaveTimer.Tick += AutoSave;
            AutoSaveTimer.Start();
        }

        private void AutoSave(object sender, EventArgs e) {
            Log.Update($"Автосохранение профиля {CurrentProfile.ProfileName} выполнено в {DateTime.Now}");
            FileSerializer.WriteAndCreateBackupFile(CurrentProfile, CurrentProfile.ProfilePath);
        }

        private void LoadGlobalKeyboardHook() {
            Hook.GlobalEvents().Dispose();
            var combinationDict = new Dictionary<Combination, Action>();
            foreach (var item in CurrentProfile.Collections) {
                if (item.KeyBinding != null) {
                    Action action = () => AddClipToCollection(item);
                    try {
                        combinationDict.Add(Combination.FromString(item.KeyBinding.Replace("Ctrl", "Control")), action);
                    } catch (Exception) {
                        MessageBox.Show($"Ошибка загрузки горячей клавиши для коллекции {item.CollectionTag}");
                    }
                }
            }
            Hook.GlobalEvents().OnCombination(combinationDict);
        }

        private void AddClipToCollection(Collection item) {
            FileInfo fileInfo = ItemProvider.GetLastFile(CurrentProfile.ClipsFolder, Items);
            Item newItem = new Item() { Name = fileInfo.Name, Path = fileInfo.FullName, Date = fileInfo.CreationTime };
            item.SafeAddClip(newItem);
            UpdateCollectionsUI(TV_clips_collections);
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

        private void RefreshTreeViewWithColors(TreeView treeView, string basePath, List<Collection> collections) {
            var expandedItems = new List<object>();
            SaveExpandedItems(treeView.Items, expandedItems, treeView);
            var items = itemProvider.GetItemsFromFolder(basePath, collections: collections);
            treeView.ItemsSource = items;
            RestoreExpandedItems(treeView.Items, expandedItems, treeView);
        }

        public void UpdateColors() {
            RefreshTreeViewWithColors(TV_clips, CurrentProfile.ClipsFolder, CurrentProfile.Collections);
        }

        private void TV_clips_MouseDoubleClick(object sender, MouseButtonEventArgs e) {
            if (e.RightButton != MouseButtonState.Pressed)
                LoadNewFile();
        }

        private void LoadNewFile(string videoPath = null) {
            if (videoPath != null) {
                ViewableController.LoadNewFile(videoPath);
            }
            if (_lastSelectedTreeView?.SelectedItem == null) return;
            if (_lastSelectedTreeView?.SelectedItem is Item item) {
                ViewableController.LoadNewFile(item.Path);
            }
            if (_lastSelectedTreeView?.SelectedItem is FileItem fileItem) {
                ViewableController.LoadNewFile(fileItem.Path);
            }
        }

        private void Btn_Mark_Click(object sender, RoutedEventArgs e) {
            MessageBox.Show("Пока не работает :)");
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
            Log.Update("Закрытие окна");
            GlobalSettings.Instance.LastSelectedProfile = CurrentProfile.ProfileName;
            FileSerializer.WriteAndCreateBackupFile(GlobalSettings.Instance, SettingsPath);
            FileSerializer.WriteAndCreateBackupFile(CurrentProfile, CurrentProfile.ProfilePath);
        }

        private void Btn_export_Click(object sender, RoutedEventArgs e) {
            Window window = new ExportWindow(CurrentProfile);
            window.Owner = this;
            window.Show();
            TV_clips.IsEnabled = false;
            TV_clips_collections.IsEnabled = false;
        }

        private void Window_KeyDown(object sender, KeyEventArgs e) {
            if (e.Key == Key.Enter) {
                LoadNewFile();
            } else if (e.Key == Key.M) {
                MarkLastSelectedItem();
            } else if (e.Key == Key.S && Keyboard.Modifiers.HasFlag(ModifierKeys.Control)) {
                SaveSettings();
            } else {
                ViewableController.PassKeyStroke(e);
            }
        }

        private void MarkLastSelectedItem() {
            if (_lastSelectedItem is Item && _lastSelectedItem.GetType() != typeof(DirectoryItem) && _lastSelectedCollection != null) {
                _lastSelectedCollection.SafeAddClip(_lastSelectedItem as Item);
                UpdateCollectionsUI(TV_clips_collections);
                UpdateColors();
            }
        }

        private void SaveSettings() {
            FileSerializer.WriteAndCreateBackupFile(GlobalSettings.Instance, SettingsPath);
            Log.Update("Настройки сохранены");
        }

        private void TV_UpdateLastUsed(object sender, RoutedPropertyChangedEventArgs<object> e) {
            _lastSelectedTreeView = sender as TreeView;
            _lastSelectedItem = e.NewValue;
        }

        private void Btn_settings_Click(object sender, RoutedEventArgs e) {
            Window window = new SettingsWindow(GlobalSettings.Instance, CurrentProfile);
            window.ShowDialog();
            UpdateProfileAfterSettingsChange();
        }

        private void UpdateProfileAfterSettingsChange() {
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
            var files = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (files == null || files.Length != 1) {
                e.Effects = DragDropEffects.None;
                return;
            }
            var fileType = ViewableController.FileTypeDetector.DetectFileType(files.First());
            e.Effects = fileType != SupportedFileTypes.Unknown ? DragDropEffects.Link : DragDropEffects.None;
        }

        private void Window_Drop(object sender, DragEventArgs e) {
            LoadNewFile((e.Data.GetData(DataFormats.FileDrop) as string[]).First());
        }

        private void CB_Profile_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (!this.IsLoaded) return;
            string selectedProfile = CB_Profile.SelectedItem as string;
            if (!File.Exists(ProfileManager.ProfilePath + $"/{selectedProfile}.json")) {
                Log.Update($"Не удалось найти {selectedProfile} в папке с профилями {ProfileManager.ProfilePath}");
            } else {
                FileSerializer.WriteAndCreateBackupFile(CurrentProfile, CurrentProfile.ProfilePath);
                CurrentProfile = FileSerializer.ReadFile<Profile>(ProfileManager.ProfilePath + $"/{selectedProfile}.json");
                Items = itemProvider.GetItemsFromFolder(CurrentProfile.ClipsFolder, collections: CurrentProfile.Collections);
                TV_clips_collections.ItemsSource = CurrentProfile.Collections;
                TV_clips.ItemsSource = Items;
            }
        }

        private void MB_OpenLog(object sender, RoutedEventArgs e) {
            Window window = new LogWindow();
            window.Show();
        }

        private void TB_log_MouseDown(object sender, MouseButtonEventArgs e) {
            Window window = new LogWindow();
            window.Show();
        }
    }
}
