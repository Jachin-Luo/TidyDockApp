using System;
using System.IO;

namespace TidyDock
{
    public class ErrorLogService
    {
        private readonly string _logDirectory;
        private readonly string _logPath;

        public ErrorLogService(SettingsService settings)
        {
            _logDirectory = Path.Combine(settings.AppDirectory, "logs");
            _logPath = Path.Combine(_logDirectory, "error.log");
        }

        public void Write(Exception exception)
        {
            try
            {
                Directory.CreateDirectory(_logDirectory);
                var message = exception == null ? "Unknown exception" : exception.ToString();
                File.AppendAllText(
                    _logPath,
                    "=== " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " ===" + Environment.NewLine +
                    message + Environment.NewLine + Environment.NewLine);
            }
            catch
            {
            }
        }
    }
}
