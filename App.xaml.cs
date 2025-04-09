using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace ClipsOrganizer {
    /// <summary>
    /// Логика взаимодействия для App.xaml
    /// </summary>
    public partial class App : Application {
        private static readonly string LogFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "application_log.txt");
        protected override void OnStartup(StartupEventArgs e) {
            base.OnStartup(e);
            this.DispatcherUnhandledException += (sender, args) => {
                HandleException(args.Exception);
                args.Handled = true; 
            };
            AppDomain.CurrentDomain.UnhandledException += (sender, args) => {
                if (args.ExceptionObject is Exception ex) {
                    HandleException(ex);
                }
            };
            TaskScheduler.UnobservedTaskException += (sender, args) => {
                HandleException(args.Exception);
                args.SetObserved(); 
            };
        }

        private static void HandleException(Exception ex) {
            try {
                Log.Update($"[{DateTime.Now}] Unhandled Exception: {ex.Message}");
                Log.Update($"Stack Trace: {ex.StackTrace}");
                SaveLogToFile();
            }
            catch {
                // В крайнем случае игнорируем ошибки логирования
            }
        }

        private static void SaveLogToFile() {
            try {
                File.AppendAllText(LogFilePath, Log.Instance.AllLogInfo.ToString());
            }
            catch {
                // Игнорируем ошибки записи в лог
            }
        }
    }
}
