﻿using System;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ClipsOrganizer.ExportControls {
    /// <summary>
    /// Логика взаимодействия для ExportLocalControl.xaml
    /// </summary>
    public partial class ExportLocalControl : UserControl {
        public ExportLocalControl() {
            InitializeComponent();
        }


        private void TB_TargetFolder_PreviewKeyUp(object sender, KeyEventArgs e) {
            TB_FolderExists.Visibility = DataValidation.InputValidator.IsFolderExists(TB_TargetFolder.Text) ? Visibility.Hidden : Visibility.Visible;
        }
    }
}
