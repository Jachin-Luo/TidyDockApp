using Microsoft.Win32;

namespace TidyDock.WinForms.UI;

internal sealed class ThemePalette
{
    public bool IsDark { get; init; }
    public Color Window { get; init; }
    public Color Panel { get; init; }
    public Color PanelAlt { get; init; }
    public Color Text { get; init; }
    public Color MutedText { get; init; }
    public Color Border { get; init; }
    public Color Accent { get; init; }
    public Color AccentText { get; init; }
    public Color Danger { get; init; }
    public Color SurfaceLine { get; init; }
    public Color DockTop { get; init; }
    public Color DockBottom { get; init; }
    public Color Shadow { get; init; }
}

internal static class Theme
{
    public static ThemePalette Get(string theme)
    {
        if (IsDark(theme))
        {
            return new ThemePalette
            {
                IsDark = true,
                Window = Color.FromArgb(28, 28, 30),
                Panel = Color.FromArgb(36, 36, 38),
                PanelAlt = Color.FromArgb(52, 52, 54),
                Text = Color.FromArgb(245, 245, 247),
                MutedText = Color.FromArgb(178, 178, 184),
                Border = Color.FromArgb(78, 78, 82),
                Accent = Color.FromArgb(34, 184, 166),
                AccentText = Color.FromArgb(12, 29, 26),
                Danger = Color.FromArgb(226, 92, 92),
                SurfaceLine = Color.FromArgb(96, 96, 100),
                DockTop = Color.FromArgb(44, 44, 46),
                DockBottom = Color.FromArgb(28, 28, 30),
                Shadow = Color.Black
            };
        }

        return new ThemePalette
        {
            IsDark = false,
            Window = Color.FromArgb(242, 242, 247),
            Panel = Color.FromArgb(248, 248, 250),
            PanelAlt = Color.White,
            Text = Color.FromArgb(28, 28, 30),
            MutedText = Color.FromArgb(90, 90, 96),
            Border = Color.FromArgb(200, 200, 206),
            Accent = Color.FromArgb(16, 132, 117),
            AccentText = Color.White,
            Danger = Color.FromArgb(199, 71, 71),
            SurfaceLine = Color.FromArgb(220, 220, 225),
            DockTop = Color.White,
            DockBottom = Color.FromArgb(235, 235, 240),
            Shadow = Color.FromArgb(28, 28, 30)
        };
    }

    private static bool IsDark(string theme)
    {
        if (string.Equals(theme, "dark", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (string.Equals(theme, "light", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            return key?.GetValue("AppsUseLightTheme") is int value && value == 0;
        }
        catch
        {
            return false;
        }
    }
}
