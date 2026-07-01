using System.Collections.Concurrent;
using TidyDock.WinForms.Core;

namespace TidyDock.WinForms.Services;

internal sealed class IconService : IDisposable
{
    private readonly AppPaths _paths;
    private readonly LogService _log;
    private readonly ConcurrentDictionary<string, Image> _cache = new(StringComparer.OrdinalIgnoreCase);

    public IconService(AppPaths paths, LogService log)
    {
        _paths = paths;
        _log = log;
    }

    public Image GetIcon(DockItem item, int size)
    {
        var key = $"{item.Type}|{item.Target}|{item.IconPath}|{size}";
        return _cache.GetOrAdd(key, _ => LoadIcon(item, size));
    }

    public void Clear()
    {
        foreach (var image in _cache.Values)
        {
            image.Dispose();
        }
        _cache.Clear();
        try
        {
            if (Directory.Exists(_paths.Cache))
            {
                Directory.Delete(_paths.Cache, true);
            }
            Directory.CreateDirectory(_paths.Cache);
        }
        catch (Exception ex)
        {
            _log.Error(ex);
        }
    }

    private Image LoadIcon(DockItem item, int size)
    {
        try
        {
            var preferred = !string.IsNullOrWhiteSpace(item.IconPath) ? item.IconPath : item.Target;
            if (!string.IsNullOrWhiteSpace(preferred) && File.Exists(preferred))
            {
                using var icon = Icon.ExtractAssociatedIcon(preferred);
                if (icon != null)
                {
                    return icon.ToBitmap();
                }
            }

            if (string.Equals(item.Type, DockItemTypes.Folder, StringComparison.OrdinalIgnoreCase))
            {
                return DrawFallback(size, Color.FromArgb(58, 166, 104), "F");
            }
            if (string.Equals(item.Type, DockItemTypes.Url, StringComparison.OrdinalIgnoreCase))
            {
                return DrawFallback(size, Color.FromArgb(44, 160, 154), "U");
            }
            if (string.Equals(item.Type, DockItemTypes.Settings, StringComparison.OrdinalIgnoreCase))
            {
                return DrawFallback(size, Color.FromArgb(85, 94, 112), "S");
            }
        }
        catch (Exception ex)
        {
            _log.Error(ex);
        }

        return DrawFallback(size, Color.FromArgb(73, 125, 186), "T");
    }

    private static Bitmap DrawFallback(int size, Color color, string text)
    {
        var bitmap = new Bitmap(size, size);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        using var brush = new SolidBrush(color);
        using var textBrush = new SolidBrush(Color.White);
        graphics.Clear(Color.Transparent);
        using var path = RoundedRect(new RectangleF(2, 2, size - 4, size - 4), size / 4f);
        graphics.FillPath(brush, path);
        using var font = new Font("Segoe UI", Math.Max(10, size * 0.38f), FontStyle.Bold, GraphicsUnit.Pixel);
        var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        graphics.DrawString(text, font, textBrush, new RectangleF(0, 0, size, size), format);
        return bitmap;
    }

    private static System.Drawing.Drawing2D.GraphicsPath RoundedRect(RectangleF bounds, float radius)
    {
        var path = new System.Drawing.Drawing2D.GraphicsPath();
        var diameter = radius * 2;
        path.AddArc(bounds.X, bounds.Y, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Y, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.X, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }

    public void Dispose()
    {
        foreach (var image in _cache.Values)
        {
            image.Dispose();
        }
        _cache.Clear();
    }
}
