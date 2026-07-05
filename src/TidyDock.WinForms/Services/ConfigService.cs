using System.Text.Json;
using TidyDock.WinForms.Core;

namespace TidyDock.WinForms.Services;

internal sealed class ConfigService
{
    private readonly AppPaths _paths;
    private readonly LogService _log;
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    public ConfigService(AppPaths paths, LogService log)
    {
        _paths = paths;
        _log = log;
    }

    public string AppDirectory => _paths.Root;

    public AppConfig Load()
    {
        try
        {
            if (!File.Exists(_paths.ConfigPath))
            {
                var created = CreateDefault();
                Save(created);
                return created;
            }

            var json = File.ReadAllText(_paths.ConfigPath);
            var config = JsonSerializer.Deserialize<AppConfig>(json, Options) ?? CreateDefault();
            var changed = Normalize(config);
            if (changed)
            {
                Save(config);
            }
            return config;
        }
        catch (Exception ex)
        {
            _log.Error(ex);
            return RecoverFromLoadFailure(ex);
        }
    }

    public void Save(AppConfig config)
    {
        _ = Normalize(config);
        _paths.Ensure();
        var temp = _paths.ConfigPath + ".tmp";
        var backup = _paths.ConfigPath + ".bak";
        File.WriteAllText(temp, JsonSerializer.Serialize(config, Options));
        if (File.Exists(_paths.ConfigPath))
        {
            File.Copy(_paths.ConfigPath, backup, true);
        }
        File.Move(temp, _paths.ConfigPath, true);
    }

    public static AppConfig Clone(AppConfig config)
    {
        var json = Serialize(config);
        return Deserialize(json);
    }

    public static bool AreEquivalent(AppConfig left, AppConfig right)
    {
        return string.Equals(Serialize(left), Serialize(right), StringComparison.Ordinal);
    }

    public static string Serialize(AppConfig config)
    {
        return JsonSerializer.Serialize(config, Options);
    }

    public static AppConfig Deserialize(string json)
    {
        return JsonSerializer.Deserialize<AppConfig>(json, Options) ?? new AppConfig();
    }

    public static void CopyInto(AppConfig target, AppConfig source)
    {
        var clone = Clone(source);
        target.ConfigVersion = clone.ConfigVersion;
        target.Dock = clone.Dock;
        target.FolderPanel = clone.FolderPanel;
        target.Items = clone.Items;
    }

    private static AppConfig CreateDefault()
    {
        return new AppConfig
        {
            Items =
            [
                new DockItem
                {
                    Type = DockItemTypes.Settings,
                    Name = "\u8bbe\u7f6e",
                    Target = "settings"
                }
            ]
        };
    }

    public bool Normalize(AppConfig config)
    {
        var changed = false;
        config.ConfigVersion = 1;
        config.Dock ??= new DockSettings();
        config.FolderPanel ??= new FolderPanelSettings();
        config.Items ??= [];

        config.Dock.IconSize = Math.Clamp(config.Dock.IconSize, 32, 96);
        config.Dock.IconGap = Math.Clamp(config.Dock.IconGap, 0, 32);
        config.Dock.CornerRadius = Math.Clamp(config.Dock.CornerRadius, 6, 40);
        config.Dock.Opacity = Math.Clamp(config.Dock.Opacity, 0, 1);
        config.Dock.Magnification = Math.Clamp(config.Dock.Magnification, 1, 1.8);
        config.FolderPanel.MaxItems = Math.Clamp(config.FolderPanel.MaxItems, 20, 1000);
        config.FolderPanel.MaxHeight = Math.Clamp(config.FolderPanel.MaxHeight, 240, 760);
        if (string.IsNullOrWhiteSpace(config.FolderPanel.ViewMode))
        {
            config.FolderPanel.ViewMode = "grid";
        }
        if (string.IsNullOrWhiteSpace(config.Dock.Position))
        {
            config.Dock.Position = "bottom";
        }
        if (string.IsNullOrWhiteSpace(config.Dock.Theme))
        {
            config.Dock.Theme = "system";
        }
        if (string.IsNullOrWhiteSpace(config.Dock.Language))
        {
            config.Dock.Language = "zh-CN";
        }

        foreach (var item in config.Items)
        {
            if (string.IsNullOrWhiteSpace(item.Id))
            {
                item.Id = Guid.NewGuid().ToString("N");
                changed = true;
            }
            item.Type = string.IsNullOrWhiteSpace(item.Type) ? DockItemTypes.File : item.Type;
            item.Name = string.IsNullOrWhiteSpace(item.Name) ? DefaultName(item.Type) : item.Name;
            item.Target ??= "";
            item.IconPath ??= "";
            item.IconStyle = NormalizeIconStyle(item.IconStyle);
            item.OpenBehavior = NormalizeOpenBehavior(item.OpenBehavior);
            if (NormalizeFolderGroups(item))
            {
                changed = true;
            }
            if (string.Equals(item.Type, DockItemTypes.File, StringComparison.OrdinalIgnoreCase)
                && File.Exists(item.Target)
                && string.Equals(Path.GetExtension(item.Target), ".lnk", StringComparison.OrdinalIgnoreCase))
            {
                var shortcutTarget = ShortcutService.ResolveShortcutTarget(item.Target);
                if (Directory.Exists(shortcutTarget))
                {
                    item.Type = DockItemTypes.Folder;
                    item.Target = shortcutTarget;
                    changed = true;
                    if (item.OpenBehavior == DockOpenBehaviors.Auto)
                    {
                        item.OpenBehavior = DockOpenBehaviors.Flyout;
                        changed = true;
                    }
                }
            }
            if (ShouldImportShortcut(item.Target))
            {
                item.Target = ImportShortcut(item.Target);
                changed = true;
            }
            if (string.Equals(item.Type, DockItemTypes.Folder, StringComparison.OrdinalIgnoreCase)
                && item.OpenBehavior == DockOpenBehaviors.Auto)
            {
                item.OpenBehavior = DockOpenBehaviors.Flyout;
                changed = true;
            }
        }

        return changed;
    }

