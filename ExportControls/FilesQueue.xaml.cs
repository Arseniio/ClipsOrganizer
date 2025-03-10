using ClipsOrganizer.Settings;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ClipsOrganizer.ExportControls
{
    /// <summary>
    /// Логика взаимодействия для FilesQueue.xaml
    /// </summary>
    public partial class FilesQueue : UserControl
    {
        public FilesQueue()
        {
            InitializeComponent();
            LB_Queue.DataContext = ExportQueue._queue;
            TB_QueueLength.Text = $"Очередь: {ExportQueue._queue.Count} ожидающих заданий";
        }

    }
}
