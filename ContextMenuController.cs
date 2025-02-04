using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ClipsOrganizer {
    public class ContextMenuController {
        private ContextMenu CT_mark;
        private Profile CurrentProfile;
        private TreeView TV_clips;
        private TreeView TV_clips_collections;

        public ContextMenuController(Profile currentProfile, TreeView tvClips, TreeView tvClipsCollections) {
            CurrentProfile = currentProfile;
            TV_clips = tvClips;
            TV_clips_collections = tvClipsCollections;
            InitializeContextMenu();
        }

        private void InitializeContextMenu() {
            CT_mark = new ContextMenu() { Name = "CT_mark" };
            if (CurrentProfile.Collections.Count == 0) {
                CT_mark.Items.Add("Нет коллекций");
                AddNewCollectionMenuItem();
            } else {
                UpdateCollectionsMenuItems();
            }
            CT_mark.Opened += CT_mark_Opened;
            TV_clips.ContextMenu = CT_mark;
            TV_clips_collections.ContextMenu = CT_mark;
            TV_clips_collections.ItemsSource = CurrentProfile.Collections;
            TV_clips.ItemsSource = CurrentProfile.Items;
        }

        private void CT_mark_Opened(object sender, RoutedEventArgs e) {
            if (sender is ContextMenu contextMenu) {
                if (contextMenu.PlacementTarget is FrameworkElement placementTarget) {
                    HandleContextMenuOpening(placementTarget);
                }
            }
        }

        private void HandleContextMenuOpening(FrameworkElement placementTarget) {
            if (placementTarget == TV_clips) {
                HandleClipsContextMenu();
            } else if (placementTarget == TV_clips_collections) {
                HandleCollectionsContextMenu();
            }
        }

        private void HandleClipsContextMenu() {
            if (TV_clips.SelectedItem is DirectoryItem) {
                CT_mark.IsOpen = false;
                return;
            }
            UpdateCollectionsMenuItems();
        }

        private void HandleCollectionsContextMenu() {
            if (TV_clips_collections.SelectedItem is Item selectedItem) {
                UpdateCollectionsMenuItems();
                AddRemoveMenuItem(selectedItem);
            } else if (TV_clips_collections.SelectedItem is Collection selectedCollection) {
                CT_mark.Items.Clear();
                AddDeleteCollectionMenuItem(selectedCollection);
                AddEditCollectionMenuItem(selectedCollection);
            }
        }

        private void AddRemoveMenuItem(Item selectedItem) {
            var removeMenuItem = new MenuItem { Header = "Удалить" };
            removeMenuItem.Tag = selectedItem;
            removeMenuItem.Click += MI_CT_remove_Click;
            CT_mark.Items.Add(removeMenuItem);
        }

        private void AddDeleteCollectionMenuItem(Collection selectedCollection) {
            var deleteMenuItem = new MenuItem { Header = "Удалить коллекцию" };
            deleteMenuItem.Tag = selectedCollection;
            deleteMenuItem.Click += MI_CT_DeleteCollection_Click;
            CT_mark.Items.Add(deleteMenuItem);
        }

        private void AddEditCollectionMenuItem(Collection selectedCollection) {
            var editMenuItem = new MenuItem { Header = "Изменить" };
            editMenuItem.Tag = selectedCollection;
            editMenuItem.Click += MI_CT_edit_Click;
            CT_mark.Items.Add(editMenuItem);
        }

        private void MI_CT_DeleteCollection_Click(object sender, RoutedEventArgs e) {
            var collection = (sender as MenuItem).Tag as Collection;
            CurrentProfile.Collections.Remove(collection);
            UpdateCollectionsMenuItems();
            UpdateCollectionsUI(TV_clips_collections);
        }

        private void MI_CT_edit_Click(object sender, RoutedEventArgs e) {
            var itemToEdit = (sender as MenuItem).Tag as Collection;
            var window = new CollectionCreatorWindow(itemToEdit);
            window.ShowDialog();
            if (window.DialogResult.HasValue) {
                // Assuming LoadGlobalKeyboardHook and UpdateColors are methods in MainWindow
                // You might need to pass a reference to MainWindow or refactor these methods
                // LoadGlobalKeyboardHook();
                // UpdateColors();
                UpdateCollectionsUI(TV_clips_collections);
            }
        }

        private void MI_CT_remove_Click(object sender, RoutedEventArgs e) {
            var itemToDelete = (sender as MenuItem).Tag as Item;
            RemoveItemFromCollections(itemToDelete);
            UpdateCollectionsUI(TV_clips_collections);
        }

        private void RemoveItemFromCollections(Item itemToDelete) {
            List<Collection> foundCollections = FindCollectionsContainingItem(itemToDelete);
            if (foundCollections.Count >= 2) {
                HandleMultipleCollections(itemToDelete, foundCollections);
            } else {
                RemoveItemFromAllCollections(itemToDelete);
            }
        }

        private List<Collection> FindCollectionsContainingItem(Item itemToDelete) {
            List<Collection> foundCollections = new List<Collection>();
            foreach (var collection in CurrentProfile.Collections) {
                if (collection.Files.Exists(p => p.Name == itemToDelete.Name)) {
                    foundCollections.Add(collection);
                }
            }
            return foundCollections;
        }

        private void HandleMultipleCollections(Item itemToDelete, List<Collection> foundCollections) {
            Window window = new CollectionDeletionWindow(foundCollections);
            bool? dialogResult = window.ShowDialog();
            if (dialogResult == true && (window as CollectionDeletionWindow).selectedCollections.Count > 0) {
                foreach (var collection in (window as CollectionDeletionWindow).selectedCollections) {
                    collection.Files.RemoveAll(i => i.Name == itemToDelete.Name);
                }
            }
        }

        private void RemoveItemFromAllCollections(Item itemToDelete) {
            foreach (var collection in CurrentProfile.Collections) {
                collection.Files.RemoveAll(p => p.Name == itemToDelete.Name);
            }
        }

        private void UpdateCollectionsMenuItems() {
            CT_mark.Items.Clear();
            // Assuming LoadGlobalKeyboardHook is a method in MainWindow
            // You might need to pass a reference to MainWindow or refactor this method
            // LoadGlobalKeyboardHook();
            foreach (var collection in CurrentProfile.Collections) {
                var menuItem = new MenuItem { Header = collection.CollectionTag };
                menuItem.Tag = collection;
                menuItem.Click += MI_CT_mark_Click;
                CT_mark.Items.Add(menuItem);
            }
            AddNewCollectionMenuItem();
        }

        private void AddNewCollectionMenuItem() {
            var createMenuItem = new MenuItem { Header = "Создать новую коллекцию" };
            createMenuItem.Click += create_collection;
            CT_mark.Items.Add(new Separator());
            CT_mark.Items.Add(createMenuItem);
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

        private void MI_CT_mark_Click(object sender, RoutedEventArgs e) {
            var clickedItem = sender as MenuItem;
            if ((clickedItem.Parent as ContextMenu).PlacementTarget is TreeView sourceElement) {
                if (sourceElement == TV_clips) {
                    MarkClip(clickedItem);
                }
            }
            // Assuming _lastSelectedCollection is a field in MainWindow
            // You might need to pass a reference to MainWindow or refactor this field
            // _lastSelectedCollection = clickedItem.Tag as Collection;
            UpdateCollectionsUI(TV_clips_collections);
            // Assuming UpdateColors is a method in MainWindow
            // You might need to pass a reference to MainWindow or refactor this method
            // UpdateColors();
        }

        private void MarkClip(MenuItem clickedItem) {
            if (TV_clips.SelectedItem == null) return;
            FileItem selectedItem = TV_clips.SelectedItem as FileItem;
            (clickedItem.Tag as Collection).SafeAddClip(new Item {
                Path = selectedItem.Path,
                Date = selectedItem.Date,
                Name = selectedItem.Name,
                Color = (clickedItem.Tag as Collection).Color,
            });
        }

        private void create_collection(object sender, RoutedEventArgs e) {
            Window window = new CollectionCreatorWindow(null);
            if (window.ShowDialog() == true) {
                Collection collection = (window as CollectionCreatorWindow).Collection;
                if (CurrentProfile.Collections.Find(p => p.CollectionTag == collection.CollectionTag) == null) {
                    CurrentProfile.Collections.Add(collection);
                    UpdateCollectionsMenuItems();
                    // Assuming UpdateColors is a method in MainWindow
                    // You might need to pass a reference to MainWindow or refactor this method
                    // UpdateColors();
                    TV_clips_collections.Items.Refresh();
                } else {
                    MessageBox.Show("Коллекция с таким названием уже существует");
                }
            }
        }
    }
}
