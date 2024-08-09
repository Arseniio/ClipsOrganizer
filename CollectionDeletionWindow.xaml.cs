using ClipsOrganizer.Collections;
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

namespace ClipsOrganizer {
    /// <summary>
    /// Логика взаимодействия для CollectionDeletionWindow.xaml
    /// </summary>
    public partial class CollectionDeletionWindow : Window {
        public CollectionDeletionWindow(List<Collection> collections) {
            InitializeComponent();
            foreach (var collection in collections) {
                LB_Collections.Items.Add(new CheckBox() { Name = "LB_CB_" + collection.CollectionTag, Tag = collection, Content = collection.CollectionTag });
            }
        }

        public List<Collection> selectedCollections = new List<Collection>();
        private void Btn_Delete_Click(object sender, RoutedEventArgs e) {
            List<Collection> selectedCollections = new List<Collection>();
            foreach (var item in LB_Collections.Items) {
                if (item is CheckBox checkBox && checkBox.IsChecked == true) {
                    selectedCollections.Add(checkBox.Tag as Collection);
                }
            }
            this.DialogResult = true;
        }
    }
}
