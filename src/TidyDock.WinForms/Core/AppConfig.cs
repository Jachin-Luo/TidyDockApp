namespace TidyDock.WinForms.Core;

internal sealed class AppConfig
{
    public int ConfigVersion { get; set; } = 1;
    public DockSettings Dock { get; set; } = new();
    public FolderPanelSettings FolderPanel { get; set; } = new();
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
    public bool EnableDragReorder { get; set; }
    public bool WrapCustomIcons { get; set; }
    public string Theme { get; set; } = "system";
    public string Language { get; set; } = "zh-CN";
}

internal sealed class FolderPanelSettings
{
    public int MaxItems { get; set; } = 300;
    public int MaxHeight { get; set; } = 520;
    public bool ShowHiddenFiles { get; set; }
    public string ViewMode { get; set; } = "grid";
}

internal sealed class DockItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Type { get; set; } = "file";
    public string Name { get; set; } = "";
    public string Target { get; set; } = "";
    public string IconPath { get; set; } = "";
    public string IconStyle { get; set; } = "default";
    public string OpenBehavior { get; set; } = "auto";

    public bool IsSeparator => string.Equals(Type, DockItemTypes.Separator, StringComparison.OrdinalIgnoreCase);

    public override string ToString()
    {
        return IsSeparator ? "\u5206\u9694\u7b26" : Name;
    }
}

internal sealed class FolderEntry
{
    public string Name { get; set; } = "";
    public string Path { get; set; } = "";
    public bool IsDirectory { get; set; }
    public bool IsShortcut { get; set; }
}

internal sealed class FolderReadResult
{
    public List<FolderEntry> Entries { get; set; } = [];
    public int TotalCount { get; set; }
    public bool IsTruncated { get; set; }
    public string ErrorMessage { get; set; } = "";
}

internal static class DockItemTypes
{
    public const string App = "app";
    public const string File = "file";
    public const string Folder = "folder";
    public const string Url = "url";
    public const string Shell = "shell";
    public const string Separator = "separator";
    public const string Settings = "settings";
}

internal static class DockOpenBehaviors
{
    public const string Auto = "auto";
    public const string Flyout = "flyout";
    public const string Explorer = "explorer";
}

internal static class DockIconStyles
{
    public const string Default = "default";
    public const string RoundedRect = "rounded-rect";
}
