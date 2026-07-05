using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Text;
using TidyDock.WinForms.Core;

namespace TidyDock.WinForms.Services;

internal sealed class IconService : IDisposable
{
    private const int MaxCacheEntries = 256;
    private readonly AppPaths _paths;
    private readonly LogService _log;
    private readonly object _cacheLock = new();
    private readonly Dictionary<string, CacheEntry> _cache = new(StringComparer.OrdinalIgnoreCase);
    private readonly LinkedList<string> _lru = new();

    public IconService(AppPaths paths, LogService log)
    {
        _paths = paths;
        _log = log;
    }

    public Image GetIcon(DockItem item, int size, bool wrapCustomIcons = false)
    {
        var key = $"{item.Type}|{item.Target}|{item.IconPath}|{item.IconStyle}|{wrapCustomIcons}|{size}";
        var cached = TryGetCachedIcon(key);
        if (cached is not null)
        {
            return cached;
        }

        var loaded = LoadIcon(item, size, wrapCustomIcons);
        lock (_cacheLock)
        {
            if (_cache.TryGetValue(key, out var existing))
            {
                loaded.Dispose();
                Touch(existing);
                return existing.Image;
            }

            var node = _lru.AddLast(key);
            _cache[key] = new CacheEntry(loaded, node);
            TrimCache();
            return loaded;
        }
    }

    public Image GetIcon(FolderEntry entry, int size)
    {
        var item = new DockItem
        {
            Type = entry.IsDirectory ? DockItemTypes.Folder : DockItemTypes.File,
            Name = entry.Name,
            Target = entry.Path
        };
        return GetIcon(item, size);
    }

    public string ImportCustomIcon(string path)
    {
        _paths.Ensure();
        var extension = Path.GetExtension(path);
        var safeName = string.Join("_", Path.GetFileNameWithoutExtension(path).Split(Path.GetInvalidFileNameChars()));
        var target = Path.Combine(_paths.Icons, $"{Guid.NewGuid():N}-{safeName}{extension}");
        File.Copy(path, target, true);
        ClearMemoryCache();
        return target;
    }

