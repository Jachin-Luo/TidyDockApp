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
                Window = Color.FromArgb(25, 27, 31),
                Panel = Color.FromArgb(36, 39, 45),
                PanelAlt = Color.FromArgb(49, 53, 60),
                Text = Color.FromArgb(238, 242, 247),
                MutedText = Color.FromArgb(170, 178, 190),
                Border = Color.FromArgb(78, 86, 99),
                Accent = Color.FromArgb(34, 184, 166),
                AccentText = Color.White,
                Danger = Color.FromArgb(226, 92, 92),
                SurfaceLine = Color.FromArgb(104, 112, 126),
                DockTop = Color.FromArgb(78, 88, 104),
                DockBottom = Color.FromArgb(23, 25, 30),
                Shadow = Color.Black
            };
        }

        return new ThemePalette
        {
            IsDark = false,
            Window = Color.FromArgb(243, 247, 250),
            Panel = Color.FromArgb(248, 250, 252),
            PanelAlt = Color.White,
            Text = Color.FromArgb(23, 32, 51),
            MutedText = Color.FromArgb(91, 100, 116),
            Border = Color.FromArgb(194, 209, 222),
            Accent = Color.FromArgb(16, 132, 117),
            AccentText = Color.White,
            Danger = Color.FromArgb(199, 71, 71),
            SurfaceLine = Color.FromArgb(217, 226, 235),
            DockTop = Color.White,
            DockBottom = Color.FromArgb(225, 235, 244),
            Shadow = Color.FromArgb(31, 41, 55)
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
