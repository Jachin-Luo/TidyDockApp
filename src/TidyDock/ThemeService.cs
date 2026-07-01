using Microsoft.Win32;
using System.Windows.Media;

namespace TidyDock
{
    public class ThemePalette
    {
        public bool IsDark { get; set; }
        public Color DockBackground { get; set; }
        public Color DockBorder { get; set; }
        public Color PanelBackground { get; set; }
        public Color PanelHeader { get; set; }
        public Color PanelBorder { get; set; }
        public Color Text { get; set; }
        public Color MutedText { get; set; }
        public Color ControlBackground { get; set; }
        public Color TileBackground { get; set; }
        public Color TileHover { get; set; }
        public Color Shadow { get; set; }
    }

    public static class ThemeService
    {
        public static ThemePalette GetPalette(string theme)
        {
            if (IsDark(theme))
            {
                return new ThemePalette
                {
                    IsDark = true,
                    DockBackground = Color.FromRgb(36, 42, 52),
                    DockBorder = Color.FromArgb(130, 255, 255, 255),
                    PanelBackground = Color.FromRgb(35, 41, 52),
                    PanelHeader = Color.FromRgb(45, 52, 64),
                    PanelBorder = Color.FromArgb(100, 255, 255, 255),
                    Text = Color.FromRgb(238, 242, 247),
                    MutedText = Color.FromRgb(174, 184, 198),
                    ControlBackground = Color.FromRgb(48, 56, 70),
                    TileBackground = Color.FromRgb(50, 58, 72),
                    TileHover = Color.FromRgb(66, 76, 94),
                    Shadow = Color.FromRgb(0, 0, 0)
                };
            }

            return new ThemePalette
            {
                IsDark = false,
                DockBackground = Color.FromRgb(241, 246, 250),
                DockBorder = Color.FromArgb(135, 255, 255, 255),
                PanelBackground = Color.FromRgb(248, 250, 252),
                PanelHeader = Color.FromRgb(255, 255, 255),
                PanelBorder = Color.FromArgb(150, 255, 255, 255),
                Text = Color.FromRgb(23, 32, 51),
                MutedText = Color.FromRgb(91, 100, 116),
                ControlBackground = Color.FromRgb(255, 255, 255),
                TileBackground = Color.FromRgb(255, 255, 255),
                TileHover = Color.FromRgb(255, 255, 255),
                Shadow = Color.FromRgb(31, 41, 55)
            };
        }

        public static SolidColorBrush Brush(Color color)
        {
            var brush = new SolidColorBrush(color);
            brush.Freeze();
            return brush;
        }

        public static SolidColorBrush Brush(Color color, double opacity)
        {
            var brush = new SolidColorBrush(Color.FromArgb(
                (byte)(opacity * 255),
                color.R,
                color.G,
                color.B));
            brush.Freeze();
            return brush;
        }

        private static bool IsDark(string theme)
        {
            if (theme == "dark")
            {
                return true;
            }

            if (theme == "light")
            {
                return false;
            }

            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
                {
                    if (key != null)
                    {
                        var value = key.GetValue("AppsUseLightTheme");
                        if (value is int)
                        {
                            return (int)value == 0;
                        }
                    }
                }
            }
            catch
            {
            }

            return false;
        }
    }
}
