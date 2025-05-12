using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Printing;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

using System.Windows.Input;
using System.Windows.Media;

using NAudio.Wave;

using Windows.ApplicationModel.Activation;

namespace ClipsOrganizer.ViewableControls.AudioControls {
    /// <summary>
    /// Логика взаимодействия для WaveFormViewer.xaml
    /// </summary>
    /// 

    public abstract class BaseVisualHost : FrameworkElement {
        private DrawingVisual _visual;
        protected readonly VisualCollection _children;
        public ScaleTransform ScaleTransform { get; set; }
        public TranslateTransform translateTransform { get; set; }
        protected BaseVisualHost() {
            _children = new VisualCollection(this);
            TransformGroup group = new TransformGroup();
            ScaleTransform = new ScaleTransform();
            group.Children.Add(ScaleTransform);
            translateTransform = new TranslateTransform();
            group.Children.Add(translateTransform);
            this.RenderTransform = group;
            this.RenderTransformOrigin = new Point(0, Application.Current.MainWindow.ActualHeight / 2);
            _visual = new DrawingVisual();
            _children.Add(_visual);
        }
        protected override int VisualChildrenCount => _children.Count;
        protected override Visual GetVisualChild(int index) => _children[index];
        public virtual void ClearVisuals() {
            _children.Clear();
        }



    }
    public class WavefromSelector : BaseVisualHost {
        public double currentTime;
        private Visual SelectorVisual = null;
        public WavefromSelector() : base() {

        }
        public void OnMouseClicked(Point point, double rawScroll, double heigth) {
            DrawLineOnClick(point.X + rawScroll, heigth);
        }
        private void DrawLineOnClick(double xPos, double heigth) {
            if (SelectorVisual != null) {
                _children.Remove(SelectorVisual);
                SelectorVisual = null;
            }
            var visual = new DrawingVisual();
            using (DrawingContext dc = visual.RenderOpen()) {
                Pen pen = new Pen(Brushes.Aqua, 5);
                dc.DrawLine(pen, new Point(xPos, 0), new Point(xPos, heigth));
                SelectorVisual = visual;
                _children.Add(SelectorVisual);
            }
        }
        public void MoveLine(double xPos) {
            this.translateTransform.X = xPos;
        }
        public void RecalcZoom(double xPos, double zoom) {
            this.translateTransform.X = translateTransform.X * zoom;
        }
    }
    public class WaveformVisualHost : BaseVisualHost {
        private readonly List<DrawingVisual> visuals = new();
        public int ChildrenCount => visuals.Count;
        public TimeSpan TimePerOnePoint;
        public int points = 0;
        public WaveformVisualHost() : base() {

        }
        public void AddVisual(DrawingVisual visual) {
            visuals.Add(visual);
            _children.Add(visual);
        }
        public override void ClearVisuals() {
            foreach (var visual in visuals) {
                RemoveVisualChild(visual);
                RemoveLogicalChild(visual);
            }
            visuals.Clear();
            //base.ClearVisuals();
        }
        public void ClampPan() {
            var tg = (TransformGroup)RenderTransform;
            var st = (ScaleTransform)tg.Children.OfType<ScaleTransform>().First();
            var tt = (TranslateTransform)tg.Children.OfType<TranslateTransform>().First();
            double scaledWidth = points * st.ScaleX;
            double viewport = ActualWidth;
            double minX = viewport - scaledWidth;
            if (minX > 0) minX = 0;
            tt.X = Math.Min(0, Math.Max(tt.X, minX));
        }
        public void DrawWaveform(List<float> waveform, double height, TimeSpan TimePerOnePoint, int blockSize = 5) {
            var visual = new DrawingVisual();
            this.TimePerOnePoint = TimePerOnePoint;
            int localPoints = 0;
            using (DrawingContext dc = visual.RenderOpen()) {
                Pen pen = new Pen(Brushes.Blue, 3);
                for (localPoints = 0; localPoints < waveform.Count; localPoints += blockSize) {
                    var block = waveform.Skip(localPoints).Take(blockSize).ToArray();
                    if (block.Length == 0) continue;
                    float blockMax = block.Max();
                    float blockMin = block.Min();
                    double maxHeight = blockMax * height;
                    double minHeight = blockMin * height;
                    double x = localPoints;
                    dc.DrawLine(pen,
                        new Point(x, height / 2 - maxHeight / 2),
                        new Point(x, height / 2 + minHeight / 2));
                }
            }
            points = localPoints;
            AddVisual(visual);
        }
    }

