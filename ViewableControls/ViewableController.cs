using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.ComponentModel.DataAnnotations;

namespace ClipsOrganizer.ViewableControls {
    public static class ViewableController {
        public static ContentControl MainWindowCC { get; private set; }
        static ViewableController() {
            if (App.Current.MainWindow is MainWindow mainWindow) {
                MainWindowCC = mainWindow.CC_Viewable;
            }
            else {
                throw new InvalidOperationException("MainWindow не инициализирован");
            }
        }

        public static class FileTypeDetector {
            public static string DetectFileType(string filePath) {
                if (!File.Exists(filePath))
                    throw new FileNotFoundException("The file does not exist.");

                string contentType;
                FileExtensionsAttribute fileExtensions = new FileExtensionsAttribute();
                fileExtensions.
                new FileExtensionContentTypeProvider().TryGetContentType(FileName, out contentType);
                return contentType ?? "application/octet-stream";

                if (mimeType.StartsWith("video/"))
                    return "Video";
                if (mimeType.StartsWith("image/"))
                    return "Image";
                if (mimeType.StartsWith("text/"))
                    return "Text";

                return "Unknown";
            }
        }

    }
}
