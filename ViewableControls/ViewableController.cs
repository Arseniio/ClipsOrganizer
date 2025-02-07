using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace ClipsOrganizer.ViewableControls {
    public enum SupportedFileTypes {
        Unknown = 0,
        Text,
        Image,
        Video
    }
    public static class ViewableController {
        public static ContentControl MainWindowCC { get; private set; }
        public static VideoViewer VideoViewerInstance;
        public static ImageViewer ImageViewerInstance;
        static ViewableController() {
            if (App.Current.MainWindow is MainWindow mainWindow) {
                MainWindowCC = mainWindow.CC_Viewable;
            }
            else {
                throw new InvalidOperationException("MainWindow не инициализирован");
            }
        }
        public static void LoadNewFile(string filePath) {
            SupportedFileTypes FileType = FileTypeDetector.DetectFileType(filePath);
            switch (FileType) {
                case SupportedFileTypes.Unknown:
                    //TODO: Add support to try and open that file in text editor
                    Log.Update("Неизвестный тип файла");
                    break;
                case SupportedFileTypes.Image:
                    if (MainWindowCC.Content is not ImageViewer) {
                        ImageViewerInstance = new ImageViewer();
                        MainWindowCC.Content = ImageViewerInstance;
                    }
                    try {
                        ((ImageViewer)MainWindowCC.Content).LoadImage(filePath);
                    }
                    catch (Exception ex) {
                        Log.Update($"Невозможно загрузить видеофайл {filePath}");
                        Log.Update(ex.Message);
                    }
                    break;
                case SupportedFileTypes.Text:
                    throw new NotImplementedException("Пока что невозможоно открыть текстовый файл");
                    break;
                case SupportedFileTypes.Video:
                    if (MainWindowCC.Content is not VideoViewer) {
                        VideoViewerInstance = new VideoViewer();
                        MainWindowCC.Content = VideoViewerInstance;
                    }
                    try {
                        ((VideoViewer)MainWindowCC.Content).LoadVideoFile(filePath);
                    }
                    catch (Exception ex) {
                        Log.Update($"Невозможно загрузить видеофайл {filePath}");
                        Log.Update(ex.Message);
                    }
                    break;
            }
            UpdateWindowName(filePath);
        }
        public static void PassKeyStroke(KeyEventArgs e) {
            if (MainWindowCC.Content is VideoViewer)
                ((VideoViewer)MainWindowCC.Content).HandleKeyStroke(e);
        }
        private static void UpdateWindowName(string filepath) {
            App.Current.MainWindow.Title = string.Format("ClipsOrganizer / {0}", System.IO.Path.GetFileName(filepath));
        }

        public static class FileTypeDetector {

            public static SupportedFileTypes DetectFileType(string filePath) {
                if (!File.Exists(filePath))
                    throw new FileNotFoundException("The file does not exist.");
                string MimeType = MimeTypes.GetMimeType(filePath);

                if (MimeType.StartsWith("video/"))
                    return SupportedFileTypes.Video;
                if (MimeType.StartsWith("image/") || Path.GetExtension(filePath).Contains("CR2"))
                    return SupportedFileTypes.Image;
                if (MimeType.StartsWith("text/"))
                    return SupportedFileTypes.Text;

                return SupportedFileTypes.Unknown;
            }
        }

    }
}
