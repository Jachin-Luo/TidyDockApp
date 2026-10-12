using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Reflection;
using TidyDock.WinForms.Core;
using TidyDock.WinForms.Services;
using TidyDock.WinForms.Shell;
using TidyDock.WinForms.UI;

namespace TidyDock.UiTests;

internal static class FrostPreview
{
    public static void Run(string output)
    {
        using var scene = new SceneForm
        {
            FormBorderStyle = FormBorderStyle.None,
            StartPosition = FormStartPosition.CenterScreen,
            Size = new Size(1060, 780),
            ShowInTaskbar = false,
            TopMost = true
        };
        scene.Show();
        // Terminal hosts may force the first ShowWindow call to use SW_HIDE.
        scene.Hide();
        scene.Show();
        scene.Activate();
        scene.BringToFront();
        _ = SetWindowPos(scene.Handle, new IntPtr(-1), scene.Left, scene.Top, scene.Width, scene.Height, 0x0040);
        scene.Refresh();
        Pump();
        using var baseline = CaptureScene(scene);
        baseline.Save(Path.Combine(output, "frost-baseline.png"), ImageFormat.Png);
        using var glass = new LayeredSample
        {
            FormBorderStyle = FormBorderStyle.None,
            StartPosition = FormStartPosition.Manual,
            Bounds = new Rectangle(scene.Left + 60, scene.Top + 80, 640, 260),
            ShowInTaskbar = false,
            TopMost = true,
            BackColor = Color.Black
        };
        glass.Show(scene);
        glass.Render();
        glass.CheckRegion();
        Pump();
        using var bitmap = CaptureScene(scene);
        bitmap.Save(Path.Combine(output, "frost-compositor.png"), ImageFormat.Png);
        CheckRoundedCorners(baseline, bitmap, new Rectangle(80, 120, 600, 180), 18, "Glass");
        var sample = new Rectangle(140, 240, 440, 30);
        if (glass.FrostActive)
        {
            var before = EdgeEnergy(baseline, sample);
            var after = EdgeEnergy(bitmap, sample);
            Require(after < before * 0.35, $"Background detail is blurred ({before:F2} -> {after:F2})");
            var variation = ColorDistance(bitmap.GetPixel(140, 260), bitmap.GetPixel(580, 260));
            Require(variation > 3, $"Glass retains backdrop color variation instead of becoming a flat fill ({variation:F2})");
            Console.WriteLine($"PASS: compositor blur, edge energy {before:F2} -> {after:F2}, backdrop color variation {variation:F2}.");
        }
        else
        {
            Console.WriteLine("Compositor is unavailable or transparency is disabled; tested opaque fallback.");
        }
        glass.CheckStableFrames();
        glass.CheckLifecycle(scene);
        glass.Hide();
        Require(!glass.FrostActive && !IsWindowVisible(glass.FrostHandle), "Hiding the owner hides the material");
        glass.Dark = true;
        glass.Show(scene);
        glass.Render();
        Pump();
        using (var dark = CaptureScene(scene))
        {
            dark.Save(Path.Combine(output, "frost-compositor-dark.png"), ImageFormat.Png);
        }
        glass.Close();
        using var settings = new Program.SettingsFixture(6);
        settings.Form.Hide();
        settings.Form.Location = new Point(scene.Left + 40, scene.Top + 40);
        settings.Form.TopMost = true;
        settings.Form.Show(scene);
        settings.Form.BringToFront();
        Pump();
        using var settingsImage = CaptureScene(scene);
        settingsImage.Save(Path.Combine(output, "settings-frost-compositor.png"), ImageFormat.Png);
        settings.Form.Dispose();
        CaptureDockAndFlyout(scene, output, "light");
        CaptureDockAndFlyout(scene, output, "dark");
        scene.Close();
    }

