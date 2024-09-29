using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace ClipsOrganizer {
    public struct Log {
        public static TextBlock TB_log { get; set; }
        public void Update(String text) {
            if(TB_log != null) TB_log.Text = text;
        }
    }
}