    public class TimelineVisualHost : BaseVisualHost {
        private DrawingVisual timeLineVisual = null;
        public TimeSpan TimePerOnePoint;
        public int Points = 0;
        public TimelineVisualHost() : base() { }
        public void DrawTimeLine(int points, TimeSpan timePerOnePoint, double zoom = 1, double Height = 100) {
            this.Points = points;
            this.TimePerOnePoint = timePerOnePoint;
            RemoveTimeLine();

            var visual = new DrawingVisual();
            using (DrawingContext dc = visual.RenderOpen()) {
                Pen pen = new Pen(Brushes.DarkRed, 1);
                TimeSpan time = TimeSpan.Zero;

                // выбираем шаг подписей исходя из зума
                TimeSpan labelStep;
                if (zoom >= 1.4)
                    labelStep = TimeSpan.FromMilliseconds(500);
                else if (zoom >= 1.2)
                    labelStep = TimeSpan.FromSeconds(1);
                else if (zoom >= 0.7)
                    labelStep = TimeSpan.FromSeconds(5);
                else
                    labelStep = TimeSpan.FromSeconds(10);

                int labelInterval = (int)(labelStep.Ticks / timePerOnePoint.Ticks);
                var typeface = new Typeface("Segoe UI");

                for (int i = 0; i < points; i++) {
                    time += timePerOnePoint;
                    if (i % labelInterval != 0) continue;
                    double x = i * zoom;

                    var formattedText = new FormattedText(
                        time.ToString(@"hh\:mm\:ss\.ffff"),
                        CultureInfo.InvariantCulture,
                        FlowDirection.LeftToRight,
                        typeface,
                        16,
                        Brushes.Black,
                        VisualTreeHelper.GetDpi(this).PixelsPerDip
                    );
                    double tx = x - formattedText.Width / 2;
                    double ty = 0;
                    dc.DrawText(formattedText, new Point(tx, ty));

                    dc.DrawLine(pen, new Point(x, 20), new Point(x, Height));
                }
            }
            //visual.ContentBounds;
            timeLineVisual = visual;
            _children.Add(timeLineVisual);
        }

        public void RemoveTimeLine() {
            if (timeLineVisual != null) {
                _children.Remove(timeLineVisual);
                timeLineVisual = null;
            }
        }
    }
    public class MyVisualItem {
        public DrawingVisual Visual { get; set; }
        public string Id { get; set; } // фильтрация по нему
    }

    public class RefWfV : FrameworkElement {
        private Dictionary<string, DrawingVisual> visuals = new();

        public double height { get; set; }
        public string filename { get; set; }
        public TimeSpan TotalTime { get; set; }

        private List<float> waveformPoints = new();

        private TimeSpan TimePerOnePoint => TotalTime / waveformPoints.Count;

        public RefWfV(List<float> waveformPoints, TimeSpan TotalTime) {
            this.waveformPoints = waveformPoints;
            this.TotalTime = TotalTime;
        }

        public void DrawWaveform(int blockSize = 5) {
            var visual = new DrawingVisual();
            using (DrawingContext dc = visual.RenderOpen()) {
                Pen pen = new Pen(Brushes.Blue, 3);
                for (int i = 0; i < waveformPoints.Count; i += blockSize) {
                    var block = waveformPoints.Skip(i).Take(blockSize).ToArray();
                    if (block.Length == 0) continue;
                    float blockMax = block.Max();
                    float blockMin = block.Min();
                    double maxHeight = blockMax * height;
                    double minHeight = blockMin * height;
                    double x = i;
                    dc.DrawLine(pen,
                        new Point(x, height / 2 - maxHeight / 2),
                        new Point(x, height / 2 + minHeight / 2));
                }
            }

            visuals["waveform"] = visual;
            AddVisualChild(visual);
            AddLogicalChild(visual);
        }