    private static bool NormalizeFolderGroups(DockItem item)
    {
        var changed = false;
        item.FolderGroups ??= [];
        var groups = new List<FolderGroup>();
        foreach (var group in item.FolderGroups)
        {
            if (string.IsNullOrWhiteSpace(group.Name))
            {
                changed = true;
                continue;
            }

            if (string.IsNullOrWhiteSpace(group.Id))
            {
                group.Id = Guid.NewGuid().ToString("N");
                changed = true;
            }

            group.Name = group.Name.Trim();
            group.EntryKeys ??= [];
            var normalizedKeys = group.EntryKeys
                .Where(key => !string.IsNullOrWhiteSpace(key))
                .Select(NormalizeEntryKey)
                .Where(key => !string.IsNullOrWhiteSpace(key))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (normalizedKeys.Count != group.EntryKeys.Count || !normalizedKeys.SequenceEqual(group.EntryKeys, StringComparer.Ordinal))
            {
                group.EntryKeys = normalizedKeys;
                changed = true;
            }

            groups.Add(group);
        }

        if (groups.Count != item.FolderGroups.Count)
        {
            changed = true;
        }
        item.FolderGroups = groups;
        return changed;
    }

    private static string NormalizeEntryKey(string key)
    {
        return key.Trim().Replace('\\', '/').TrimStart('/');
    }

    private bool ShouldImportShortcut(string path)
    {
        return ShortcutService.IsShortcutFile(path) && !IsManagedShortcut(path);
    }

    private bool IsManagedShortcut(string path)
    {
        try
        {
            var shortcutRoot = Path.GetFullPath(_paths.Shortcuts).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            var fullPath = Path.GetFullPath(path);
            return fullPath.StartsWith(shortcutRoot, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    private string ImportShortcut(string path)
    {
        _paths.Ensure();
        var safeName = string.Join("_", Path.GetFileName(path).Split(Path.GetInvalidFileNameChars()));
        var target = Path.Combine(_paths.Shortcuts, $"{Guid.NewGuid():N}-{safeName}");
        File.Copy(path, target, true);
        return target;
    }

    private AppConfig RecoverFromLoadFailure(Exception loadException)
    {
        TryPreserveBrokenConfig(loadException);

        try
        {
            var backupService = new DataBackupService(_paths, _log);
            var latest = backupService.GetLatestBackup();
            if (!string.IsNullOrWhiteSpace(latest))
            {
                var recovered = backupService.ReadConfigFromBackup(latest);
                backupService.ImportResources(latest);
                Save(recovered);
                _log.Info("Recovered settings from automatic backup: " + latest);
                return recovered;
            }
        }
        catch (Exception ex)
        {
            _log.Error(ex);
        }

        var created = CreateDefault();
        try
        {
            Save(created);
        }
        catch (Exception ex)
        {
            _log.Error(ex);
        }
        return created;
    }

    private void TryPreserveBrokenConfig(Exception loadException)
    {
        try
        {
            if (!File.Exists(_paths.ConfigPath))
            {
                return;
            }

            _paths.Ensure();
            var brokenPath = Path.Combine(
                _paths.Root,
                $"settings.broken-{DateTime.Now:yyyyMMdd-HHmmss}.json");
            File.Copy(_paths.ConfigPath, brokenPath, overwrite: false);
            _log.Info("Preserved broken settings file: " + brokenPath + " (" + loadException.GetType().Name + ")");
        }
        catch (Exception ex)
        {
            _log.Error(ex);
        }
    }

    private static string NormalizeIconStyle(string? iconStyle)
    {
        return iconStyle?.ToLowerInvariant() switch
        {
            DockIconStyles.RoundedRect => DockIconStyles.RoundedRect,
            _ => DockIconStyles.Default
        };
    }

    private static string NormalizeOpenBehavior(string? behavior)
    {
        return behavior?.ToLowerInvariant() switch
        {
            DockOpenBehaviors.Flyout => DockOpenBehaviors.Flyout,
            DockOpenBehaviors.Explorer => DockOpenBehaviors.Explorer,
            _ => DockOpenBehaviors.Auto
        };
    }

    private static string DefaultName(string type)
    {
        return type switch
        {
            DockItemTypes.Settings => "\u8bbe\u7f6e",
            DockItemTypes.Separator => "\u5206\u9694\u7b26",
            DockItemTypes.App => "\u5e94\u7528",
            DockItemTypes.Folder => "\u6587\u4ef6\u5939",
            DockItemTypes.Url => "\u7f51\u5740",
            DockItemTypes.Shell => "\u7cfb\u7edf\u4f4d\u7f6e",
            _ => "\u6587\u4ef6"
        };
    }
}
