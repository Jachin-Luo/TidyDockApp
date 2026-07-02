using System.Text.Json;
using TidyDock.WinForms.Core;

namespace TidyDock.WinForms.Services;

internal sealed class ConfigService
{
    private readonly AppPaths _paths;
    private readonly LogService _log;
    private readonly JsonSerializerOptions _options = new() { WriteIndented = true };

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
            var config = JsonSerializer.Deserialize<AppConfig>(json, _options) ?? CreateDefault();
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
            return CreateDefault();
        }
    }

    public void Save(AppConfig config)
    {
        _ = Normalize(config);
        _paths.Ensure();
        var temp = _paths.ConfigPath + ".tmp";
        var backup = _paths.ConfigPath + ".bak";
        File.WriteAllText(temp, JsonSerializer.Serialize(config, _options));
        if (File.Exists(_paths.ConfigPath))
        {
            File.Copy(_paths.ConfigPath, backup, true);
        }
        File.Move(temp, _paths.ConfigPath, true);
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

    private bool Normalize(AppConfig config)
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
