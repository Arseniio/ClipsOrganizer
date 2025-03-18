using System;
using System.ComponentModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Threading;

public class Log : INotifyPropertyChanged {
    private static Log _instance;
    public static Log Instance => _instance ??= new Log();

    private StringBuilder _allLogInfo = new StringBuilder();
    public string AllLogInfo {
        get => _allLogInfo.ToString();
        private set {
            _allLogInfo.Clear();
            _allLogInfo.Append(value);
            OnPropertyChanged(nameof(AllLogInfo));
        }
    }

    public static TextBlock TB_log { get; set; }
    private static DoubleAnimation _fadeOutAnimation;
    private static DispatcherTimer _resetTimer;

    public event PropertyChangedEventHandler PropertyChanged;

    protected void OnPropertyChanged(string propertyName) {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public static void Update(string text) {
        if (TB_log != null) {
            TB_log.Text = text.Replace("\r\n", "");
            TB_log.BeginAnimation(TextBlock.OpacityProperty, null);
            TB_log.Opacity = 1.0;
            _resetTimer?.Stop();
            if (_fadeOutAnimation == null) {
                _fadeOutAnimation = new DoubleAnimation
                {
                    From = 1.0,
                    To = 0.0,
                    Duration = TimeSpan.FromMilliseconds(500),
                    FillBehavior = FillBehavior.HoldEnd
                };
            }
            if (_resetTimer == null) {
                _resetTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(2)
                };
                _resetTimer.Tick += (s, e) =>
                {
                    _resetTimer.Stop();
                    TB_log.BeginAnimation(TextBlock.OpacityProperty, _fadeOutAnimation);
                };
            }
            Instance.AllLogInfo += text + Environment.NewLine;
            _resetTimer.Start();
        }
    }
}