        public void DrawTimeLine(double zoom = 1) {
            var visual = new DrawingVisual();
            using (DrawingContext dc = visual.RenderOpen()) {
                Pen pen = new Pen(Brushes.DarkRed, 1);
                TimeSpan time = TimeSpan.Zero;

                TimeSpan labelStep;
                if (zoom >= 1.4)
                    labelStep = TimeSpan.FromMilliseconds(500);
                else if (zoom >= 1.2)
                    labelStep = TimeSpan.FromSeconds(1);
                else if (zoom >= 0.7)
                    labelStep = TimeSpan.FromSeconds(5);
                else
                    labelStep = TimeSpan.FromSeconds(10);

                int labelInterval = (int)(labelStep.Ticks / TimePerOnePoint.Ticks);
                var typeface = new Typeface("Segoe UI");

                for (int i = 0; i < waveformPoints.Count; i++) {
                    time += TimePerOnePoint;
                    if (i % labelInterval != 0) continue;
                    double x = i * zoom;
                    var formattedText = new FormattedText(
                        time.ToString(@"hh\:mm\:ss\.ffff"),
                        CultureInfo.InvariantCulture,
                        FlowDirection.LeftToRight,
                        typeface,
                        16,
                        Brushes.Black,
                        VisualTreeHelper.GetDpi(this).PixelsPerDip
                    );
                    double tx = x - formattedText.Width / 2;
                    double ty = 0;
                    dc.DrawText(formattedText, new Point(tx, ty));

                    dc.DrawLine(pen, new Point(x, 20), new Point(x, Height));
                }
            }

            visuals["timeline"] = visual;
            AddVisualChild(visual);
            AddLogicalChild(visual);
        }

        protected override int VisualChildrenCount => visuals.Count;

        protected override Visual GetVisualChild(int index) {
            if (index < 0 || index >= visuals.Count)
                throw new ArgumentOutOfRangeException();
            return visuals.Values.ElementAt(index);
        }

        // Дополнительно: метод для доступа к конкретному visual по ID
        public DrawingVisual? GetVisualById(string id) =>
            visuals.TryGetValue(id, out var vis) ? vis : null;
    }


    public partial class WaveFormViewer : UserControl {
        public string FilePath { get; set; }
        public int Resolution { get; set; }
        WaveformVisualHost host = null;
        TimelineVisualHost tlHost = null;
        WavefromSelector Selwf = null;

        public int SamplesPerChunk = 5000;

        List<float> waveform = new List<float>();
        private void PreloadComponent(FrameworkElement elem, bool overrideMouse = true) {
            if (overrideMouse) {
                elem.MouseLeftButtonDown += Host_MouseLeftButtonDown;
                elem.MouseLeftButtonUp += Host_MouseLeftButtonUp;
                elem.MouseDown += Host_MouseDown;
                elem.MouseWheel += Host_MouseWheel;
            }
            Grid.SetRow(elem, 0);
            Grid.SetColumn(elem, 0);
            elem.HorizontalAlignment = HorizontalAlignment.Stretch;
            elem.VerticalAlignment = VerticalAlignment.Stretch;
            elem.Margin = new Thickness(0);
            //elem.ClipToBounds = true;
            //elem.HorizontalAlignment = HorizontalAlignment.Center;
            //elem.VerticalAlignment = VerticalAlignment.Center;
            MainGrid.Children.Add(elem);
        }

        private RefWfV _Visual;

        public WaveFormViewer() {
            //host = new WaveformVisualHost();
            //tlHost = new TimelineVisualHost();
            //Selwf = new WavefromSelector();
            //InitializeComponent();
            //PreloadComponent(host);
            //PreloadComponent(tlHost);
            //PreloadComponent(Selwf, false);
            InitializeComponent();
        }
        public void loadWaveForm() {
            waveform.Clear();
            //host.ClearVisuals();
            //WaveForm_canvas.Children.Clear();
            using (var reader = new AudioFileReader(FilePath)) {
                int totalSeconds = (int)reader.TotalTime.TotalSeconds;
                int totalSamples = reader.WaveFormat.SampleRate * totalSeconds;
                float[] buffer = new float[totalSamples];

                int samplesRead;

                while ((samplesRead = reader.Read(buffer, 0, buffer.Length)) > 0) {
                    for (int i = 0; i < samplesRead; i += samplesPerChunk) {
                        int chunkSize = Math.Min(samplesPerChunk, samplesRead - i);
                        float max = buffer.Skip(i).Take(chunkSize).Max(x => Math.Abs(x));
                        waveform.Add(max);
                    }
                }
                _Visual = new RefWfV(waveform, reader.TotalTime) { height = MainGrid.RowDefinitions[0].ActualHeight };
                PreloadComponent(_Visual);
                _Visual.DrawTimeLine();
                _Visual.DrawWaveform(100);
                //host.DrawWaveform(waveform, this.ActualHeight, TimeSpan.FromMilliseconds(reader.TotalTime.TotalMilliseconds / waveform.Count));
                //tlHost.DrawTimeLine(waveform.Count, TimeSpan.FromMilliseconds(reader.TotalTime.TotalMilliseconds / waveform.Count), GetScaleTransform(host).ScaleX, this.ActualHeight);
            }

            //SL_XPos.Maximum = _Visual;
        }
        public double zoom;
        public ScaleTransform st = null;
        private int samplesPerChunk = 500;

