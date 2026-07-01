namespace TidyDock.WinForms.Services;

internal sealed class ShortcutService
{
    private readonly AppPaths _paths;

    public ShortcutService(AppPaths paths)
    {
        _paths = paths;
    }

    public string ImportIfShortcut(string path)
    {
        if (!File.Exists(path) || !string.Equals(Path.GetExtension(path), ".lnk", StringComparison.OrdinalIgnoreCase))
        {
            return path;
        }

        _paths.Ensure();
        var safeName = string.Join("_", Path.GetFileName(path).Split(Path.GetInvalidFileNameChars()));
        var target = Path.Combine(_paths.Shortcuts, $"{Guid.NewGuid():N}-{safeName}");
        File.Copy(path, target, true);
        return target;
    }
}
