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

using ClipsOrganizer.Model;

namespace ClipsOrganizer.ViewableControls.AudioControls {
    /// <summary>
    /// Логика взаимодействия для AudioViewer.xaml
    /// </summary>
    public partial class AudioViewer : UserControl {

        public AudioViewer(Item LoadedFile) {
            InitializeComponent();
            WaveForm_Viewer.FilePath = LoadedFile.Path;

            Loaded += AudioViewer_Loaded;
            Unloaded += AudioViewer_Unloaded;
            DispatcherTimer dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += DispatcherTimer_Tick;
            dispatcherTimer.Interval = TimeSpan.FromMilliseconds(200);
            dispatcherTimer.Start();
        }

        private void DispatcherTimer_Tick(object sender, EventArgs e) {
            if (WaveForm_Viewer.st is not null && this.IsLoaded && WaveForm_Viewer.IsLoaded)
                TB_zoom.Text = $"X: {WaveForm_Viewer.st.ScaleX} Y: {WaveForm_Viewer.st.ScaleY}";
        }

        private void AudioViewer_Unloaded(object sender, RoutedEventArgs e) {
            ViewableController.FileLoaded -= ViewableController_FileLoaded;
        }

        private void ViewableController_FileLoaded(object sender, FileLoadedEventArgs e) {
            WaveForm_Viewer.FilePath = e.Item.Path;
            WaveForm_Viewer.Resolution = 120;
        }

        private void AudioViewer_Loaded(object sender, RoutedEventArgs e) {
            ViewableController.FileLoaded += ViewableController_FileLoaded;
        }

        private void Button_Click(object sender, RoutedEventArgs e) {
            WaveForm_Viewer.loadWaveForm();
        }

        private void TB_Samplerate_LostFocus(object sender, RoutedEventArgs e) {
            WaveForm_Viewer.SamplesPerChunk = int.Parse((sender as TextBox).Text);
        }
    }
}
