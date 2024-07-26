using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ClipsOrganizer.FileUtils {
    internal class FileUtils {
        [StructLayout(LayoutKind.Sequential)]
        public struct BY_HANDLE_FILE_INFORMATION {
            public uint FileAttributes;
            public System.Runtime.InteropServices.ComTypes.FILETIME CreationTime;
            public System.Runtime.InteropServices.ComTypes.FILETIME LastAccessTime;
            public System.Runtime.InteropServices.ComTypes.FILETIME LastWriteTime;
            public uint VolumeSerialNumber;
            public uint FileSizeHigh;
            public uint FileSizeLow;
            public uint NumberOfLinks;
            public uint FileIndexHigh;
            public uint FileIndexLow;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool GetFileInformationByHandle(IntPtr hFile, out BY_HANDLE_FILE_INFORMATION lpFileInformation);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern IntPtr CreateFile(
            string lpFileName,
            uint dwDesiredAccess,
            uint dwShareMode,
            IntPtr lpSecurityAttributes,
            uint dwCreationDisposition,
            uint dwFlagsAndAttributes,
            IntPtr hTemplateFile);
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool CloseHandle(IntPtr hObject);

        const uint GENERIC_READ = 0x80000000;
        const uint OPEN_EXISTING = 3;
        const uint FILE_ATTRIBUTE_NORMAL = 0x80;

        private static BY_HANDLE_FILE_INFORMATION? GetFileInformation(string filePath) {
            IntPtr hFile = CreateFile(filePath, GENERIC_READ, 0, IntPtr.Zero, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, IntPtr.Zero);
            if (hFile == IntPtr.Zero) {
                throw new IOException("Unable to open file.", Marshal.GetLastWin32Error());
            }
            if(hFile.ToInt64() == -1) {
                return null;
                throw new IOException("Unable to open file.", Marshal.GetLastWin32Error());
            }
            BY_HANDLE_FILE_INFORMATION fileInfo;
            if (!GetFileInformationByHandle(hFile, out fileInfo)) {
                CloseHandle(hFile);
                throw new IOException("Unable to get file information.", Marshal.GetLastWin32Error());
            }

            CloseHandle(hFile);
            return fileInfo;
        }

        public BY_HANDLE_FILE_INFORMATION? GetFileinfo(string filePath) {
            return GetFileInformation(filePath);
        }
    }
}
