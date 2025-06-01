using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NAudio;

using ClipsOrganizer.Model;
using System.Reflection.Metadata;

namespace ClipsOrganizer.Classes {

    public class ExportFileInfoAudio : ExportFileInfoBase {
        public int AudioSampleRate { get; set; }
        public ExportAudioFormat outputFormat { get; set; } = ExportAudioFormat.mp3;
        public int AudioBitrate { get; set; } = 128;
        public int AudioChannels { get; set; } = 2;
        public bool NormalizeAudio { get; set; } = true;
        public TimeSpan TrimStart { get; set; }
        public TimeSpan TrimEnd { get; set; }

    }
    public enum ExportAudioFormat {
        mp3,
        wav
    }
}