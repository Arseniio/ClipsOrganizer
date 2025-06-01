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
            MediaPlayer = new MediaPlayer();
            InitializeComponent();
            WaveForm_Viewer.FilePath = LoadedFile.Path;
            MediaPlayer.Open(new Uri(LoadedFile.Path));
            Loaded += AudioViewer_Loaded;
            Unloaded += AudioViewer_Unloaded;
        }

        private void _Visual_OnElementClicked(object sender, TimeSpan e) {
            if (MediaPlayer != null) {
                MediaPlayer.Position = e;
            }
        }

        private void AudioViewer_Unloaded(object sender, RoutedEventArgs e) {
            ViewableController.FileLoaded -= ViewableController_FileLoaded;
        }

        private void ViewableController_FileLoaded(object sender, FileLoadedEventArgs e) {
            WaveForm_Viewer.FilePath = e.Item.Path;
            WaveForm_Viewer.Resolution = 120;
            MediaPlayer.Open(new Uri(e.Item.Path));
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


        MediaPlayer MediaPlayer;
        private void Btn_Play_Click(object sender, RoutedEventArgs e) {
            MediaPlayer.Play();
            WaveForm_Viewer._Visual.SetupMediaPositionTracking(MediaPlayer);
            WaveForm_Viewer._Visual.OnElementClicked += _Visual_OnElementClicked;
        }

        private void Btn_Stop_Click(object sender, RoutedEventArgs e) {
            if (MediaPlayer.CanPause)
                MediaPlayer.Pause();
        }

        private void SL_volume_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
            if (this.IsLoaded)
                MediaPlayer.Volume = (double)e.NewValue;
        }

        private void Btn_keyshortcuts_Click(object sender, RoutedEventArgs e) {

        }

        private void Btn_ExportAudio_Click(object sender, RoutedEventArgs e)
        {
            var trimStart = WaveForm_Viewer.TrimStart ?? TimeSpan.Zero;
            var trimEnd = WaveForm_Viewer.TrimEnd ?? TimeSpan.Zero;
            var audioPath = WaveForm_Viewer.FilePath;
            var wnd = new ClipsOrganizer.AudioRendererWindow(new Uri(audioPath), trimStart, trimEnd);
            wnd.Show();
        }
    }
}
