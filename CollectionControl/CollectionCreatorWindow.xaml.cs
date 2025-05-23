﻿using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using ClipsOrganizer.Collections;
using System.Text.RegularExpressions;

namespace ClipsOrganizer {
    /// <summary>
    /// Логика взаимодействия для CollectionCreatorWindow.xaml
    /// </summary>
    public partial class CollectionCreatorWindow : Window {
        public Collection Collection { get; private set; }

        public CollectionCreatorWindow(Collection collection = null) {
            InitializeComponent();
            Collection = collection;
            if (collection != null) Btn_createCollection.Content = "Сохранить";
            DataContext = Collection;
        }
        private void Btn_ColorPicker_Click(object sender, RoutedEventArgs e) {
            System.Windows.Forms.ColorDialog colorDialog = new System.Windows.Forms.ColorDialog();
            if (colorDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                var color = colorDialog.Color;
                double luminance = 0.2126 * color.R + 0.7152 * color.G + 0.0722 * color.B; //??
                if (luminance < 50) {
                    var result = MessageBox.Show("Выбранный цвет слишком тёмный. Вы уверены, что хотите использовать этот цвет?", "Предупреждение", MessageBoxButton.YesNo);
                    if (result == MessageBoxResult.Yes) {
                        TB_color.Text = $"#{color.R:X2}{color.G:X2}{color.B:X2}";
                    }
                }
                else if (luminance > 200) {
                    var result = MessageBox.Show("Выбранный цвет слишком яркий. Вы уверены, что хотите использовать этот цвет?", "Предупреждение", MessageBoxButton.YesNo);
                    if (result == MessageBoxResult.Yes) {
                        TB_color.Text = $"#{color.R:X2}{color.G:X2}{color.B:X2}";
                    }
                }
                else {
                    TB_color.Text = $"#{color.R:X2}{color.G:X2}{color.B:X2}";
                }
            }
        }


        private void Btn_createCollection_Click(object sender, RoutedEventArgs e) {
            StringBuilder err = new StringBuilder();
            if (TB_keybind.Text != Collection?.KeyBinding) {
                if (!HasModifierKey) err.AppendLine("Невозможно создать горячую клавишу без модификатора");
                if (!HasMainKey) err.AppendLine("Невозможно создать горячую клавишу без основной кнопки");
            }
            if (string.IsNullOrWhiteSpace(TB_color.Text)) TB_color.Text = "#FFFFFF"; //default color to not trip regex check if field is empty
            if (string.IsNullOrWhiteSpace(TB_CollName.Text)) err.AppendLine("Неверное название коллекции");
            if (!Regex.Match(TB_color.Text, @"^#?[0-9a-fA-F]{6}").Success) err.Append("Неверный цвет");
            if (err.Length > 0) {
                System.Windows.MessageBox.Show(err.ToString(), "Ошибка создания коллекции");
                return;
            }
            Collection = Collection ?? new Collection();
            Collection.CollectionTag = TB_CollName.Text;
            Collection.Color = TB_color.Text;
            Collection.Files = Collection.Files ?? new List<Model.Item>();
            Collection.KeyBinding = TB_keybind.Text;
            this.DialogResult = true;
            this.Close();
        }

        // Yeah that code sucks
        private bool HasModifierKey = false;
        private bool HasMainKey = false;
        private List<Key> PreviousKey = new List<Key>();

        private void TB_keybind_PreviewKeyDown(object sender, KeyEventArgs e) {
            e.Handled = true;
            if (PreviousKey.Count == 0) {
                HasMainKey = false;
                HasModifierKey = false;
            }
            Log.Update($"mod: {HasModifierKey} main: {HasMainKey} keys: {PreviousKey.Count}");
            var modifiers = GetModifiers();
            Key? key = e.Key != Key.System ? e.Key : e.SystemKey;
            HasModifierKey = modifiers.Length > 0;
            HasMainKey = key != null && !IsModifierKey(key.Value);
            if (modifiers.Length == 0 && IsModifierKey(key.Value))
                return;
            if (IsModifierKey(key.Value)) {
                key = null;
            }
            var shortcut = FormatKeyCombination(modifiers, key);
            TB_keybind.Text = shortcut;
            SaveShortcut(modifiers, key);
            if (!PreviousKey.Contains(e.Key)) PreviousKey.Add(e.Key);
        }

        private void TB_keybind_KeyDown(object sender, KeyEventArgs e) {
            e.Handled = true;
        }

        private string FormatKeyCombination(string[] modifiers, Key? key) {
            var sb = new StringBuilder();
            if (modifiers.Length > 0) {
                sb.Append(string.Join(" + ", modifiers));
                sb.Append(" + ");
            }
            if (key != null) sb.Append(key.ToString());
            return sb.ToString();
        }

        private string[] GetModifiers() {
            var modifiers = new[]
            {
        Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl) ? "Ctrl" : null,
        Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt) ? "Alt" : null,
        Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift) ? "Shift" : null,
        Keyboard.IsKeyDown(Key.LWin) || Keyboard.IsKeyDown(Key.RWin) ? "Win" : null
    };

            return modifiers.Where(m => m != null).ToArray();
        }

        private bool IsModifierKey(Key key) {
            return key == Key.LeftCtrl || key == Key.RightCtrl ||
                   key == Key.LeftAlt || key == Key.RightAlt ||
                   key == Key.LeftShift || key == Key.RightShift ||
                   key == Key.LWin || key == Key.RWin;
        }

        private string SaveShortcut(string[] modifiers, Key? key) {
            string shortcut = FormatKeyCombination(modifiers, key);
            return shortcut;
        }

        private void ResetKeyStates() {
            HasModifierKey = false;
            HasMainKey = false;
        }

        private void TB_keybind_KeyUp(object sender, KeyEventArgs e) {
            PreviousKey.Remove(e.Key);
            Log.Update($"mod: {HasModifierKey.ToString()} main: {HasMainKey.ToString()} keys: {PreviousKey.Count}");
        }
    }
}
