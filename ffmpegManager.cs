using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ClipsOrganizer {
    public enum VideoCodec {
        Unknown = 0,
        H264_NVENC,
        HEVC_NVENC,
        H264_x264,
        H265_x265,
        H264_AMF,
        HEVC_AMF,
        H264_QuickSync,
        HEVC_QuickSync,
        VP8,
        VP9,
        AV1,
        MPEG4_Xvid,
        GIF,
        WebP,
        APNG
    }

    public class ffmpegManager : ExternalProcessManager {

        private bool _disposed = false;
        private bool isReady = false;

        public StringBuilder Output { get; private set; }

        private string ffmpegpath;

        public ffmpegManager(string affmpegpath) {
            this.ffmpegpath = affmpegpath;
            Output = new StringBuilder();
            if (!File.Exists(ffmpegpath)) {
                throw new FileNotFoundException();
            }
            OutputDataReceived += FfmpegManager_OutputDataReceived;
            ErrorDataReceived += FfmpegManager_OutputDataReceived;
        }

        private void FfmpegManager_OutputDataReceived(object sender, DataReceivedEventArgs e) {
            lock (this) {
                string data = e.Data;

                if (!string.IsNullOrEmpty(data)) {
                    Output.AppendLine(data);
                }
            }
        }

        public string GetCodecArgs(VideoCodec codec, int Bitrate, string InputFilePath, string OutputFilePath, TimeSpan? StartTime = null, TimeSpan? EndTime = null) {
            StringBuilder args = new StringBuilder();
            if (StartTime != null && StartTime != TimeSpan.Zero) {
                args.Append($"-ss {StartTime.Value.ToString(@"hh\:mm\:ss")} ");
            }
            args.Append($"-i \"{InputFilePath}\" ");
            switch (codec) {
                case VideoCodec.Unknown:
                    return null;
                case VideoCodec.H264_NVENC:
                    args.Append("-c:v h264_nvenc ");
                    args.Append("-preset p4 ");
                    args.Append("-tune hq ");
                    args.Append("-profile:v high ");
                    args.Append($"-b:v {Bitrate}k ");
                    break;
                case VideoCodec.HEVC_NVENC:
                    args.Append("-c:v hevc_nvenc ");
                    args.Append("-preset p4 ");
                    args.Append("-tune hq ");
                    args.Append("-profile:v main ");
                    args.Append($"-b:v {Bitrate}k ");
                    break;
            }



            if (EndTime != null && EndTime != TimeSpan.Zero) {
                args.Append($"-to {EndTime.Value.ToString(@"hh\:mm\:ss")} ");
            }

            args.Append($"-y ");

            args.Append($"\"{OutputFilePath}\"");
            return args.ToString();
        }
        public Task<bool> StartEncodingAsync(string inputVideo, string outputVideo, VideoCodec codec, int bitrate) {
            return Task.Run(() => StartEncoding(inputVideo, outputVideo, codec, bitrate));
        }
        public Task<bool> StartEncodingAsync(string inputVideo, string outputVideo, VideoCodec codec, int bitrate, TimeSpan StartTime, TimeSpan EndTime) {
            return Task.Run(() => StartEncoding(inputVideo, outputVideo, codec, bitrate, StartTime, EndTime));
        }
        public bool StartEncoding(string inputVideo, string outputVideo, VideoCodec codec, int bitrate, TimeSpan? StartTime = null, TimeSpan? EndTime = null) {
            if (codec == VideoCodec.Unknown) return false;

            bool errorcode = this.Open(this.ffmpegpath, GetCodecArgs(codec, bitrate, inputVideo, outputVideo, StartTime, EndTime)) == 0;
            if (!errorcode) {
                MessageBox.Show(Output.ToString());
            }
            return true;
        }
        public void CloseFFmpeg() {
            if (this.IsProcessRunning) {
                this.Close();
            }
        }
    }
}
