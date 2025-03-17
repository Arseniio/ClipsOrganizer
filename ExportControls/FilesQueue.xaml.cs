using ClipsOrganizer.Settings;
using ClipsOrganizer.ViewableControls.ImageControls;
using ClipsOrganizer.ViewableControls.VideoControls;
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

namespace ClipsOrganizer.ExportControls {
    /// <summary>
    /// Логика взаимодействия для FilesQueue.xaml
    /// </summary>
    public partial class FilesQueue : UserControl {
        public FilesQueue() {
            InitializeComponent();
            LB_Queue.DataContext = ExportQueue._queue;
            LB_Queue.ItemsSource = ExportQueue._queue;
            TB_QueueLength.Text = $"Очередь: {ExportQueue._queue.Count} ожидающих заданий";
        }

        private void Btn_Clear_queue_Click(object sender, RoutedEventArgs e) {
            ExportQueue.Clear();
        }

        private async void LB_Queue_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (LB_Queue.SelectedItem is ExportFileInfoVideo selectedVideo) {
                var formattedText = await selectedVideo.GetVideoParams();
                TB_Data.Inlines.Clear();
                foreach (var line in formattedText.Split(new[] { Environment.NewLine }, StringSplitOptions.None)) {
                    if (line.Contains("🎥") || line.Contains("🔊") || line.Contains("📝")) {
                        TB_Data.Inlines.Add(new Run(line)
                        {
                            FontWeight = FontWeights.Bold,
                            Foreground = (Brush)FindResource("MaterialDesignBody")
                        });
                    }
                    else if (line.StartsWith("   ")) {
                        TB_Data.Inlines.Add(new Run(line)
                        {
                            Foreground = (Brush)FindResource("MaterialDesignBody")
                        });
                    }
                    else {
                        TB_Data.Inlines.Add(new Run(line)
                        {
                            Foreground = (Brush)FindResource("MaterialDesignBodyLight")
                        });
                    }
                    TB_Data.Inlines.Add(new LineBreak());
                }
                UC_Queue_Actions.Content = new VideoActions(selectedVideo);
            }
            else if (LB_Queue.SelectedItem is ExportFileInfoImage selectedImage) {
                var formattedText = await selectedImage.GetImageParams();
                TB_Data.Inlines.Clear();

                foreach (var line in formattedText.Split(new[] { Environment.NewLine }, StringSplitOptions.None)) {
                    if (line.Contains("🖼️") || line.Contains("📷") || line.Contains("📸") || line.Contains("🌍")) {
                        TB_Data.Inlines.Add(new Run(line)
                        {
                            FontWeight = FontWeights.Bold,
                            Foreground = (Brush)FindResource("MaterialDesignBody")
                        });
                    }
                    else if (line.StartsWith("   ") || line.StartsWith("   •") || line.StartsWith("   ◈")) {
                        TB_Data.Inlines.Add(new Run(line)
                        {
                            Foreground = (Brush)FindResource("MaterialDesignBody")
                        });
                    }
                    else {
                        TB_Data.Inlines.Add(new Run(line)
                        {
                            Foreground = (Brush)FindResource("MaterialDesignBodyLight")
                        });
                    }
                    TB_Data.Inlines.Add(new LineBreak());
                }

                UC_Queue_Actions.Content = new ImageActions(selectedImage);
            }
        }

    }
}
