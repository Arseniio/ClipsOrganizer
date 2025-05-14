using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.DirectoryServices;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

using System.Windows.Input;
using System.Windows.Media;

using NAudio.Wave;

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
        public double ZoomFactor { get; set; } = 1.0;

        public int pointCount => waveformPoints.Count;

        private List<float> waveformPoints = new();

        double selectedPoint = 0;
        private TimeSpan TimePerOnePoint => TotalTime / waveformPoints.Count;

        public RefWfV(List<float> waveformPoints, TimeSpan TotalTime) {
            this.waveformPoints = waveformPoints;
            this.TotalTime = TotalTime;
            this.MouseLeftButtonDown += RefWfV_MouseLeftButtonDown;
        }

        public void RefWfV_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            double logicalX = GetLogicalX(e);
            Log.Update($"Click on: , {TimePerOnePoint * (int)logicalX}");
            selectedPoint = logicalX;
            DrawSelector(logicalX);
        }

        private double GetLogicalX(MouseButtonEventArgs e) {
            var clickPoint = e.GetPosition(this);

            var waveformVisual = visuals["waveform"];
            var tt = waveformVisual.Transform as TranslateTransform ?? new TranslateTransform();

            double logicalX = (clickPoint.X - tt.X) / ZoomFactor;
            return logicalX;
        }
        private double GetLogicalX(MouseEventArgs e) {
            var clickPoint = e.GetPosition(this);

            var waveformVisual = visuals["waveform"];
            var tt = waveformVisual.Transform as TranslateTransform ?? new TranslateTransform();

            double logicalX = (clickPoint.X - tt.X) / ZoomFactor;
            return logicalX;
        }

        public void OnDrag(MouseEventArgs e) {
            double logicalX = GetLogicalX(e);

            DrawFiller(selectedPoint, logicalX);
            Debug.WriteLine($"Selected from {selectedPoint * TimePerOnePoint} to {logicalX * TimePerOnePoint} ");
        }

        public void DrawSelector(double logicalX) {
            //if (visuals.TryGetValue("selector", out var old)) {
            //    RemoveVisualChild(old);
            //    visuals.Remove("selector");
            //}
            var visual = new DrawingVisual();
            var waveformVisual = visuals["waveform"];
            visual.Transform = waveformVisual.Transform;
            using (var dc = visual.RenderOpen()) {
                var pen = new Pen(Brushes.DarkKhaki, 3);
                double screenX = logicalX * ZoomFactor;
                dc.DrawLine(pen,
                    new Point(screenX, 0),
                    new Point(screenX, this.height));
            }
            RedrawElement(visual, "selector");
        }

        public void DrawFiller(double startPoint, double endPoint) {
            var visual = new DrawingVisual();
            var waveformVisual = visuals["waveform"];
            visual.Transform = waveformVisual.Transform;
            using (var dc = visual.RenderOpen()) {
                var brush = new SolidColorBrush(Color.FromArgb(128, 100, 100, 100));
                var pen = new Pen(Brushes.Transparent, 0);
                var p1 = new Point(startPoint, height);
                var p2 = new Point(endPoint, 0);
                Debug.WriteLine($"{p1} : {p2}");
                dc.DrawRectangle(brush, pen, new Rect(p1, p2));
            }
            RedrawElement(visual, "filler");
        }

        public void DrawWaveform(int density = 5) {
            var sw = Stopwatch.StartNew();
            Debug.WriteLine($"WaveForm Start: {sw.ElapsedMilliseconds}");
            var visual = new DrawingVisual();
            using (DrawingContext dc = visual.RenderOpen()) {
                Pen pen = new Pen(Brushes.Blue, 2);
                
                // Новая логика масштабирования
                if (ZoomFactor >= 3.0) {
                    density = 1;
                }
                else if (ZoomFactor >= 2.0) {
                    density = 2;
                }
                else if (ZoomFactor >= 1.0) {
                    density = 5;
                }
                else if (ZoomFactor >= 0.5) {
                    density = 10;
                }
                else if (ZoomFactor >= 0.25) {
                    density = 20;
                }
                else {
                    density = 40;
                }

                Debug.WriteLine($"WaveForm Started drawing points: {sw.ElapsedMilliseconds}");

                for (int i = 0; i < waveformPoints.Count; i += density) {
                    var block = waveformPoints.Skip(i).Take(density).ToArray();
                    if (block.Length == 0) continue;
                    double maxHeight = block.Max() * height;
                    double minHeight = block.Min() * height;
                    double x = i * ZoomFactor;
                    dc.DrawLine(pen,
                        new Point(x, height / 2 - maxHeight / 2),
                        new Point(x, height / 2 + minHeight / 2));
                }
                Debug.WriteLine($"WaveForm Done drawing points: {sw.ElapsedMilliseconds}");
            }
            Debug.WriteLine($"WaveForm Started redrawing points: {sw.ElapsedMilliseconds}");
            RedrawElement(visual, "waveform");
            Debug.WriteLine($"WaveForm Ended redrawing points: {sw.ElapsedMilliseconds}");
            sw.Stop();
        }

        private void RedrawElement(DrawingVisual visual, string name) {
            if (visuals.ContainsKey(name)) {
                var oldVisual = visuals[name];
                RemoveVisualChild(oldVisual);
                visuals.Remove(name);
            }
            visuals[name] = visual;
            AddVisualChild(visual);
            InvalidateVisual();
        }

        public void DrawTimeLine(double zoom = 1) {
            var visual = new DrawingVisual();
            using (DrawingContext dc = visual.RenderOpen()) {
                Pen pen = new Pen(Brushes.DarkRed, 1);
                TimeSpan time = TimeSpan.Zero;

                // Новая логика масштабирования временных меток
                TimeSpan labelStep;
                if (ZoomFactor >= 3.0)
                    labelStep = TimeSpan.FromMilliseconds(100);
                else if (ZoomFactor >= 2.0)
                    labelStep = TimeSpan.FromMilliseconds(250);
                else if (ZoomFactor >= 1.0)
                    labelStep = TimeSpan.FromMilliseconds(500);
                else if (ZoomFactor >= 0.5)
                    labelStep = TimeSpan.FromSeconds(1);
                else if (ZoomFactor >= 0.25)
                    labelStep = TimeSpan.FromSeconds(2);
                else
                    labelStep = TimeSpan.FromSeconds(5);

                int labelInterval = (int)(labelStep.Ticks / TimePerOnePoint.Ticks);
                var typeface = new Typeface("Segoe UI");

                for (int i = 0; i < waveformPoints.Count; i++) {
                    time += TimePerOnePoint;
                    if (i % labelInterval != 0) continue;
                    double x = i * ZoomFactor;
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

                    dc.DrawLine(pen, new Point(x, 20), new Point(x, this.height));
                }
            }

            RedrawElement(visual, "timeline");
        }
        double current_offset_x = 0;
        public void OffsetAllVisualsX(double offsetX) {
            current_offset_x = offsetX;
            Debug.WriteLine("offset: " + offsetX);
            foreach (var kvp in visuals) {
                kvp.Value.Transform = new TranslateTransform(offsetX, 0);
            }
        }
        public void ChangeScaleX(double newZoomFactor) {
            ZoomFactor = newZoomFactor;
            DrawWaveform();
            DrawTimeLine();
        }

        protected override int VisualChildrenCount => visuals.Count;

        protected override Visual GetVisualChild(int index) {
            if (index < 0 || index >= visuals.Count)
                throw new ArgumentOutOfRangeException();
            return visuals.Values.ElementAt(index);
        }
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
        private void PreloadComponent(FrameworkElement elem) {
            Grid.SetRow(elem, 0);
            Grid.SetColumn(elem, 0);
            elem.HorizontalAlignment = HorizontalAlignment.Stretch;
            elem.VerticalAlignment = VerticalAlignment.Stretch;
            elem.Margin = new Thickness(0);
            MainGrid.Children.Add(elem);
            var rect = new Rect(0, 0, MainGrid.ColumnDefinitions[0].ActualWidth, MainGrid.RowDefinitions[0].ActualHeight);
            _Visual.Clip = new RectangleGeometry(rect);

        }

        private RefWfV _Visual;

        public WaveFormViewer() {
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
                _Visual.DrawWaveform();
                SL_XPos.Maximum = waveform.Count;
                //host.DrawWaveform(waveform, this.ActualHeight, TimeSpan.FromMilliseconds(reader.TotalTime.TotalMilliseconds / waveform.Count));
                //tlHost.DrawTimeLine(waveform.Count, TimeSpan.FromMilliseconds(reader.TotalTime.TotalMilliseconds / waveform.Count), GetScaleTransform(host).ScaleX, this.ActualHeight);
            }

            //SL_XPos.Maximum = _Visual;
        }
        public double zoom;
        public ScaleTransform st = null;
        private int samplesPerChunk = 500;


        private void SL_XPos_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
            if (!this.IsLoaded) return;
            _Visual.OffsetAllVisualsX(-e.NewValue);
        }



        private Point? _mouseDownPosition = null;

        private void MainGrid_MouseDown(object sender, MouseButtonEventArgs e) {
            _mouseDownPosition = e.GetPosition(MainGrid);
            MainGrid.CaptureMouse();
            _Visual.RefWfV_MouseLeftButtonDown(sender, e);
        }

        private void MainGrid_MouseMove(object sender, MouseEventArgs e) {
            if (_mouseDownPosition.HasValue) {
                Point currentPos = e.GetPosition(MainGrid);
                double dx = currentPos.X - _mouseDownPosition.Value.X;
                double dy = currentPos.Y - _mouseDownPosition.Value.Y;
                double distance = Math.Sqrt(dx * dx + dy * dy);
                if (distance >= 50) {
                    _Visual.OnDrag(e);
                }
            }
        }

        private void MainGrid_MouseUp(object sender, MouseButtonEventArgs e) {
            _mouseDownPosition = null;
            MainGrid.ReleaseMouseCapture();
        }




        private void SL_XZoom_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e) {
            _Visual.ChangeScaleX(SL_XZoom.Value);
            SL_XPos.Maximum = _Visual.pointCount * SL_XZoom.Value;
            Debug.Write(SL_XZoom.Value + "\n");
            TB_zoomtext.Text = $"Zoom: {SL_XZoom.Value}";
        }

        private void MainGrid_SizeChanged(object sender, SizeChangedEventArgs e) {
            _Visual.height = MainGrid.ActualHeight;
        }
    }
}
