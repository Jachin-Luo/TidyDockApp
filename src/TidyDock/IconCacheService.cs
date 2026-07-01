using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace TidyDock
{
    public class IconCacheService
    {
        private readonly string _cacheDirectory;

        public IconCacheService(string cacheDirectory)
        {
            _cacheDirectory = cacheDirectory;
            Directory.CreateDirectory(_cacheDirectory);
        }

        public ImageSource GetIcon(DockItem item, int size)
        {
            if (item == null || item.IsSeparator)
            {
                return null;
            }

            if (!string.IsNullOrEmpty(item.Icon) && File.Exists(item.Icon))
            {
                return LoadBitmap(item.Icon, size);
            }

            var key = Hash("v2|" + size + "|" + (item.Type ?? string.Empty) + "|" + (item.Target ?? string.Empty));
            var cachePath = Path.Combine(_cacheDirectory, key + ".png");
            if (File.Exists(cachePath))
            {
                return LoadBitmap(cachePath, size);
            }

            var icon = ExtractIcon(item, size);
            if (icon != null)
            {
                try
                {
                    SaveBitmap(icon, cachePath);
                }
                catch
                {
                }
                return icon;
            }

            return CreateFallbackIcon(item.Type, size);
        }

        public ImageSource GetPathIcon(string path, bool isDirectory, int size)
        {
            var key = Hash("v2|" + size + "|" + (isDirectory ? "folder" : "file") + "|" + path);
            var cachePath = Path.Combine(_cacheDirectory, key + ".png");
            if (File.Exists(cachePath))
            {
                return LoadBitmap(cachePath, size);
            }

            var icon = ExtractShellIcon(path, isDirectory, size);
            if (icon != null)
            {
                try
                {
                    SaveBitmap(icon, cachePath);
                }
                catch
                {
                }
                return icon;
            }

            return CreateFallbackIcon(isDirectory ? "folder" : "file", size);
        }

        private ImageSource ExtractIcon(DockItem item, int size)
        {
            if (item.Type == "url")
            {
                return CreateFallbackIcon("url", size);
            }

            if (item.Type == "settings")
            {
                return CreateFallbackIcon("settings", size);
            }

            return ExtractShellIcon(item.Target, item.Type == "folder", size);
        }

        private ImageSource ExtractShellIcon(string path, bool isDirectory, int requestedSize)
        {
            var imageListIcon = ExtractImageListIcon(path, isDirectory, requestedSize);
            if (imageListIcon != null)
            {
                return imageListIcon;
            }

            try
            {
                SHFILEINFO shinfo = new SHFILEINFO();
                uint flags = SHGFI_ICON | SHGFI_LARGEICON;
                uint attributes = 0;
                var target = path;

                if (isDirectory)
                {
                    attributes = FILE_ATTRIBUTE_DIRECTORY;
                    flags = flags | SHGFI_USEFILEATTRIBUTES;
                }
                else if (!File.Exists(path))
                {
                    flags = flags | SHGFI_USEFILEATTRIBUTES;
                }

                IntPtr result = SHGetFileInfo(target, attributes, ref shinfo, (uint)Marshal.SizeOf(typeof(SHFILEINFO)), flags);
                if (result == IntPtr.Zero || shinfo.hIcon == IntPtr.Zero)
                {
                    return null;
                }

                var source = Imaging.CreateBitmapSourceFromHIcon(
                    shinfo.hIcon,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());
                DestroyIcon(shinfo.hIcon);
                source.Freeze();
                return source;
            }
            catch
            {
                return null;
            }
        }

        private ImageSource ExtractImageListIcon(string path, bool isDirectory, int requestedSize)
        {
            try
            {
                SHFILEINFO shinfo = new SHFILEINFO();
                uint flags = SHGFI_SYSICONINDEX;
                uint attributes = 0;

                if (isDirectory)
                {
                    attributes = FILE_ATTRIBUTE_DIRECTORY;
                    flags = flags | SHGFI_USEFILEATTRIBUTES;
                }
                else if (!File.Exists(path))
                {
                    flags = flags | SHGFI_USEFILEATTRIBUTES;
                }

                var result = SHGetFileInfo(path, attributes, ref shinfo, (uint)Marshal.SizeOf(typeof(SHFILEINFO)), flags);
                if (result == IntPtr.Zero || shinfo.iIcon < 0)
                {
                    return null;
                }

                var imageListSize = requestedSize >= 64 ? SHIL_JUMBO : SHIL_EXTRALARGE;
                var icon = GetImageListIcon(shinfo.iIcon, imageListSize);
                if (icon == null && imageListSize == SHIL_JUMBO)
                {
                    icon = GetImageListIcon(shinfo.iIcon, SHIL_EXTRALARGE);
                }

                return icon;
            }
            catch
            {
                return null;
            }
        }

        private ImageSource GetImageListIcon(int iconIndex, int imageListSize)
        {
            IImageList imageList;
            var iid = new Guid("46EB5926-582E-4017-9FDF-E8998DAA0950");
            var hr = SHGetImageList(imageListSize, ref iid, out imageList);
            if (hr != 0 || imageList == null)
            {
                return null;
            }

            IntPtr hIcon;
            hr = imageList.GetIcon(iconIndex, ILD_TRANSPARENT, out hIcon);
            if (hr != 0 || hIcon == IntPtr.Zero)
            {
                return null;
            }

            try
            {
                var source = Imaging.CreateBitmapSourceFromHIcon(
                    hIcon,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());
                source.Freeze();
                return source;
            }
            finally
            {
                DestroyIcon(hIcon);
            }
        }

        private ImageSource LoadBitmap(string path, int decodeSize)
        {
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
                if (decodeSize > 0)
                {
                    bitmap.DecodePixelWidth = Math.Max(16, decodeSize);
                }
                bitmap.UriSource = new Uri(path, UriKind.Absolute);
                bitmap.EndInit();
                bitmap.Freeze();
                return bitmap;
            }
            catch
            {
                return null;
            }
        }

        private void SaveBitmap(ImageSource source, string path)
        {
            var bitmap = source as BitmapSource;
            if (bitmap == null)
            {
                return;
            }

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using (var stream = File.Create(path))
            {
                encoder.Save(stream);
            }
        }

        private ImageSource CreateFallbackIcon(string type, int size)
        {
            var group = new DrawingGroup();
            var color = System.Windows.Media.Color.FromArgb(255, 73, 125, 186);
            if (type == "folder")
            {
                color = System.Windows.Media.Color.FromArgb(255, 58, 166, 104);
            }
            else if (type == "url")
            {
                color = System.Windows.Media.Color.FromArgb(255, 44, 160, 154);
            }
            else if (type == "settings")
            {
                color = System.Windows.Media.Color.FromArgb(255, 85, 94, 112);
            }

            using (var context = group.Open())
            {
                var brush = new SolidColorBrush(color);
                brush.Freeze();
                context.DrawRoundedRectangle(brush, null, new Rect(0, 0, size, size), 10, 10);
                var pen = new System.Windows.Media.Pen(System.Windows.Media.Brushes.White, 3);
                if (type == "folder")
                {
                    context.DrawGeometry(null, pen, Geometry.Parse("M 12 20 L 24 20 L 28 25 L 44 25 L 44 39 L 12 39 Z"));
                }
                else if (type == "url")
                {
                    context.DrawEllipse(null, pen, new System.Windows.Point(size / 2, size / 2), 15, 15);
                    context.DrawLine(pen, new System.Windows.Point(12, size / 2), new System.Windows.Point(size - 12, size / 2));
                }
                else
                {
                    context.DrawLine(pen, new System.Windows.Point(16, 16), new System.Windows.Point(size - 16, size - 16));
                    context.DrawLine(pen, new System.Windows.Point(size - 16, 16), new System.Windows.Point(16, size - 16));
                }
            }
            group.Freeze();
            return new DrawingImage(group);
        }

        private string Hash(string input)
        {
            using (var sha = SHA1.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
                var builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }

        [DllImport("Shell32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbFileInfo, uint uFlags);

        [DllImport("Shell32.dll", EntryPoint = "#727")]
        private static extern int SHGetImageList(int iImageList, ref Guid riid, out IImageList ppv);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyIcon(IntPtr hIcon);

        private const uint SHGFI_ICON = 0x000000100;
        private const uint SHGFI_LARGEICON = 0x000000000;
        private const uint SHGFI_USEFILEATTRIBUTES = 0x000000010;
        private const uint SHGFI_SYSICONINDEX = 0x000004000;
        private const uint FILE_ATTRIBUTE_DIRECTORY = 0x00000010;
        private const int SHIL_EXTRALARGE = 2;
        private const int SHIL_JUMBO = 4;
        private const int ILD_TRANSPARENT = 1;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct SHFILEINFO
        {
            public IntPtr hIcon;
            public int iIcon;
            public uint dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string szTypeName;
        }

        [ComImport]
        [Guid("46EB5926-582E-4017-9FDF-E8998DAA0950")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IImageList
        {
            [PreserveSig]
            int Add(IntPtr hbmImage, IntPtr hbmMask, ref int pi);

            [PreserveSig]
            int ReplaceIcon(int i, IntPtr hicon, ref int pi);

            [PreserveSig]
            int SetOverlayImage(int iImage, int iOverlay);

            [PreserveSig]
            int Replace(int i, IntPtr hbmImage, IntPtr hbmMask);

            [PreserveSig]
            int AddMasked(IntPtr hbmImage, int crMask, ref int pi);

            [PreserveSig]
            int Draw(IntPtr pimldp);

            [PreserveSig]
            int Remove(int i);

            [PreserveSig]
            int GetIcon(int i, int flags, out IntPtr picon);
        }
    }
}