    public void Clear()
    {
        ClearMemoryCache();
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

    public void ClearMemoryCache()
    {
        lock (_cacheLock)
        {
            foreach (var entry in _cache.Values)
            {
                entry.Image.Dispose();
            }
            _cache.Clear();
            _lru.Clear();
        }
    }

    private Image? TryGetCachedIcon(string key)
    {
        lock (_cacheLock)
        {
            if (!_cache.TryGetValue(key, out var entry))
            {
                return null;
            }

            Touch(entry);
            return entry.Image;
        }
    }

    private void Touch(CacheEntry entry)
    {
        _lru.Remove(entry.Node);
        _lru.AddLast(entry.Node);
    }

    private void TrimCache()
    {
        while (_cache.Count > MaxCacheEntries && _lru.First is not null)
        {
            var key = _lru.First.Value;
            _lru.RemoveFirst();
            if (_cache.Remove(key, out var entry))
            {
                entry.Image.Dispose();
            }
        }
    }

    private Image LoadIcon(DockItem item, int size, bool wrapCustomIcons)
    {
        var icon = LoadBaseIcon(item, size);
        if (!ShouldWrapIcon(item, wrapCustomIcons))
        {
            return icon;
        }

        try
        {
            var wrapped = DrawRoundedRectIcon(icon, item, size);
            icon.Dispose();
            return wrapped;
        }
        catch (Exception ex)
        {
            _log.Error(ex);
            return icon;
        }
    }

    private Image LoadBaseIcon(DockItem item, int size)
    {
        try
        {
            var preferred = !string.IsNullOrWhiteSpace(item.IconPath) ? item.IconPath : item.Target;
            var iconSource = ResolveIconSource(preferred);
            if (IsShellTarget(iconSource))
            {
                var shellIcon = TryLoadShellIcon(iconSource, size);
                return shellIcon ?? DrawFallback(size, Color.FromArgb(76, 120, 188), "PC");
            }

            if (!string.IsNullOrWhiteSpace(iconSource) && (File.Exists(iconSource) || Directory.Exists(iconSource)))
            {
                if (File.Exists(iconSource) && IsImageFile(iconSource))
                {
                    return LoadImageIcon(iconSource, size);
                }

                var shellIcon = TryLoadShellIcon(iconSource, size);
                if (shellIcon is not null)
                {
                    return shellIcon;
                }

                if (File.Exists(iconSource))
                {
                    using var icon = Icon.ExtractAssociatedIcon(iconSource);
                    if (icon != null)
                    {
                        return icon.ToBitmap();
                    }
                }
            }

            if (string.Equals(item.Type, DockItemTypes.Folder, StringComparison.OrdinalIgnoreCase))
            {
                var folderIcon = TryLoadShellIcon(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), size);
                return folderIcon ?? DrawFallback(size, Color.FromArgb(58, 166, 104), "F");
            }
            if (string.Equals(item.Type, DockItemTypes.Url, StringComparison.OrdinalIgnoreCase))
            {
                return DrawFallback(size, Color.FromArgb(44, 160, 154), "U");
            }
            if (string.Equals(item.Type, DockItemTypes.Shell, StringComparison.OrdinalIgnoreCase))
            {
                return DrawFallback(size, Color.FromArgb(76, 120, 188), "PC");
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

    private static bool IsShellTarget(string path)
    {
        return path.StartsWith("shell:", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("::", StringComparison.OrdinalIgnoreCase);
    }

    private static string ResolveIconSource(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return path;
        }

        if (!File.Exists(path) || !string.Equals(Path.GetExtension(path), ".lnk", StringComparison.OrdinalIgnoreCase))
        {
            return path;
        }

        var shortcut = ResolveShortcutTarget(path);
        return !string.IsNullOrWhiteSpace(shortcut) ? shortcut : path;
    }

    public static string ResolveShortcutTarget(string path)
    {
        object? shellLink = null;
        try
        {
            shellLink = new ShellLink();
            var link = (IShellLinkW)shellLink;
            ((IPersistFile)shellLink).Load(path, 0);
            var builder = new StringBuilder(1024);
            var result = link.GetPath(builder, builder.Capacity, IntPtr.Zero, 0);
            if (result == 0)
            {
                var target = builder.ToString();
                if (!string.IsNullOrWhiteSpace(target))
                {
                    return target;
                }
            }
        }
        catch
        {
            return "";
        }
        finally
        {
            if (shellLink is not null && Marshal.IsComObject(shellLink))
            {
                _ = Marshal.FinalReleaseComObject(shellLink);
            }
        }

        return "";
    }

    private static bool IsImageFile(string path)
    {
        return Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".png" or ".jpg" or ".jpeg" or ".bmp" or ".gif" or ".webp" => true,
            _ => false
        };
    }

    private static Bitmap LoadImageIcon(string path, int size)
    {
        using var source = Image.FromFile(path);
        using var normalized = NormalizeForTransparentCrop(source);
        var visibleBounds = FindVisibleBounds(normalized);
        var bitmap = new Bitmap(size, size, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.Clear(Color.Transparent);
        graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
        graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;

        DrawVisibleImage(graphics, normalized, visibleBounds, new Rectangle(0, 0, size, size));
        return bitmap;
    }

    private static Bitmap? TryLoadShellIcon(string path, int size)
    {
        IShellItemImageFactory? factory = null;
        try
        {
            var iid = typeof(IShellItemImageFactory).GUID;
            SHCreateItemFromParsingName(path, IntPtr.Zero, ref iid, out factory);
            factory.GetImage(new NativeSize(size, size), ShellItemImageFactoryFlags.IconOnly | ShellItemImageFactoryFlags.BiggerSizeOk | ShellItemImageFactoryFlags.ScaleUp, out var bitmapHandle);
            if (bitmapHandle == IntPtr.Zero)
            {
                return null;
            }

            try
            {
                return CreateBitmapWithAlpha(bitmapHandle);
            }
            finally
            {
                _ = DeleteObject(bitmapHandle);
            }
        }
        catch
        {
            return null;
        }
        finally
        {
            if (factory is not null && Marshal.IsComObject(factory))
            {
                _ = Marshal.FinalReleaseComObject(factory);
            }
        }
    }

    private static Bitmap CreateBitmapWithAlpha(IntPtr bitmapHandle)
    {
        if (GetObject(bitmapHandle, Marshal.SizeOf<NativeBitmap>(), out var nativeBitmap) > 0
            && nativeBitmap.Bits != IntPtr.Zero
            && nativeBitmap.BitsPixel == 32
            && nativeBitmap.Width > 0
            && nativeBitmap.Height > 0)
        {
            var bitmap = new Bitmap(nativeBitmap.Width, nativeBitmap.Height, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
            var bounds = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
            var data = bitmap.LockBits(bounds, System.Drawing.Imaging.ImageLockMode.WriteOnly, bitmap.PixelFormat);
            try
            {
                var sourceStride = Math.Abs(nativeBitmap.WidthBytes);
                var targetStride = Math.Abs(data.Stride);
                var pixelBytes = Math.Min(nativeBitmap.Width * 4, Math.Min(sourceStride, targetStride));
                var row = new byte[pixelBytes];
                for (var y = 0; y < bitmap.Height; y++)
                {
                    var sourceY = bitmap.Height - 1 - y;
                    var sourceOffset = sourceY * sourceStride;
                    var targetOffset = y * data.Stride;
                    Marshal.Copy(IntPtr.Add(nativeBitmap.Bits, sourceOffset), row, 0, pixelBytes);
                    PremultiplyAlpha(row);
                    Marshal.Copy(row, 0, IntPtr.Add(data.Scan0, targetOffset), pixelBytes);
                }
            }
            finally
            {
                bitmap.UnlockBits(data);
            }

            return bitmap;
        }

        using var shellBitmap = Image.FromHbitmap(bitmapHandle);
        var clone = new Bitmap(shellBitmap.Width, shellBitmap.Height, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
        using var graphics = Graphics.FromImage(clone);
        graphics.Clear(Color.Transparent);
        graphics.DrawImage(shellBitmap, new Rectangle(0, 0, clone.Width, clone.Height));
        return clone;
    }

    private static void PremultiplyAlpha(byte[] row)
    {
        for (var i = 0; i <= row.Length - 4; i += 4)
        {
            var alpha = row[i + 3];
            if (alpha == 0)
            {
                row[i] = 0;
                row[i + 1] = 0;
                row[i + 2] = 0;
                continue;
            }
            if (alpha == 255)
            {
                continue;
            }

            row[i] = (byte)((row[i] * alpha + 127) / 255);
            row[i + 1] = (byte)((row[i + 1] * alpha + 127) / 255);
            row[i + 2] = (byte)((row[i + 2] * alpha + 127) / 255);
        }
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

    private static bool ShouldWrapIcon(DockItem item, bool wrapCustomIcons)
    {
        return !item.IsSeparator
            && string.Equals(item.IconStyle, DockIconStyles.RoundedRect, StringComparison.OrdinalIgnoreCase);
    }

    private static Bitmap DrawRoundedRectIcon(Image source, DockItem item, int size)
    {
        var bitmap = new Bitmap(size, size, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.Clear(Color.Transparent);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
        graphics.CompositingQuality = CompositingQuality.HighQuality;

        var inset = Math.Max(1f, size * 0.035f);
        var bounds = new RectangleF(inset, inset, size - inset * 2, size - inset * 2);
        var radius = Math.Max(8f, size * 0.23f);
        var topColor = Color.FromArgb(252, 253, 255);
        var bottomColor = Color.FromArgb(236, 239, 244);

        using (var shape = RoundedRect(bounds, radius))
        using (var background = new LinearGradientBrush(bounds, topColor, bottomColor, 90f))
        {
            graphics.FillPath(background, shape);

            using var border = new Pen(Color.FromArgb(52, Color.White), Math.Max(1f, size / 90f));
            graphics.DrawPath(border, shape);
        }

        var iconScale = string.Equals(item.Type, DockItemTypes.Folder, StringComparison.OrdinalIgnoreCase) ? 0.64f : 0.70f;
        var iconSize = Math.Max(1, (int)Math.Round(size * iconScale));
        var iconBounds = new Rectangle((size - iconSize) / 2, (size - iconSize) / 2, iconSize, iconSize);

        using var normalized = NormalizeForTransparentCrop(source);
        var visibleBounds = FindVisibleBounds(normalized);
        DrawVisibleImage(graphics, normalized, visibleBounds, iconBounds);
        return bitmap;
    }

    private static Bitmap NormalizeForTransparentCrop(Image source)
    {
        var bitmap = new Bitmap(source.Width, source.Height, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.Clear(Color.Transparent);
        graphics.CompositingMode = CompositingMode.SourceOver;
        graphics.CompositingQuality = CompositingQuality.HighQuality;
        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
        graphics.DrawImage(source, new Rectangle(0, 0, bitmap.Width, bitmap.Height));
        return bitmap;
    }

    private static Rectangle FindVisibleBounds(Bitmap bitmap)
    {
        var bounds = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
        var data = bitmap.LockBits(bounds, System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
        try
        {
            var stride = Math.Abs(data.Stride);
            var bytes = new byte[stride * bitmap.Height];
            Marshal.Copy(data.Scan0, bytes, 0, bytes.Length);

            var left = bitmap.Width;
            var top = bitmap.Height;
            var right = -1;
            var bottom = -1;
            const byte visibleAlphaThreshold = 8;

            for (var y = 0; y < bitmap.Height; y++)
            {
                var row = y * stride;
                for (var x = 0; x < bitmap.Width; x++)
                {
                    var alpha = bytes[row + x * 4 + 3];
                    if (alpha <= visibleAlphaThreshold)
                    {
                        continue;
                    }

                    left = Math.Min(left, x);
                    top = Math.Min(top, y);
                    right = Math.Max(right, x);
                    bottom = Math.Max(bottom, y);
                }
            }

            return right < left || bottom < top
                ? bounds
                : Rectangle.FromLTRB(left, top, right + 1, bottom + 1);
        }
        finally
        {
            bitmap.UnlockBits(data);
        }
    }

    private static void DrawVisibleImage(Graphics graphics, Image source, Rectangle visibleBounds, Rectangle targetBounds)
    {
        if (visibleBounds.Width <= 0 || visibleBounds.Height <= 0)
        {
            graphics.DrawImage(source, targetBounds);
            return;
        }

        var scale = Math.Min(targetBounds.Width / (double)visibleBounds.Width, targetBounds.Height / (double)visibleBounds.Height);
        var width = Math.Max(1, (int)Math.Round(visibleBounds.Width * scale));
        var height = Math.Max(1, (int)Math.Round(visibleBounds.Height * scale));
        var destination = new Rectangle(
            targetBounds.Left + (targetBounds.Width - width) / 2,
            targetBounds.Top + (targetBounds.Height - height) / 2,
            width,
            height);
        graphics.DrawImage(source, destination, visibleBounds, GraphicsUnit.Pixel);
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
        ClearMemoryCache();
    }

    private sealed record CacheEntry(Image Image, LinkedListNode<string> Node);

    [Flags]
    private enum ShellItemImageFactoryFlags
    {
        BiggerSizeOk = 0x00000001,
        IconOnly = 0x00000004,
        ScaleUp = 0x00000100
    }

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct NativeSize
    {
        public readonly int Cx;
        public readonly int Cy;

        public NativeSize(int cx, int cy)
        {
            Cx = cx;
            Cy = cy;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeBitmap
    {
        public int Type;
        public int Width;
        public int Height;
        public int WidthBytes;
        public ushort Planes;
        public ushort BitsPixel;
        public IntPtr Bits;
    }

    [ComImport]
    [Guid("BCC18B79-BA16-442F-80C4-8A59C30C463B")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IShellItemImageFactory
    {
        void GetImage(NativeSize size, ShellItemImageFactoryFlags flags, out IntPtr phbm);
    }

    [ComImport]
    [Guid("00021401-0000-0000-C000-000000000046")]
    private sealed class ShellLink
    {
    }

    [ComImport]
    [Guid("000214F9-0000-0000-C000-000000000046")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IShellLinkW
    {
        [PreserveSig]
        int GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile, int cchMaxPath, IntPtr pfd, uint fFlags);

        [PreserveSig]
        int GetIDList(out IntPtr ppidl);

        [PreserveSig]
        int SetIDList(IntPtr pidl);

        [PreserveSig]
        int GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszName, int cchMaxName);

        [PreserveSig]
        int SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);

        [PreserveSig]
        int GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszDir, int cchMaxPath);

        [PreserveSig]
        int SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);

        [PreserveSig]
        int GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszArgs, int cchMaxPath);

        [PreserveSig]
        int SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);

        [PreserveSig]
        int GetHotkey(out short pwHotkey);

        [PreserveSig]
        int SetHotkey(short wHotkey);

        [PreserveSig]
        int GetShowCmd(out int piShowCmd);

        [PreserveSig]
        int SetShowCmd(int iShowCmd);

        [PreserveSig]
        int GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszIconPath, int cchIconPath, out int piIcon);

        [PreserveSig]
        int SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);

        [PreserveSig]
        int SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, uint dwReserved);

        [PreserveSig]
        int Resolve(IntPtr hwnd, uint fFlags);

        [PreserveSig]
        int SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
    }

    [ComImport]
    [Guid("0000010B-0000-0000-C000-000000000046")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IPersistFile
    {
        [PreserveSig]
        int GetClassID(out Guid pClassID);

        [PreserveSig]
        int IsDirty();

        [PreserveSig]
        int Load([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, uint dwMode);

        [PreserveSig]
        int Save([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, bool fRemember);

        [PreserveSig]
        int SaveCompleted([MarshalAs(UnmanagedType.LPWStr)] string pszFileName);

        [PreserveSig]
        int GetCurFile([MarshalAs(UnmanagedType.LPWStr)] out string ppszFileName);
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
    private static extern void SHCreateItemFromParsingName(
        string pszPath,
        IntPtr pbc,
        ref Guid riid,
        [MarshalAs(UnmanagedType.Interface)] out IShellItemImageFactory ppv);

    [DllImport("gdi32.dll", SetLastError = true)]
    private static extern bool DeleteObject(IntPtr hObject);

    [DllImport("gdi32.dll", SetLastError = true)]
    private static extern int GetObject(IntPtr hObject, int cbBuffer, out NativeBitmap lpvObject);
}
