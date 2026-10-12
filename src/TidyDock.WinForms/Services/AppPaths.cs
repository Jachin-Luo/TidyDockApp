namespace TidyDock.WinForms.Services;

internal sealed class AppPaths
{
    public string Root { get; }
    public string Cache { get; }
    public string Shortcuts { get; }
    public string Icons { get; }
    public string Logs { get; }
    public string Backups { get; }

    public AppPaths(string? root = null)
    {
        Root = root ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TidyDock", "winforms");
        Cache = Path.Combine(Root, "cache");
        Shortcuts = Path.Combine(Root, "shortcuts");
        Icons = Path.Combine(Root, "icons");
        Logs = Path.Combine(Root, "logs");
        Backups = Path.Combine(Root, "backups");
    }

    public string ConfigPath => Path.Combine(Root, "settings.json");
    public string LogPath => Path.Combine(Logs, "tidydock.log");

    public void Ensure()
    {
        Directory.CreateDirectory(Root);
        Directory.CreateDirectory(Cache);
        Directory.CreateDirectory(Shortcuts);
        Directory.CreateDirectory(Icons);
        Directory.CreateDirectory(Logs);
        Directory.CreateDirectory(Backups);
    }
}
