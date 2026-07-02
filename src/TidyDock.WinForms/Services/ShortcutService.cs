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
        if (!IsShortcutFile(path))
        {
            return path;
        }

        _paths.Ensure();
        var safeName = string.Join("_", Path.GetFileName(path).Split(Path.GetInvalidFileNameChars()));
        var target = Path.Combine(_paths.Shortcuts, $"{Guid.NewGuid():N}-{safeName}");
        File.Copy(path, target, true);
        return target;
    }

    public static bool IsShortcutFile(string path)
    {
        if (!File.Exists(path))
        {
            return false;
        }

        return Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".lnk" or ".url" => true,
            _ => false
        };
    }

    public static string ResolveShortcutTarget(string path)
    {
        return IconService.ResolveShortcutTarget(path);
    }
}
