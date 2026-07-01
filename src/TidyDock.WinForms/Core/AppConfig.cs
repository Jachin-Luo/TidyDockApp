namespace TidyDock.WinForms.Core;

internal sealed class AppConfig
{
    public int ConfigVersion { get; set; } = 1;
    public DockSettings Dock { get; set; } = new();
    public List<DockItem> Items { get; set; } = new();
}

internal sealed class DockSettings
{
    public string Position { get; set; } = "bottom";
    public string Display { get; set; } = "primary";
    public int IconSize { get; set; } = 52;
    public int IconGap { get; set; } = 10;
    public double Magnification { get; set; } = 1.42;
    public double Opacity { get; set; } = 0.78;
    public int CornerRadius { get; set; } = 20;
    public bool AutoHide { get; set; }
    public bool AlwaysOnTop { get; set; }
    public bool StartVisible { get; set; } = true;
    public bool ShowTrayIcon { get; set; } = true;
    public string Theme { get; set; } = "system";
    public string Language { get; set; } = "zh-CN";
}

internal sealed class DockItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Type { get; set; } = "file";
    public string Name { get; set; } = "";
    public string Target { get; set; } = "";
    public string IconPath { get; set; } = "";

    public bool IsSeparator => string.Equals(Type, DockItemTypes.Separator, StringComparison.OrdinalIgnoreCase);

    public override string ToString()
    {
        return IsSeparator ? "Separator" : $"[{Type}] {Name}";
    }
}

internal static class DockItemTypes
{
    public const string App = "app";
    public const string File = "file";
    public const string Folder = "folder";
    public const string Url = "url";
    public const string Separator = "separator";
    public const string Settings = "settings";
}
