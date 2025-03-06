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
using ClipsOrganizer.ViewableControls.ImageControls;

namespace ClipsOrganizer.ViewableControls {
    public enum SupportedFileTypes {
        Unknown = 0,
        Text,
        Image,
        Video
    }
    public class FileLoadedEventArgs : EventArgs {
        public string FilePath { get; }

        public FileLoadedEventArgs(string filePath) {
            FilePath = filePath;
        }
    }

    public static class ViewableController {
        public static event EventHandler<FileLoadedEventArgs> FileLoaded;
        public static ContentControl MainWindowCC { get; private set; }
        public static ContentControl MainwindowCC_Actions { get; private set; }
        public static VideoViewer VideoViewerInstance;
        public static ImageViewer ImageViewerInstance;
        public static ImageData ImageViewerDataInstance;
        static ViewableController() {
            if (App.Current.MainWindow is MainWindow mainWindow) {
                MainWindowCC = mainWindow.CC_Viewable;
                MainwindowCC_Actions = mainWindow.CC_Actions;
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
                        ImageViewerDataInstance = new ImageData();
                        MainWindowCC.Content = ImageViewerInstance;
                        MainwindowCC_Actions.Content = ImageViewerDataInstance;
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
                    break;
            }
            OnFileLoaded(filePath);
        }
        private static void OnFileLoaded(string filePath) {
            FileLoaded?.Invoke(null, new FileLoadedEventArgs(filePath));
            App.Current.MainWindow.Title = string.Format("ClipsOrganizer / {0}", System.IO.Path.GetFileName(filePath));
        }

        public static void PassKeyStroke(KeyEventArgs e) {
            if (MainWindowCC.Content is VideoViewer)
                ((VideoViewer)MainWindowCC.Content).HandleKeyStroke(e);
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
