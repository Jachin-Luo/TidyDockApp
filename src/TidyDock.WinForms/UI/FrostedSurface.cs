using System.Drawing.Drawing2D;

namespace TidyDock.WinForms.UI;

internal static class FrostedSurface
{
    public static Color Tint(ThemePalette palette, double opacity, bool highContrast)
    {
        var strength = highContrast ? 224 : palette.IsDark ? 192 : 164;
        return Color.FromArgb((int)Math.Round(strength * Math.Clamp(opacity, 0, 1)),
            palette.IsDark ? Color.FromArgb(28, 28, 30) : Color.FromArgb(242, 242, 247));
    }

    public static void Draw(Graphics graphics, Rectangle bounds, int radius, ThemePalette palette, bool blurred, double opacity = 1, bool highContrast = false)
    {
        opacity = Math.Clamp(opacity, 0, 1);
        if (bounds.Width <= 0 || bounds.Height <= 0 || opacity <= 0)
        {
            return;
        }

        radius = Math.Clamp(radius, 1, Math.Max(1, Math.Min(bounds.Width, bounds.Height) / 2));
        using var path = CardPanel.RoundedRect(bounds, radius);
        using var shadowPath = CardPanel.RoundedRect(new Rectangle(bounds.X, bounds.Y + 2, bounds.Width, bounds.Height), radius);
        using var shadow = new Pen(Color.FromArgb((int)(opacity * (palette.IsDark ? 38 : 22)), Color.Black), 3);
        graphics.DrawPath(shadow, shadowPath);

        // The compositor supplies the blur and grain. Only tint and a fine rim are drawn here.
        var fillAlpha = blurred ? Tint(palette, opacity, highContrast).A : 255;
        using var fill = new SolidBrush(Color.FromArgb(fillAlpha, palette.Panel));
        using var rim = new Pen(Color.FromArgb((int)(opacity * (palette.IsDark ? 48 : 156)), Color.White));
        graphics.FillPath(fill, path);
        graphics.DrawPath(rim, path);
    }
}
