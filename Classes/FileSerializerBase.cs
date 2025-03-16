using Newtonsoft.Json;
using System.IO;
using System;

namespace ClipsOrganizer.FileUtils {
    public static class FileSerializer {
        private static bool WriteFile<T>(T obj, string Filepath) where T : class {
            string contents = JsonConvert.SerializeObject(obj);
            File.WriteAllText(Filepath, contents);
            return false;
        }
        public static T ReadFile<T>(string Filepath) where T : class {
            if (!File.Exists(Filepath)) {
                using (var fileStream = File.Create(Filepath)) { }
                return null;
            }

            try {
                var lines = File.ReadAllText(Filepath);
                if (string.IsNullOrWhiteSpace(lines)) return null;
                T file = JsonConvert.DeserializeObject<T>(lines);
                return file;
            }
            catch (Exception ex) {
                Log.Update($"Ошибка при чтении файла: {ex.Message}");
                return null;
            }
        }

        public static bool CheckIfChanged<T>(T Obj, string Filepath) where T : class {
            var lines = File.ReadAllText(Filepath);
            T Oldfile = JsonConvert.DeserializeObject<T>(lines);
            bool changed = !Obj.Equals(Oldfile);
            return changed;
        }
        private static object objLock = new object();
        public static void WriteAndCreateBackupFile<T>(T data, string Filepath) where T : class {
            lock (objLock) {
                FileInfo file = new FileInfo(Filepath);
                string directory = Path.GetDirectoryName(Filepath);
                string oldFilename = $"{Path.GetFileNameWithoutExtension(Filepath)}_bkp{Path.GetExtension(Filepath)}";
                string backupFilePath = Path.Combine(directory, oldFilename);
                if (File.Exists(backupFilePath)) {
                    File.Delete(backupFilePath);
                }
                if (File.Exists(Filepath)) {
                    File.Move(Filepath, backupFilePath);
                }
                WriteFile(data, Filepath);
            }
        }
    }
}