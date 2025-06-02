using ClipsOrganizer.Classes;
using ClipsOrganizer.Model;
using ClipsOrganizer.Profiles;
using ClipsOrganizer.Settings;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xaml;

using Windows.Media.Core;

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

        public event Action<int> OnEncodeProgressChanged;
        public FFmpegManager(string ffmpegPath) {
            FFmpeg.SetExecutablesPath(ffmpegPath);
        }
        private static readonly Dictionary<string, IMediaInfo> _mediaInfoCache = new();

        public static async Task<IMediaInfo> GetMediaInfoCachedAsync(string path) {
            if (_mediaInfoCache.TryGetValue(path, out var cached))
                return cached;

            var info = await FFmpeg.GetMediaInfo(path);
            _mediaInfoCache[path] = info;
            return info;
        }

        public static async Task<double> CalculateVideoSizeAsync(
            string path,
            double? overrideVideoBitrate = null,
            TimeSpan? overrideDuration = null,
            double? overrideAudioBitrate = null) {
            var mediaInfo = await GetMediaInfoCachedAsync(path);

            var videoStream = mediaInfo.VideoStreams.FirstOrDefault();
            var audioStream = mediaInfo.AudioStreams.FirstOrDefault();

            double videoBitrate = overrideVideoBitrate ?? videoStream?.Bitrate ?? 0;
            double audioBitrate = (overrideAudioBitrate ?? audioStream?.Bitrate / 1000.0) ?? 0;
            double durationSec = overrideDuration?.TotalSeconds ?? mediaInfo.Duration.TotalSeconds;

            double totalBitrate = videoBitrate + audioBitrate;

            double fileSizeBytes = (totalBitrate * 1000 * durationSec) / 8;
            return fileSizeBytes / (1024 * 1024);
        }

        public static void ClearCache(string? path = null) {
            if (path == null)
                _mediaInfoCache.Clear();
            else
                _mediaInfoCache.Remove(path);
        }


        public async Task<bool> StartEncodingAsync(
                string inputVideo,
                string outputVideo,
                VideoCodec codec,
                int bitrate,
                TimeSpan? startTime = null,
                TimeSpan? endTime = null) {
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
                    conversion.AddStream(audioStream.SetCodec(Xabe.FFmpeg.AudioCodec.copy));

                conversion.OnProgress += (sender, args) =>
                {
                    double progress = args.Duration.TotalSeconds / duration.TotalSeconds * 100;
                    OnEncodeProgressChanged?.Invoke((int)progress);
                };

                await conversion.Start();
                return true;
            }
            catch (ConversionException ex) {
                Log.Update(ex.Message.ToString());
                return false;
            }
        }
        public async Task<bool> StartEncodingAsync(ExportFileInfoVideo infoVideo, string outputVideo, int bitrate, CancellationToken cancellationToken, int MultiThread = 0) {
            try {
                var mediaInfo = await FFmpeg.GetMediaInfo(infoVideo.Path);
                var videoStream = mediaInfo.VideoStreams.FirstOrDefault();
                var audioStream = mediaInfo.AudioStreams.FirstOrDefault();
                TimeSpan duration = infoVideo.TrimStart != TimeSpan.Zero && infoVideo.TrimEnd != TimeSpan.Zero
                    ? infoVideo.TrimEnd - infoVideo.TrimStart
                    : mediaInfo.Duration;
                IConversion conversion = FFmpeg.Conversions.New()
                    .SetPreset(ConversionPreset.VeryFast)
                    .SetOutput(outputVideo)
                    .SetOverwriteOutput(true);
                if (MultiThread == 0) {
                    MultiThread = GlobalSettings.Instance.ExportSettings.MaxFFmpegThreads;
                }
                if (GlobalSettings.Instance.ExportSettings.UseAllThreads) {
                    conversion.UseMultiThread(true);
                }
                else if (MultiThread > 0) {
                    conversion.UseMultiThread(MultiThread);
                }

                if (infoVideo.TrimStart != TimeSpan.Zero)
                    conversion.SetSeek(infoVideo.TrimStart);

                if (infoVideo.TrimEnd != TimeSpan.Zero && infoVideo.TrimStart != TimeSpan.Zero)
                    conversion.AddParameter($"-to {infoVideo.TrimEnd:hh\\:mm\\:ss\\.fff}");

                ConfigureVideoCodec(conversion, videoStream, infoVideo.VideoCodec, bitrate);
                if (infoVideo.FrameRate.HasValue) {
                    conversion.SetFrameRate(infoVideo.FrameRate.Value);
                }

                if (audioStream != null)
                    conversion.AddStream(audioStream.SetCodec(infoVideo.AudioCodec switch
                    {
                        Model.AudioCodec.AAC => Xabe.FFmpeg.AudioCodec.aac,
                        Model.AudioCodec.MP3 => Xabe.FFmpeg.AudioCodec.mp3,
                        Model.AudioCodec.Opus => Xabe.FFmpeg.AudioCodec.opus,
                        Model.AudioCodec.FLAC => Xabe.FFmpeg.AudioCodec.flac,
                        _ => Xabe.FFmpeg.AudioCodec.aac
                    }));

                conversion.OnProgress += (sender, args) =>
                {
                    double progress = args.Duration.TotalSeconds / duration.TotalSeconds * 100;
                    OnEncodeProgressChanged?.Invoke((int)progress);
                };

                await conversion.Start(cancellationToken: cancellationToken);
                return true;
            }
            catch (ConversionException ex) {
                Log.Update(ex.Message.ToString());
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
            //TODO: maybe change later
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
            GC.SuppressFinalize(this);
        }

        public async Task<bool> StartAudioEncodingAsync(
            ExportFileInfoAudio audioInfo,
            string outputPath,
            CancellationToken cancellationToken) {
            try {
                var mediaInfo = await FFmpeg.GetMediaInfo(audioInfo.Path);
                var audioStream = mediaInfo.AudioStreams.FirstOrDefault();
                
                if (audioInfo.TrimStart >= audioInfo.TrimEnd && audioInfo.TrimEnd > TimeSpan.Zero) {
                    Log.Update("Ошибка: время начала обрезки должно быть меньше времени конца обрезки");
                    return false;
                }

                TimeSpan duration = audioInfo.TrimStart != TimeSpan.Zero && audioInfo.TrimEnd != TimeSpan.Zero
                    ? audioInfo.TrimEnd - audioInfo.TrimStart
                    : mediaInfo.Duration;

                IConversion conversion = FFmpeg.Conversions.New()
                    .SetPreset(ConversionPreset.VeryFast)
                    .SetOutput(outputPath)
                    .SetOverwriteOutput(true);

                if (audioInfo.TrimStart > TimeSpan.Zero) {
                    conversion.SetSeek(audioInfo.TrimStart);
                }

                if (audioInfo.TrimEnd > TimeSpan.Zero) {
                    conversion.AddParameter($"-to {audioInfo.TrimEnd:hh\\:mm\\:ss\\.fff}");
                }

                if (audioStream != null) {
                    var stream = audioStream;
                    
                    switch (audioInfo.outputFormat) {
                        case ExportAudioFormat.mp3:
                            stream = stream.SetCodec(Xabe.FFmpeg.AudioCodec.mp3)
                                         .SetBitrate(audioInfo.AudioBitrate * 1000);
                            break;
                        case ExportAudioFormat.wav:
                            stream = stream.SetCodec(Xabe.FFmpeg.AudioCodec.pcm_s16le);
                            break;
                    }

                    stream = stream.SetSampleRate(audioInfo.AudioSampleRate)
                                 .SetChannels(audioInfo.AudioChannels);

                    conversion.AddStream(stream);

                    if (audioInfo.NormalizeAudio) {
                        conversion.AddParameter("-af loudnorm=I=-16:TP=-1.5:LRA=11");
                    }
                }

                conversion.OnProgress += (sender, args) =>
                {
                    double progress = args.Duration.TotalSeconds / duration.TotalSeconds * 100;
                    OnEncodeProgressChanged?.Invoke((int)progress);
                };

                await conversion.Start(cancellationToken: cancellationToken);
                return true;
            }
            catch (ConversionException ex) {
                Log.Update(ex.Message.ToString());
                return false;
            }
        }
    }
}