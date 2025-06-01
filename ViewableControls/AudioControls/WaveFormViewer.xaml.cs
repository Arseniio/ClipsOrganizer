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
using System.Windows.Threading;

using ClipsOrganizer.Settings;

using NAudio.Wave;

namespace ClipsOrganizer.ViewableControls.AudioControls {
    /// <summary>
    /// Логика взаимодействия для WaveFormViewer.xaml
    /// </summary>
    /// 
    public class RefWfV : FrameworkElement {
        private Dictionary<string, DrawingVisual> visuals = new();

        public double height { get; set; }
        public string filename { get; set; }
        public TimeSpan TotalTime { get; set; }
        public double ZoomFactor { get; set; } = 1.0;
        public event EventHandler<TimeSpan> OnElementClicked;
        public int pointCount => waveformPoints.Count;

        private List<float> waveformPoints = new();

        public double selectedPoint = 0;
        public TimeSpan TimePerOnePoint => TotalTime / waveformPoints.Count;

        public RefWfV() {
            this.MouseLeftButtonDown += RefWfV_MouseLeftButtonDown;
        }
        public void LoadBasicInfo(List<float> waveformPoints, TimeSpan TotalTime) {
            this.waveformPoints = waveformPoints;
            this.TotalTime = TotalTime;
        }

        ~RefWfV() {
            foreach (var visual in visuals.Values.ToList()) {
                RemoveVisualChild(visual);
            }
            visuals.Clear();
            InvalidateVisual();
        }
        public void RefWfV_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            double logicalX = GetLogicalX(e);
            Log.Update($"Click on: , {TimePerOnePoint * (int)logicalX}");
            selectedPoint = logicalX;
            OnElementClicked?.Invoke(this, TimePerOnePoint * (int)logicalX);
            DrawSelector(logicalX);
        }
        DispatcherTimer _positionTimer = new DispatcherTimer();
        public void SetupMediaPositionTracking(MediaPlayer mediaPlayer) {
            _positionTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(10)
            };
            _positionTimer.Tick += (s, e) =>
            {
                var position = mediaPlayer.Position;
                DrawSelector(position / TimePerOnePoint);
            };
            _positionTimer.Start();
        }

        public double GetLogicalX(MouseButtonEventArgs e) {
            var clickPoint = e.GetPosition(this);
            var waveformVisual = visuals["waveform"];
            var tt = waveformVisual.Transform as TranslateTransform ?? new TranslateTransform();
            
            // Учитываем смещение и зум при расчете логической позиции
            double logicalX = (clickPoint.X - tt.X) / ZoomFactor;
            return Math.Max(0, Math.Min(logicalX, waveformPoints.Count - 1));
        }
        public double GetLogicalX(MouseEventArgs e) {
            var clickPoint = e.GetPosition(this);
            var waveformVisual = visuals["waveform"];
            var tt = waveformVisual.Transform as TranslateTransform ?? new TranslateTransform();
            
            // Учитываем смещение и зум при расчете логической позиции
            double logicalX = (clickPoint.X - tt.X) / ZoomFactor;
            return Math.Max(0, Math.Min(logicalX, waveformPoints.Count - 1));
        }

        public void OnDrag(MouseEventArgs e) {
            double logicalX = GetLogicalX(e);

            DrawFiller(selectedPoint, logicalX);
            Debug.WriteLine($"Selected from {selectedPoint * TimePerOnePoint} to {logicalX * TimePerOnePoint} ");
        }

        public void DrawSelector(double logicalX) {
            var visual = new DrawingVisual();
            var waveformVisual = visuals["waveform"];
            visual.Transform = waveformVisual.Transform;
            using (var dc = visual.RenderOpen()) {
                var pen = new Pen(Brushes.DarkKhaki, 3);
                // Учитываем зум при отрисовке селектора
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
                // Учитываем зум при отрисовке выделения
                var p1 = new Point(startPoint * ZoomFactor, height);
                var p2 = new Point(endPoint * ZoomFactor, 0);
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

                // Улучшенная логика масштабирования временных меток
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
                double lastTextEnd = 0;

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

                    // Проверяем, не перекрывается ли текст
                    double tx = x - formattedText.Width / 2;
                    if (tx > lastTextEnd) {
                        double ty = 0;
                        dc.DrawText(formattedText, new Point(tx, ty));
                        dc.DrawLine(pen, new Point(x, 20), new Point(x, this.height));
                        lastTextEnd = tx + formattedText.Width;
                    }
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
        public void ClearAllElements() {
            foreach (var visual in visuals.Values.ToList()) {
                RemoveVisualChild(visual);
            }
            visuals.Clear();
            InvalidateVisual();
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
        public int SamplesPerChunk = 5000;
        List<float> waveform = new List<float>();
        public TimeSpan? TrimStart { get; set; }
        public TimeSpan? TrimEnd { get; set; }
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

        public RefWfV _Visual;
        private DispatcherTimer _positionTimer;



        public WaveFormViewer() {
            InitializeComponent();
            MainGrid.Loaded += (s, e) =>
            {
                MainGrid.SizeChanged += MainGrid_SizeChanged;
                _Visual = new RefWfV() { height = MainGrid.RowDefinitions[0].ActualHeight };
            };
        }
        public void loadWaveForm() {
            waveform.Clear();
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
                _Visual.LoadBasicInfo(waveform, reader.TotalTime);
                _Visual.ClearAllElements();

                PreloadComponent(_Visual);
                _Visual.DrawTimeLine();
                _Visual.DrawWaveform();
                SL_XPos.Maximum = waveform.Count;
            }
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
                    // Устанавливаем значения выделения
                    double start = _Visual.selectedPoint;
                    double end = _Visual.GetLogicalX(e);
                    if (_Visual.TotalTime != TimeSpan.Zero && _Visual.pointCount > 0) {
                        var t1 = TimeSpan.FromTicks((long)(_Visual.TimePerOnePoint.Ticks * Math.Min(start, end)));
                        var t2 = TimeSpan.FromTicks((long)(_Visual.TimePerOnePoint.Ticks * Math.Max(start, end)));
                        TrimStart = t1;
                        TrimEnd = t2;
                    }
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
        }

        private void MainGrid_SizeChanged(object sender, SizeChangedEventArgs e) {
            if (_Visual != null)
                _Visual.height = MainGrid.ActualHeight;
        }
    }
}
