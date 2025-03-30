using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClipsOrganizer.Classes
{
    public static class FfmpegHelper {
        public static async Task<(bool ffmpegExists, bool ffprobeExists)> CheckSystemFfmpeg() {
            try {
                var ffmpeg = await CheckToolExists("ffmpeg");
                var ffprobe = await CheckToolExists("ffprobe");
                return (ffmpeg, ffprobe);
            }
            catch {
                return (false, false);
            }
        }

        public static async Task<(bool ffmpegExists, bool ffprobeExists)> CheckCustomFfmpeg(string directory) {
            var ffmpegPath = Path.Combine(directory, "ffmpeg.exe");
            var ffprobePath = Path.Combine(directory, "ffprobe.exe");

            return (File.Exists(ffmpegPath), File.Exists(ffprobePath));
        }

        private static async Task<bool> CheckToolExists(string toolName) {
            try {
                using var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = toolName,
                        Arguments = "-version",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    }
                };

                process.Start();
                await process.WaitForExitAsync();
                return process.ExitCode == 0;
            }
            catch {
                return false;
            }
        }
    }
}
