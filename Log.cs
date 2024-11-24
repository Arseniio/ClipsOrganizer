using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace ClipsOrganizer {
    public struct Log {
        public static TextBlock TB_log { get; set; }
        public void Update(String text) {
            if (TB_log != null) {
                TB_log.Text = text;
                var fadeOutAnimation = new DoubleAnimation
                {
                    From = 1.0,
                    To = 0.0,  
                    BeginTime = TimeSpan.FromSeconds(2),
                    Duration = TimeSpan.FromSeconds(1), 
                };
                TB_log.Opacity = 1.0;
                TB_log.BeginAnimation(TextBlock.OpacityProperty, fadeOutAnimation);
            }
        }
    }
}
