using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClipsOrganizer {
    public enum VideoCodec {
        Unknown = 0,
        H264_x264,
        H264_NVENC,
        H265_x265,
        HEVC_NVENC,
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

    public class ffmpegManager {

        private bool _disposed = false;
        private bool isReady = false;

        private string ffmpegpath;

        public ffmpegManager(string affmpegpath) {
            this.ffmpegpath = affmpegpath;
            if (!File.Exists(ffmpegpath)) {
                throw new FileNotFoundException();
            }

        }
        public bool ConvertVideo(string inputVideo, string outputVideo, VideoCodec codec) {
            return false;
        }


    }
}
