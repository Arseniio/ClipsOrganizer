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

        public async Task<string> GetAudioParams() {
            var sb = new StringBuilder();
            sb.AppendLine("🎵 Параметры аудио файла");
            sb.AppendLine($"   • Формат: {outputFormat}");
            sb.AppendLine($"   • Битрейт: {AudioBitrate} kbps");
            sb.AppendLine($"   • Частота дискретизации: {AudioSampleRate} Hz");
            sb.AppendLine($"   • Каналы: {AudioChannels}");
            sb.AppendLine($"   • Нормализация: {(NormalizeAudio ? "Включена" : "Выключена")}");
            
            if (TrimStart != TimeSpan.Zero || TrimEnd != TimeSpan.Zero) {
                sb.AppendLine("🎧 Обрезка аудио");
                sb.AppendLine($"   • Начало: {TrimStart:hh\\:mm\\:ss\\.fff}");
                sb.AppendLine($"   • Конец: {TrimEnd:hh\\:mm\\:ss\\.fff}");
                sb.AppendLine($"   • Длительность: {(TrimEnd - TrimStart):hh\\:mm\\:ss\\.fff}");
            }

            return sb.ToString();
        }
    }
    public enum ExportAudioFormat {
        mp3,
        wav
    }
}