using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace ClipsOrganizer {
    public struct Log {
        public static TextBlock TB_log { get; set; }
        private static DoubleAnimation _fadeOutAnimation;
        private static DispatcherTimer _resetTimer;
        public static StringBuilder AllLogInfo = new StringBuilder();
        public static void Update(string text) {
            if (TB_log != null) {
                TB_log.Text = text;
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
                AllLogInfo.AppendLine(text);
                _resetTimer.Start();
            }
        }
    }


}
