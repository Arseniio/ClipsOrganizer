﻿using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Diagnostics;

namespace ClipsOrganizer {
    /// <summary>
    /// Логика взаимодействия для App.xaml
    /// </summary>
    public partial class App : Application {
        private static readonly string LogFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "application_log.txt");
        
        protected override void OnStartup(StartupEventArgs e) {
            base.OnStartup(e);
            
            // Обработка необработанных исключений
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

            // Обработка завершения приложения
            this.Exit += (sender, args) => {
                try {
                    // Очистка ресурсов
                    foreach (Window window in Windows) {
                        window.Close();
                    }
                    
                    // Принудительное завершение всех процессов приложения
                    Process currentProcess = Process.GetCurrentProcess();
                    foreach (Process process in Process.GetProcessesByName(currentProcess.ProcessName)) {
                        if (process.Id != currentProcess.Id) {
                            try {
                                process.Kill();
                            }
                            catch { /* Игнорируем ошибки при завершении процессов */ }
                        }
                    }
                }
                catch (Exception ex) {
                    HandleException(ex);
                }
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
                // Используем блокировку для предотвращения конфликтов при записи
                lock (Log.Instance) {
                    File.AppendAllText(LogFilePath, Log.Instance.AllLogInfo.ToString());
                }
            }
            catch {
                // Игнорируем ошибки записи в лог
            }
        }

        protected override void OnExit(ExitEventArgs e) {
            try {
                // Очистка ресурсов перед выходом
                foreach (Window window in Windows) {
                    window.Close();
                }
                
                // Сохраняем лог перед выходом
                SaveLogToFile();
            }
            catch (Exception ex) {
                HandleException(ex);
            }
            
            base.OnExit(e);
        }
    }
}
