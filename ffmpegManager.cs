using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ClipsOrganizer {
    public enum VideoCodec {
        Unknown = 0,
        H264_NVENC,
        HEVC_NVENC,
        H264_x264,
        H265_x265,
        NE_H264_AMF,
        NE_HEVC_AMF,
        NE_H264_QuickSync,
        NE_HEVC_QuickSync,
        NE_VP8,
        NE_VP9,
        NE_AV1,
        NE_MPEG4_Xvid,
        NE_GIF,
        NE_WebP,
        NE_APNG
    }

    public class ffmpegManager : ExternalProcessManager {

        private bool _disposed = false;
        private bool isReady = false;

        private TimeSpan _VideoDuration = TimeSpan.Zero;

        public StringBuilder Output { get; private set; }

        private string ffmpegpath;
        private float EncodePercentage;

        public event Action<int> OnEncodeProgressChanged;

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

                //  Duration: 00:00:15.32, start: 0.000000, bitrate: 1095 kb/s
                if (_VideoDuration == TimeSpan.Zero) {
                    Match match = Regex.Match(data, @"Duration:\s*(\d+:\d+:\d+\.\d+),\s*start:", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
                    if (match.Success && TimeSpan.TryParse(match.Groups[1].Value, out TimeSpan duration)) {
                        _VideoDuration = duration;
                    }
                }


                Match TimeMatch = Regex.Match(data, @"time=\s*(\d+:\d+:\d+\.\d+)\s*bitrate=", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
                if (TimeSpan.TryParse(TimeMatch.Groups[1].Value, out TimeSpan time)) {
                    OnEncodeProgressChanged?.Invoke((int)(((float)time.Ticks / _VideoDuration.Ticks) * 100));
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
                case VideoCodec.H264_x264:
                    args.Append("-c:v libx264 ");
                    args.Append("-preset medium ");
                    args.Append($"-b:v {Bitrate}k ");
                    break;
                case VideoCodec.H265_x265:
                    args.Append("-c:v libx265 ");
                    args.Append("-preset medium ");
                    args.Append($"-b:v {Bitrate}k ");
                    break;
                default:
                    throw new NotImplementedException(nameof(codec));
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
                OnEncodeProgressChanged?.Invoke(0);
                return false;
            }
            OnEncodeProgressChanged?.Invoke(100);

            return true;
        }
        public void CloseFFmpeg() {
            if (this.IsProcessRunning) {
                this.Close();
            }
        }
    }
}
