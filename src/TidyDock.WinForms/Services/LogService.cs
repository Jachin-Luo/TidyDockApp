namespace TidyDock.WinForms.Services;

internal sealed class LogService
{
    private readonly AppPaths _paths;

    public LogService(AppPaths paths)
    {
        _paths = paths;
    }

    public void Info(string message)
    {
        Write("INFO", message);
    }

    public void Error(Exception exception)
    {
        Write("ERROR", exception.ToString());
    }

    private void Write(string level, string message)
    {
        try
        {
            _paths.Ensure();
            File.AppendAllText(_paths.LogPath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] {message}{Environment.NewLine}");
        }
        catch
        {
        }
    }
}
