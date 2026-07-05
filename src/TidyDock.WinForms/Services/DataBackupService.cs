using System.IO.Compression;
using TidyDock.WinForms.Core;

namespace TidyDock.WinForms.Services;

internal sealed class DataBackupService
{
    private const int MaxAutomaticBackups = 5;
    private readonly AppPaths _paths;
    private readonly LogService _log;

    public DataBackupService(AppPaths paths, LogService log)
    {
        _paths = paths;
        _log = log;
    }

    public string DataDirectory => _paths.Root;

    public void ExportBackup(string zipPath, AppConfig config)
    {
        _paths.Ensure();
        CreateBackup(zipPath, config);
    }

    public string? GetLatestBackup()
    {
        _paths.Ensure();
        return Directory
            .EnumerateFiles(_paths.Backups, "*.zip", SearchOption.TopDirectoryOnly)
            .Select(path => new FileInfo(path))
            .OrderByDescending(file => file.CreationTimeUtc)
            .ThenByDescending(file => file.LastWriteTimeUtc)
            .FirstOrDefault()
            ?.FullName;
    }

    public void CreateAutomaticBackup()
    {
        try
        {
            if (!File.Exists(_paths.ConfigPath))
            {
                return;
            }

            _paths.Ensure();
            var path = Path.Combine(_paths.Backups, $"tidydock-auto-{DateTime.Now:yyyyMMdd-HHmmss}.zip");
            CreateBackup(path, config: null);
            PruneAutomaticBackups();
        }
        catch (Exception ex)
        {
            _log.Error(ex);
        }
    }

    public AppConfig ReadConfigFromBackup(string zipPath)
    {
        using var archive = ZipFile.OpenRead(zipPath);
        var entry = archive.Entries.FirstOrDefault(entry =>
            string.Equals(entry.Name, "settings.json", StringComparison.OrdinalIgnoreCase));
        if (entry is null)
        {
            throw new InvalidOperationException("备份文件中没有 settings.json。");
        }

        using var stream = entry.Open();
        using var reader = new StreamReader(stream);
        var config = ConfigService.Deserialize(reader.ReadToEnd());
        _ = new ConfigService(_paths, _log).Normalize(config);
        return config;
    }

    public void ImportResources(string zipPath)
    {
        _paths.Ensure();
        using var archive = ZipFile.OpenRead(zipPath);
        foreach (var entry in archive.Entries)
        {
            if (string.IsNullOrWhiteSpace(entry.Name))
            {
                continue;
            }

            var normalized = entry.FullName.Replace('\\', '/');
            var targetRoot = normalized.StartsWith("icons/", StringComparison.OrdinalIgnoreCase)
                ? _paths.Icons
                : normalized.StartsWith("shortcuts/", StringComparison.OrdinalIgnoreCase)
                    ? _paths.Shortcuts
                    : "";
            if (string.IsNullOrWhiteSpace(targetRoot))
            {
                continue;
            }

            var destination = Path.Combine(targetRoot, entry.Name);
            var resolvedTargetRoot = Path.GetFullPath(targetRoot).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            var resolvedDestination = Path.GetFullPath(destination);
            if (!resolvedDestination.StartsWith(resolvedTargetRoot, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("备份文件包含不安全路径。");
            }

            entry.ExtractToFile(resolvedDestination, overwrite: true);
        }
    }

    private void CreateBackup(string zipPath, AppConfig? config)
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "TidyDockBackup-" + Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(tempRoot);
            if (config is null)
            {
                File.Copy(_paths.ConfigPath, Path.Combine(tempRoot, "settings.json"), overwrite: true);
            }
            else
            {
                File.WriteAllText(Path.Combine(tempRoot, "settings.json"), ConfigService.Serialize(config));
            }

            CopyDirectoryIfExists(_paths.Icons, Path.Combine(tempRoot, "icons"));
            CopyDirectoryIfExists(_paths.Shortcuts, Path.Combine(tempRoot, "shortcuts"));

            var targetDirectory = Path.GetDirectoryName(zipPath);
            if (!string.IsNullOrWhiteSpace(targetDirectory))
            {
                Directory.CreateDirectory(targetDirectory);
            }
            if (File.Exists(zipPath))
            {
                File.Delete(zipPath);
            }
            ZipFile.CreateFromDirectory(tempRoot, zipPath, CompressionLevel.Optimal, includeBaseDirectory: false);
        }
        finally
        {
            if (Directory.Exists(tempRoot))
            {
                Directory.Delete(tempRoot, recursive: true);
            }
        }
    }

    private void PruneAutomaticBackups()
    {
        var backups = Directory
            .EnumerateFiles(_paths.Backups, "tidydock-auto-*.zip", SearchOption.TopDirectoryOnly)
            .Select(path => new FileInfo(path))
            .OrderByDescending(file => file.CreationTimeUtc)
            .ThenByDescending(file => file.LastWriteTimeUtc)
            .Skip(MaxAutomaticBackups);
        foreach (var backup in backups)
        {
            backup.Delete();
        }
    }

    private static void CopyDirectoryIfExists(string source, string destination)
    {
        if (!Directory.Exists(source))
        {
            return;
        }

        foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(source, file);
            var target = Path.Combine(destination, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target, overwrite: true);
        }
    }
}
