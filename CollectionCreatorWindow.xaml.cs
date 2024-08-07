using System;
using System.Collections.Generic;
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
using System.Windows.Shapes;
using ClipsOrganizer.Collections;
using System.Text.RegularExpressions;
using System.Drawing;

namespace ClipsOrganizer {
    /// <summary>
    /// Логика взаимодействия для CollectionCreatorWindow.xaml
    /// </summary>
    public partial class CollectionCreatorWindow : Window {
        public Collection Collection { get; private set; }

        public CollectionCreatorWindow(Collection collection = null) {
            InitializeComponent();
            Collection = collection;
            DataContext = Collection;
        }

        private void Btn_ColorPicker_Click(object sender, RoutedEventArgs e) {
            System.Windows.Forms.ColorDialog colorDialog = new System.Windows.Forms.ColorDialog();
            if (colorDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                var color = colorDialog.Color;
                double luminance = 0.2126 * color.R + 0.7152 * color.G + 0.0722 * color.B;

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

            this.DialogResult = true;
            this.Close();
        }
    }
}
