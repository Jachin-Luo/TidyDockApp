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
            Normalize(config);
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
        Normalize(config);
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
                    Name = "Settings",
                    Target = "settings"
                }
            ]
        };
    }

    private static void Normalize(AppConfig config)
    {
        config.ConfigVersion = 1;
        config.Dock ??= new DockSettings();
        config.Items ??= [];
        config.Dock.IconSize = Math.Clamp(config.Dock.IconSize, 32, 96);
        config.Dock.IconGap = Math.Clamp(config.Dock.IconGap, 0, 32);
        config.Dock.CornerRadius = Math.Clamp(config.Dock.CornerRadius, 6, 40);
        config.Dock.Opacity = Math.Clamp(config.Dock.Opacity, 0, 1);
        config.Dock.Magnification = Math.Clamp(config.Dock.Magnification, 1, 1.8);
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
            }
            item.Type = string.IsNullOrWhiteSpace(item.Type) ? DockItemTypes.File : item.Type;
            item.Name = string.IsNullOrWhiteSpace(item.Name) ? item.Type : item.Name;
            item.Target ??= "";
            item.IconPath ??= "";
        }
    }
}
