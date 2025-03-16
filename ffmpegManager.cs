using ClipsOrganizer.Profiles;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xabe.FFmpeg;
using Xabe.FFmpeg.Exceptions;

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
    public enum ImageFormat {
        Unknown = 0,
        JPEG,          // .jpg, .jpeg
        PNG,           // .png
        BMP,           // .bmp
        GIF,           // .gif
        TIFF,          // .tiff, .tif
        WEBP,          // .webp
        HEIF,          // .heif, .heic
        AVIF,          // .avif
        RAW,           // Общий RAW-формат
        DNG,           // .dng (Digital Negative)
        CR2,           // .cr2 (Canon RAW)
        NEF,           // .nef (Nikon RAW)
        ARW,           // .arw (Sony RAW)
        ORF,           // .orf (Olympus RAW)
        RW2,           // .rw2 (Panasonic RAW)
        PSD,           // .psd (Photoshop)
        EXR            // .exr (OpenEXR)
    }

    public class FFmpegManager : IDisposable {
        private IConversion _currentConversion;
        private bool _disposed = false;

        public event Action<int> OnEncodeProgressChanged;

        public FFmpegManager(string ffmpegPath) {
            FFmpeg.SetExecutablesPath(System.IO.Path.GetDirectoryName(ffmpegPath));
        }

        public async Task<bool> StartEncodingAsync(
            string inputVideo,
            string outputVideo,
            VideoCodec codec,
            int bitrate,
            TimeSpan? startTime = null,
            TimeSpan? endTime = null) {
            _currentConversion = null;
            try {
                var mediaInfo = await FFmpeg.GetMediaInfo(inputVideo);
                var videoStream = mediaInfo.VideoStreams.FirstOrDefault();
                var audioStream = mediaInfo.AudioStreams.FirstOrDefault();
                TimeSpan duration = endTime.HasValue && startTime.HasValue
                    ? endTime.Value - startTime.Value
                    : mediaInfo.Duration;

                IConversion conversion = FFmpeg.Conversions.New()
                    .SetPreset(ConversionPreset.VeryFast)
                    .SetOutput(outputVideo)
                    .SetOverwriteOutput(true);

                if (startTime.HasValue)
                    conversion.SetSeek(startTime.Value);

                if (endTime.HasValue && startTime.HasValue)
                    conversion.AddParameter($"-to {endTime.Value:hh\\:mm\\:ss\\.fff}");

                ConfigureVideoCodec(conversion, videoStream, codec, bitrate);

                if (audioStream != null)
                    conversion.AddStream(audioStream.SetCodec(AudioCodec.copy));

                conversion.OnProgress += (sender, args) =>
                {
                    double progress = args.Duration.TotalSeconds / duration.TotalSeconds * 100;
                    OnEncodeProgressChanged?.Invoke((int)progress);
                };

                _currentConversion = conversion;
                await conversion.Start();
                return true;
            }
            catch (ConversionException ex) {
                // Логирование ошибки
                return false;
            }
        }

        private void ConfigureVideoCodec(
            IConversion conversion,
            IVideoStream videoStream,
            VideoCodec codec,
            int bitrate) {
            string videoCodec = codec switch
            {
                VideoCodec.H264_NVENC => "h264_nvenc",
                VideoCodec.HEVC_NVENC => "hevc_nvenc",
                VideoCodec.H264_x264 => "libx264",
                VideoCodec.H265_x265 => "libx265",
                _ => throw new NotSupportedException()
            };

            conversion.AddStream(videoStream
                .SetCodec(videoCodec)
                .SetBitrate(bitrate * 1000));

            switch (codec) {
                case VideoCodec.H264_NVENC:
                case VideoCodec.HEVC_NVENC:
                    conversion.AddParameter("-preset p4 -tune hq");
                    break;

                case VideoCodec.H264_x264:
                case VideoCodec.H265_x265:
                    conversion.AddParameter("-preset medium");
                    break;
            }
        }

        public void Dispose() {
            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }
}