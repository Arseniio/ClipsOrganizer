using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ClipsOrganizer {
    public class ExternalProcessManager : IDisposable {
        public event DataReceivedEventHandler OutputDataReceived;
        public event DataReceivedEventHandler ErrorDataReceived;
        public bool IsProcessRunning { get; private set; }
        protected Process _process;

        public int Open(string path, string args = null) {
            if (!File.Exists(path)) return 0;
            _process = new Process();
            ProcessStartInfo psi = new ProcessStartInfo()
            {
                FileName = path,
                WorkingDirectory = Path.GetDirectoryName(path),
                Arguments = args,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };
            _process.EnableRaisingEvents = true;
            if (psi.RedirectStandardOutput) _process.OutputDataReceived += cli_OutputDataReceived;
            if (psi.RedirectStandardError) _process.ErrorDataReceived += cli_ErrorDataReceived;
            _process.StartInfo = psi;
            _process.Start();
            if (psi.RedirectStandardError) _process.BeginErrorReadLine();
            if (psi.RedirectStandardOutput) _process.BeginOutputReadLine();
            try {
                IsProcessRunning = true;
                _process.WaitForExit();
            }
            finally {
                IsProcessRunning = false;
            }
            return _process.ExitCode;
        }

        private void cli_OutputDataReceived(object sender, DataReceivedEventArgs e) {
            if (e.Data != null) {
                OutputDataReceived?.Invoke(sender, e);
            }
        }

        private void cli_ErrorDataReceived(object sender, DataReceivedEventArgs e) {
            if (e.Data != null) {
                ErrorDataReceived?.Invoke(sender, e);
            }
        }

        public void WriteInput(string input) {
            if (IsProcessRunning && _process != null && _process.StartInfo != null && _process.StartInfo.RedirectStandardInput) {
                _process.StandardInput.WriteLine(input);
            }
        }
        public virtual void Close() {
            if (IsProcessRunning && _process != null) {
                _process.CloseMainWindow();
            }
        }


        public void Dispose() {
            _process.Dispose();
        }
    };
}