        private void Host_MouseWheel(object sender, MouseWheelEventArgs e) {
            var child = host;
            if (child != null) {
                st = GetScaleTransform(child);
                var tt = GetTranslateTransform(child);

                zoom = e.Delta > 0 ? .2 : -.2;
                if (!(e.Delta > 0) && (st.ScaleX < .4))
                    return;

                Point relative = e.GetPosition(child);
                double absoluteX;
                double absoluteY;

                absoluteX = relative.X * st.ScaleX + tt.X;

                st.ScaleX += zoom;


                tt.X = absoluteX - relative.X * st.ScaleX;
                SL_XZoom.Value = st.ScaleX;
            }
        }
        private TranslateTransform GetTranslateTransform(UIElement element) {
            return (TranslateTransform)((TransformGroup)element.RenderTransform)
              .Children.First(tr => tr is TranslateTransform);
        }

        private ScaleTransform GetScaleTransform(UIElement element) {
            return (ScaleTransform)((TransformGroup)element.RenderTransform)
              .Children.First(tr => tr is ScaleTransform);
        }
        private Point origin;
        private Point start;
        private void Host_MouseDown(object sender, MouseButtonEventArgs e) {
            var child = host;
            if (e.RightButton == MouseButtonState.Pressed) {
                if (child != null) {
                    var st = GetScaleTransform(child);
                    st.ScaleX = 1.0;
                    st.ScaleY = 1.0;
                    var tt = GetTranslateTransform(child);
                    tt.X = 0.0;
                    tt.Y = 0.0;
                }
            }

            if (e.MiddleButton == MouseButtonState.Pressed) {
                var tt = GetTranslateTransform(child);
                Vector v = start - e.GetPosition(this);
                tt.X = origin.X - v.X;
                //tt.Y = origin.Y - v.Y;
            }
        }

        private void Host_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            var child = host;
            var tt = GetTranslateTransform(child);
            start = e.GetPosition(this);
            origin = new Point(tt.X, tt.Y);
            Cursor = Cursors.Hand;
            child.CaptureMouse();
        }

        private void Host_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
            var child = host;
            if (child != null) {
                child.ReleaseMouseCapture();
                Cursor = Cursors.Arrow;
            }
        }


        private void SL_YZoom_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
            if (!this.IsLoaded) return;
            var child = host;
            var st = GetScaleTransform(host);
            st.ScaleY = e.NewValue;

        }

        private void SL_XPos_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
            if (!this.IsLoaded) return;
            var child = host;
            var tt = GetTranslateTransform(host);
            var st = GetScaleTransform(host);
            ChangeScrollX(-e.NewValue);

            host.ClampPan();
        }

        private void SL_XZoom_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
            if (!this.IsLoaded) return;
            ChangeScaleX(e.NewValue);
            tlHost.DrawTimeLine(host.points, host.TimePerOnePoint, e.NewValue, this.ActualHeight);
        }
        private void ChangeScrollX(double newScroll) {
            host.translateTransform.X = newScroll;
            tlHost.translateTransform.X = newScroll;
            Selwf.MoveLine(newScroll);
            host.ClampPan();
        }
        private void ChangeScaleX(double newScale) {



            // масштабируем только waveform
            //var st = host.ScaleTransform;
            //var tt = host.translateTransform;

            //double oldScale = st.ScaleX;
            //double center = host.ActualWidth / 2;
            //double centerDataPos = (center - tt.X) / oldScale;

            //st.ScaleX = newScale;
            //tt.X = center - centerDataPos * newScale;
            //tlHost.translateTransform.X = tt.X;
            //SL_XPos.Maximum = host.points * newScale;
            //Selwf.RecalcZoom(SL_XPos.Value, newScale);
            //host.ClampPan();
            //tlHost.DrawTimeLine(host.points, host.TimePerOnePoint, newScale, ActualHeight);
        }

        private void MainGrid_MouseDown(object sender, MouseButtonEventArgs e) {
            var grid = sender as Grid;
            var pos = e.GetPosition(grid);
            double column0Width = grid.ColumnDefinitions[0].ActualWidth;
            double row0Height = grid.RowDefinitions[0].ActualHeight;
            if (pos.X >= 0 && pos.X <= column0Width &&
                pos.Y >= 0 && pos.Y <= row0Height) {
                Selwf.OnMouseClicked(pos, SL_XPos.Value, row0Height);
            }
        }
    }
}
