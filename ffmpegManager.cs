using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClipsOrganizer {
    class ffmpegManager {
        string ffmpegpath;
        public ffmpegManager(String ffmpegpath) {
            ffmpegpath = ffmpegpath;
            if(!File.Exists(ffmpegpath)) { 
                 
            }
        }
    }
}
