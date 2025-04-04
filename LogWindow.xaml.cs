﻿using ClipsOrganizer.FileUtils;
using System;
using System.Collections.Generic;
using System.IO;
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
    /// Логика взаимодействия для LogWindow.xaml
    /// </summary>
    public partial class LogWindow : Window {
        public LogWindow(string logData = null) {
            InitializeComponent();
            DataContext = Log.Instance;

            if (logData != null) {
                TB_log_window.Text = logData;
            }
            else {
                TB_log_window.SetBinding(TextBlock.TextProperty, "AllLogInfo");
            }
        }

        private void Btn_Exit_Click(object sender, RoutedEventArgs e) {
            this.Close();
        }

        private void Btn_Save_Click(object sender, RoutedEventArgs e) {
            File.WriteAllText("./Log.txt", TB_log_window.Text);
        }
    }
}
