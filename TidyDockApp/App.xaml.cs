using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace TidyDock
{
    public partial class App : Application
    {
        private Mutex _singleInstanceMutex;
        private bool _ownsSingleInstanceMutex;
        private TrayService _trayService;
        private ErrorLogService _errorLog;

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            bool createdNew;
            _singleInstanceMutex = new Mutex(true, "TidyDock.SingleInstance.40799", out createdNew);
            if (!createdNew)
            {
                Shutdown();
                return;
            }
            _ownsSingleInstanceMutex = true;

            var settings = new SettingsService();
            _errorLog = new ErrorLogService(settings);
            DispatcherUnhandledException += OnDispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

            var config = settings.Load();
            var window = new MainWindow(settings, config);
            MainWindow = window;
            _trayService = new TrayService(window, config, config.Dock.ShowTrayIcon);
            window.TrayVisibilitySetter = delegate(bool visible)
            {
                _trayService.SetVisible(visible);
            };
            window.TrayTextRefresher = delegate
            {
                _trayService.RefreshText();
            };
            if (config.Dock.StartVisible)
            {
                window.Show();
            }

            ScheduleInitialMemoryTrim();
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            if (_trayService != null)
            {
                _trayService.Dispose();
            }

            if (_singleInstanceMutex != null && _ownsSingleInstanceMutex)
            {
                _singleInstanceMutex.ReleaseMutex();
                _singleInstanceMutex.Dispose();
            }
        }

        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            if (_errorLog != null)
            {
                _errorLog.Write(e.Exception);
            }

            MessageBox.Show(
                "TidyDock encountered an error. Details were written to the local log folder.",
                "TidyDock",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            e.Handled = true;
        }

        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (_errorLog != null)
            {
                _errorLog.Write(e.ExceptionObject as Exception);
            }
        }

        private void OnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            if (_errorLog != null)
            {
                _errorLog.Write(e.Exception);
            }

            e.SetObserved();
        }

        private void ScheduleInitialMemoryTrim()
        {
            Dispatcher.BeginInvoke(new Action(delegate
            {
                try
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    GC.Collect();
                    SetProcessWorkingSetSize(Process.GetCurrentProcess().Handle, new IntPtr(-1), new IntPtr(-1));
                }
                catch
                {
                }
            }), DispatcherPriority.ApplicationIdle);
        }

        [DllImport("kernel32.dll")]
        private static extern bool SetProcessWorkingSetSize(IntPtr process, IntPtr minimumWorkingSetSize, IntPtr maximumWorkingSetSize);
    }
}