    private static void CaptureDockAndFlyout(Form scene, string output, string theme)
    {
        var paths = new AppPaths(Path.Combine(output, "fixture-" + theme));
        paths.Ensure();
        var folder = Path.Combine(paths.Root, "Documents");
        Directory.CreateDirectory(folder);
        foreach (var name in new[] { "Notes.txt", "Plan.md", "Checklist.txt", "Readme.md" })
        {
            File.WriteAllText(Path.Combine(folder, name), "TidyDock UI test fixture");
        }
        Directory.CreateDirectory(Path.Combine(folder, "Archive"));
        var log = new LogService(paths);
        using var icons = new IconService(paths, log);
        var config = new AppConfig
        {
            Dock = new DockSettings { Theme = theme, AlwaysOnTop = true, ShowTrayIcon = false, Opacity = 0.86 },
            FolderPanel = new FolderPanelSettings { MaxHeight = 340 },
            Items =
            [
                new DockItem { Name = "TidyDock", Type = DockItemTypes.App, Target = Environment.ProcessPath! },
                new DockItem { Name = "Documents", Type = DockItemTypes.Folder, Target = folder },
                new DockItem { Name = "This PC", Type = DockItemTypes.Shell, Target = "shell:MyComputerFolder" },
                new DockItem { Name = "Settings", Type = DockItemTypes.Settings }
            ]
        };
        using var dock = new DockForm(config, new ConfigService(paths, log), new DataBackupService(paths, log), icons,
            new ShortcutService(paths), new FolderService(), new StartupService(), new DockLayoutService(), log);
        dock.Show(scene);
        dock.Location = new Point(scene.Left + (scene.Width - dock.Width) / 2, scene.Bottom - dock.Height - 48);
        typeof(DockForm).GetMethod("RenderLayeredDock", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(dock, null);
        using var flyout = new FolderFlyoutForm(config, new FolderService(), icons, log) { TopMost = true, Owner = scene };
        var closeTimer = (System.Windows.Forms.Timer)typeof(FolderFlyoutForm)
            .GetField("_closeTimer", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(flyout)!;
        closeTimer.Interval = 10000;
        flyout.ShowFor(config.Items[1], new Rectangle(scene.Left + scene.Width / 2 - 24, dock.Top, 48, 48), "bottom");
        Pump();
        Require(flyout.Visible, "Flyout remains visible for compositor inspection");
        using var screenshot = CaptureScene(scene);
        screenshot.Save(Path.Combine(output, $"dock-flyout-{theme}.png"), ImageFormat.Png);
        var dockFrost = (FrostedBackdrop)typeof(DockForm).GetField("_frost", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(dock)!;
        var flyoutFrost = (FrostedBackdrop)typeof(FolderFlyoutForm).GetField("_frost", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(flyout)!;
        var hover = typeof(DockForm).GetMethod("SetHover", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var renderDock = typeof(DockForm).GetMethod("RenderLayeredDock", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var renderFlyout = typeof(FolderFlyoutForm).GetMethod("RenderLayeredFlyout", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var slot = (Rectangle)typeof(DockForm).GetMethod("GetItemRect", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(dock, [1])!;
        CheckTemporalStability(scene, dock, dockFrost, frame =>
        {
            if (frame % 10 == 0)
            {
                var index = frame % 20 == 0 ? 1 : -1;
                hover.Invoke(dock, [index, index < 0 ? null : new Point(slot.Left + slot.Width / 2, slot.Top + slot.Height / 2)]);
            }
            renderDock.Invoke(dock, null);
        }, $"Dock hover ({theme})");
        CheckTemporalStability(scene, flyout, flyoutFrost, _ => renderFlyout.Invoke(flyout, null), $"Flyout ({theme})");
        flyout.Hide();
        dock.Hide();
        Require(!dockFrost.Active && !flyoutFrost.Active, "Hidden Dock and flyout leave no material windows");
        Require(!IsWindowVisible(dockFrost.SurfaceHandle) && !IsWindowVisible(flyoutFrost.SurfaceHandle), "Native material windows are hidden");
    }

    private static void CheckTemporalStability(Form scene, Form owner, FrostedBackdrop frost, Action<int> render, string label)
    {
        if (!frost.Active)
        {
            return;
        }
        using var observer = new PositionObserver(frost.SurfaceHandle, owner.Handle);
        var strip = new Rectangle(owner.Left - scene.Left + 24, owner.Bottom - scene.Top - 7, owner.Width - 48, 3);
        using var baseline = CaptureScene(scene);
        double maximumDifference = 0;
        for (var frame = 0; frame < 60; frame++)
        {
            render(frame);
            Application.DoEvents();
            DwmFlush();
            using var image = CaptureScene(scene);
            double difference = 0;
            for (var y = strip.Top; y < strip.Bottom; y++)
            {
                for (var x = strip.Left; x < strip.Right; x++)
                {
                    difference += ColorDistance(baseline.GetPixel(x, y), image.GetPixel(x, y));
                }
            }
            maximumDifference = Math.Max(maximumDifference, difference / (strip.Width * strip.Height));
            Require(GetWindow(owner.Handle, 2) == frost.SurfaceHandle && IsWindowVisible(frost.SurfaceHandle),
                $"{label}: foreground and material keep their order and visibility");
            Thread.Sleep(16);
        }
        Require(observer.Changes == 0 && observer.OutOfOrder == 0,
            $"{label}: 60 frames do not reposition the material ({observer.Changes} requests, {observer.OutOfOrder} out of order)");
        Require(maximumDifference < 3, $"{label}: background remains stable across frames (maximum difference {maximumDifference:F2}/255)");
        Console.WriteLine($"PASS: {label}, 60 frames, no material position requests; maximum background difference {maximumDifference:F2}/255.");
    }

    private static double EdgeEnergy(Bitmap image, Rectangle bounds)
    {
        double total = 0;
        for (var y = bounds.Top; y < bounds.Bottom; y++)
        {
            for (var x = bounds.Left + 1; x < bounds.Right; x++)
            {
                total += ColorDistance(image.GetPixel(x - 1, y), image.GetPixel(x, y));
            }
        }
        return total / (bounds.Height * (bounds.Width - 1));
    }

    private static void CheckRoundedCorners(Bitmap baseline, Bitmap image, Rectangle bounds, int radius, string label)
    {
        double maximum = 0;
        foreach (var right in new[] { false, true })
        {
            foreach (var bottom in new[] { false, true })
            {
                double total = 0;
                var count = 0;
                for (var y = 0; y < radius; y++)
                {
                    for (var x = 0; x < radius; x++)
                    {
                        // Leave room for the antialiased rim and the 3px shadow offset by 2px downward.
                        if (Math.Pow(radius - x, 2) + Math.Pow(radius - y, 2) <= Math.Pow(radius + 8, 2))
                        {
                            continue;
                        }
                        var px = right ? bounds.Right - 1 - x : bounds.Left + x;
                        var py = bottom ? bounds.Bottom - 1 - y : bounds.Top + y;
                        total += ColorDistance(baseline.GetPixel(px, py), image.GetPixel(px, py));
                        count++;
                    }
                }
                if (count > 0)
                {
                    maximum = Math.Max(maximum, total / count);
                }
            }
        }
        Require(maximum < 3, $"{label}: rounded cutouts preserve the backdrop (corner difference {maximum:F2}/255)");
        Console.WriteLine($"PASS: {label}, four rounded cutouts, maximum backdrop difference {maximum:F2}/255.");
    }

    private static double ColorDistance(Color a, Color b) =>
        (Math.Abs(a.R - b.R) + Math.Abs(a.G - b.G) + Math.Abs(a.B - b.B)) / 3d;

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    private static void Pump()
    {
        var end = Environment.TickCount64 + 1200;
        while (Environment.TickCount64 < end)
        {
            Application.DoEvents();
            Thread.Sleep(10);
        }
    }

    private static Bitmap CaptureScene(Form scene)
    {
        var image = new Bitmap(scene.Width, scene.Height);
        using (var graphics = Graphics.FromImage(image))
        {
            graphics.CopyFromScreen(scene.Location, Point.Empty, scene.Size);
        }
        var marker = image.GetPixel(4, 4);
        if (Math.Abs(marker.R - 102) > 20 || Math.Abs(marker.G - 183) > 20 || Math.Abs(marker.B - 187) > 20)
        {
            image.Dispose();
            throw new InvalidOperationException($"The test scene is occluded; no desktop capture saved. Marker: {marker}; scene {scene.Handle}, visible {IsWindowVisible(scene.Handle)}, style {NativeMethods.GetWindowLong(scene.Handle, NativeMethods.GwlExStyle):X}, at marker {WindowFromPoint(new NativeMethods.PointNative(scene.Left + 4, scene.Top + 4))}, DPI {scene.DeviceDpi}");
        }
        return image;
    }

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr hwnd, IntPtr after, int x, int y, int width, int height, uint flags);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hwnd);

    [DllImport("user32.dll")]
    private static extern IntPtr GetWindow(IntPtr hwnd, uint command);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hwnd, out RectangleNative bounds);

    [DllImport("dwmapi.dll")]
    private static extern int DwmFlush();

    [DllImport("user32.dll")]
    private static extern IntPtr WindowFromPoint(NativeMethods.PointNative point);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateRectRgn(int left, int top, int right, int bottom);

    [DllImport("user32.dll")]
    private static extern int GetWindowRgn(IntPtr hwnd, IntPtr region);

    [DllImport("gdi32.dll")]
    private static extern bool PtInRegion(IntPtr region, int x, int y);

    [StructLayout(LayoutKind.Sequential)]
    private struct RectangleNative
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    private sealed class SceneForm : Form
    {
        protected override void OnPaint(PaintEventArgs e)
        {
            using var gradient = new LinearGradientBrush(ClientRectangle, Color.FromArgb(32, 150, 156), Color.FromArgb(205, 98, 110), 0f);
            e.Graphics.FillRectangle(gradient, ClientRectangle);
            using var brush = new SolidBrush(Color.FromArgb(80, Color.White));
            for (var x = 0; x < Width; x += 24)
            {
                e.Graphics.FillRectangle(brush, x, 0, 12, Height);
            }
        }
    }

    private sealed class LayeredSample : Form
    {
        private readonly FrostedBackdrop _frost;
        public bool FrostActive => _frost.Active;
        public IntPtr FrostHandle => _frost.SurfaceHandle;
        public bool Dark { get; set; }

        public void CheckStableFrames()
        {
            if (!FrostActive)
            {
                return;
            }
            using var observer = new PositionObserver(FrostHandle, Handle);
            for (var frame = 0; frame < 30; frame++)
            {
                Render();
                Application.DoEvents();
                Require(GetWindow(Handle, 2) == FrostHandle, "Material stays directly behind the foreground");
            }
            Require(observer.Changes == 0,
                $"Unchanged foreground frames do not reposition the material (observed {observer.Changes} position requests)");
            Console.WriteLine("PASS: 30 unchanged foreground frames without material position requests.");
        }

        public void CheckLifecycle(Form scene)
        {
            if (!FrostActive)
            {
                return;
            }
            using var observer = new PositionObserver(FrostHandle, Handle);
            var original = Bounds;
            Location = new Point(Left + 30, Top + 20);
            Size = new Size(Width + 20, Height + 10);
            Render();
            GetWindowRect(FrostHandle, out var bounds);
            var expected = RectangleToScreen(new Rectangle(20, 40, Width - 40, Height - 80));
            Require(Rectangle.FromLTRB(bounds.Left, bounds.Top, bounds.Right, bounds.Bottom) == expected,
                "Material follows owner movement and resize");
            CheckRegion();
            for (var cycle = 0; cycle < 3; cycle++)
            {
                Hide();
                Require(!FrostActive && !IsWindowVisible(FrostHandle), "Hide removes material");
                Show();
                Render();
                Require(FrostActive && GetWindow(Handle, 2) == FrostHandle, "Show restores material below foreground");
            }
            Owner = null;
            foreach (var topMost in new[] { false, true, false, true })
            {
                TopMost = topMost;
                Render();
                var nativeTopMost = (NativeMethods.GetWindowLong(FrostHandle, NativeMethods.GwlExStyle) & 0x0008) != 0;
                Require(nativeTopMost == topMost, "Material follows the native topmost state");
            }
            WindowState = FormWindowState.Minimized;
            Application.DoEvents();
            Require(!FrostActive && !IsWindowVisible(FrostHandle), "Minimize removes material");
            WindowState = FormWindowState.Normal;
            Owner = scene;
            Bounds = original;
            Render();
            Require(FrostActive && GetWindow(Handle, 2) == FrostHandle, "Restore returns the material below its foreground");
            Require(observer.OutOfOrder == 0, $"Material is never raised over foreground during transitions ({observer.OutOfOrder})");
            Console.WriteLine("PASS: material move, resize, repeated hide/show, topmost and minimize/restore transitions.");
        }

        public void CheckRegion()
        {
            var region = CreateRectRgn(0, 0, 0, 0);
            var kind = GetWindowRgn(_frost.SurfaceHandle, region);
            if (FrostActive)
            {
                Require(kind == 3 && !PtInRegion(region, 0, 0) && PtInRegion(region, 300, 90), "Backdrop input region respects rounded corners");
            }
            NativeMethods.DeleteObject(region);
        }

        public LayeredSample()
        {
            _frost = new FrostedBackdrop(this, () => new Rectangle(20, 40, Width - 40, Height - 80));
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _frost.Dispose();
            }
            base.Dispose(disposing);
        }

        protected override CreateParams CreateParams
        {
            get
            {
                var parameters = base.CreateParams;
                parameters.ExStyle |= NativeMethods.WsExLayered;
                return parameters;
            }
        }

        public void Render()
        {
            var palette = Theme.Get(Dark ? "dark" : "light");
            _frost.Update(FrostedSurface.Tint(palette, 1, false), 18);
            using var image = new Bitmap(Width, Height, PixelFormat.Format32bppPArgb);
            using (var graphics = Graphics.FromImage(image))
            {
                graphics.Clear(Color.Transparent);
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                FrostedSurface.Draw(graphics, new Rectangle(20, 40, Width - 40, Height - 80), 18, palette, _frost.Active);
                using var font = new Font("Segoe UI", 24);
                using var text = new SolidBrush(palette.Text);
                graphics.DrawString("TidyDock", font, text, 50, 70);
            }
            var screenDc = NativeMethods.GetDC(IntPtr.Zero);
            var dc = NativeMethods.CreateCompatibleDC(screenDc);
            var bitmap = image.GetHbitmap(Color.FromArgb(0));
            var previous = NativeMethods.SelectObject(dc, bitmap);
            var point = new NativeMethods.PointNative(Left, Top);
            var size = new NativeMethods.SizeNative(Width, Height);
            var source = new NativeMethods.PointNative();
            var blend = new NativeMethods.BlendFunction { BlendOp = 0, SourceConstantAlpha = 255, AlphaFormat = 1 };
            _ = NativeMethods.UpdateLayeredWindow(Handle, screenDc, ref point, ref size, dc, ref source, 0, ref blend, NativeMethods.UlwAlpha);
            NativeMethods.SelectObject(dc, previous);
            NativeMethods.DeleteObject(bitmap);
            NativeMethods.DeleteDC(dc);
            NativeMethods.ReleaseDC(IntPtr.Zero, screenDc);
        }
    }

    private sealed class PositionObserver : NativeWindow, IDisposable
    {
        private readonly IntPtr _foreground;
        public int Changes { get; private set; }
        public int OutOfOrder { get; private set; }

        public PositionObserver(IntPtr handle, IntPtr foreground)
        {
            _foreground = foreground;
            AssignHandle(handle);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == 0x0046) // WM_WINDOWPOSCHANGING
            {
                Changes++;
            }
            base.WndProc(ref m);
            if (m.Msg == 0x0047 && IsWindowVisible(Handle) && IsWindowVisible(_foreground)
                && GetWindow(_foreground, 2) != Handle)
            {
                OutOfOrder++;
            }
        }

        public void Dispose() => ReleaseHandle();
    }
}
