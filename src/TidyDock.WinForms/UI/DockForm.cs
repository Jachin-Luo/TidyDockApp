using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using TidyDock.WinForms.Core;
using TidyDock.WinForms.Services;
using TidyDock.WinForms.Shell;

namespace TidyDock.WinForms.UI;

internal sealed class DockForm : Form
{
    private static readonly Color TransparentBackColor = Color.FromArgb(1, 2, 3);
    private const string ThisPcTarget = "shell:MyComputerFolder";
    private const int HoverAnimationDurationMs = 150;
    private readonly AppConfig _config;
    private readonly ConfigService _configService;
    private readonly IconService _iconService;
    private readonly ShortcutService _shortcutService;
    private readonly FolderService _folderService;
    private readonly StartupService _startupService;
    private readonly DockLayoutService _layoutService;
    private readonly LogService _log;
    private readonly Icon _appIcon;
    private readonly NotifyIcon _tray;
    private readonly System.Windows.Forms.Timer _hideTimer = new() { Interval = 650 };
    private readonly System.Windows.Forms.Timer _restoreTimer = new() { Interval = 220 };
    private readonly System.Windows.Forms.Timer _hoverAnimationTimer = new() { Interval = 16 };
    private readonly Stopwatch _hoverAnimationClock = new();
    private readonly HotZoneForm _hotZone = new();
    private readonly List<Rectangle> _itemRects = [];
    private ThemePalette _palette;
    private int _hoverIndex = -1;
    private int _pressedIndex = -1;
    private Point? _hoverPoint;
    private double[] _visualScales = [];
    private double[] _scaleStart = [];
    private double[] _scaleTarget = [];
    private SettingsForm? _settings;
    private FolderFlyoutForm? _folderFlyout;
    private int _restoreAttempts;

    public DockForm(
        AppConfig config,
        ConfigService configService,
        IconService iconService,
        ShortcutService shortcutService,
        FolderService folderService,
        StartupService startupService,
        DockLayoutService layoutService,
        LogService log)
    {
        _config = config;
        _configService = configService;
        _iconService = iconService;
        _shortcutService = shortcutService;
        _folderService = folderService;
        _startupService = startupService;
        _layoutService = layoutService;
        _log = log;
        _palette = Theme.Get(_config.Dock.Theme);
        _appIcon = LoadAppIcon();

        Text = "TidyDock";
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        DoubleBuffered = true;
        AllowDrop = true;
        BackColor = TransparentBackColor;

        _tray = new NotifyIcon
        {
            Text = "TidyDock",
            Icon = _appIcon,
            Visible = _config.Dock.ShowTrayIcon,
            ContextMenuStrip = BuildTrayMenu()
        };
        _tray.DoubleClick += delegate { ToggleVisible(); };

        DragEnter += OnDragEnter;
        DragDrop += OnDragDrop;
        MouseMove += OnMouseMove;
        MouseLeave += delegate { SetHover(-1, null); StartHideTimer(); };
        MouseDown += OnMouseDown;
        MouseUp += OnMouseUp;
        Resize += delegate
        {
            if (WindowState == FormWindowState.Minimized)
            {
                ScheduleRestore();
            }
        };
        _hideTimer.Tick += delegate
        {
            _hideTimer.Stop();
            if (_config.Dock.AutoHide && !ClientRectangle.Contains(PointToClient(Cursor.Position)))
            {
                HideForAutoHide();
            }
        };
        _restoreTimer.Tick += delegate { RestoreAfterShowDesktop(); };
        _hoverAnimationTimer.Tick += delegate { StepHoverAnimation(); };
        _hotZone.MouseEnter += delegate { ShowFromAutoHide(); };

        ApplySettings();
        if (!_config.Dock.StartVisible)
        {
            Hide();
        }
    }

    protected override bool ShowWithoutActivation => true;

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            cp.ExStyle |= NativeMethods.WsExToolWindow;
            cp.ExStyle |= NativeMethods.WsExLayered;
            cp.ExStyle &= ~NativeMethods.WsExAppWindow;
            return cp;
        }
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        RenderLayeredDock();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _tray.Dispose();
            _appIcon.Dispose();
            _hideTimer.Dispose();
            _restoreTimer.Dispose();
            _hoverAnimationTimer.Dispose();
            _hotZone.Dispose();
            _settings?.Dispose();
            _folderFlyout?.Dispose();
        }
        base.Dispose(disposing);
    }

    public void ApplySettings()
    {
        _palette = Theme.Get(_config.Dock.Theme);
        TopMost = _config.Dock.AlwaysOnTop;
        _tray.Visible = _config.Dock.ShowTrayIcon;
        _tray.ContextMenuStrip = BuildTrayMenu();
        RecalculateLayout();
        UpdateHotZone();
        _configService.Save(_config);
        RenderLayeredDock();
    }

    public void AddPath(string path)
    {
        try
        {
            var shortcutTarget = ShortcutService.ResolveShortcutTarget(path);
            var targetIsFolder = Directory.Exists(path) || Directory.Exists(shortcutTarget);
            var imported = targetIsFolder && !string.IsNullOrWhiteSpace(shortcutTarget) ? shortcutTarget : _shortcutService.ImportIfShortcut(path);
            var item = new DockItem
            {
                Target = imported,
                Name = Path.GetFileNameWithoutExtension(path),
                Type = targetIsFolder ? DockItemTypes.Folder : DockItemTypes.File,
                OpenBehavior = targetIsFolder ? DockOpenBehaviors.Flyout : DockOpenBehaviors.Auto
            };

            if (!targetIsFolder && File.Exists(path) && string.Equals(Path.GetExtension(path), ".exe", StringComparison.OrdinalIgnoreCase))
            {
                item.Type = DockItemTypes.App;
            }
            _config.Items.Add(item);
            ApplySettings();
        }
        catch (Exception ex)
        {
            _log.Error(ex);
            MessageBox.Show(ex.Message, "TidyDock", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
        e.Graphics.Clear(Color.Transparent);
        DrawDockBackground(e.Graphics);
        DrawItems(e.Graphics);
        DrawHoverLabel(e.Graphics);
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
    }

    private void RenderLayeredDock()
    {
        if (!IsHandleCreated || Width <= 0 || Height <= 0 || WindowState == FormWindowState.Minimized)
        {
            return;
        }

        using var bitmap = new Bitmap(Width, Height, PixelFormat.Format32bppPArgb);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.Clear(Color.Transparent);
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            DrawDockBackground(graphics);
            DrawItems(graphics);
            DrawHoverLabel(graphics);
        }

        var screenDc = NativeMethods.GetDC(IntPtr.Zero);
        var memoryDc = NativeMethods.CreateCompatibleDC(screenDc);
        var hBitmap = bitmap.GetHbitmap(Color.FromArgb(0));
        var oldBitmap = NativeMethods.SelectObject(memoryDc, hBitmap);

        try
        {
            var topPos = new NativeMethods.PointNative(Left, Top);
            var size = new NativeMethods.SizeNative(Width, Height);
            var source = new NativeMethods.PointNative(0, 0);
            var blend = new NativeMethods.BlendFunction
            {
                BlendOp = NativeMethods.AcSrcOver,
                BlendFlags = 0,
                SourceConstantAlpha = 255,
                AlphaFormat = NativeMethods.AcSrcAlpha
            };

            _ = NativeMethods.UpdateLayeredWindow(Handle, screenDc, ref topPos, ref size, memoryDc, ref source, 0, ref blend, NativeMethods.UlwAlpha);
        }
        finally
        {
            _ = NativeMethods.SelectObject(memoryDc, oldBitmap);
            _ = NativeMethods.DeleteObject(hBitmap);
            _ = NativeMethods.DeleteDC(memoryDc);
            _ = NativeMethods.ReleaseDC(IntPtr.Zero, screenDc);
        }
    }

    private void DrawDockBackground(Graphics graphics)
    {
        var bounds = GetDockBackgroundBounds();
        var opacity = Math.Clamp(_config.Dock.Opacity, 0, 1);
        var shadowOpacity = _palette.IsDark ? 0.10 + (opacity * 0.18) : 0.06 + (opacity * 0.12);
        var topOpacity = opacity * (_palette.IsDark ? 0.24 : 0.3);
        var bottomOpacity = opacity * (_palette.IsDark ? 0.34 : 0.26);
        var borderOpacity = 0.18 + (opacity * (_palette.IsDark ? 0.24 : 0.34));
        var shineOpacity = opacity * (_palette.IsDark ? 0.10 : 0.22);
        var innerOpacity = opacity * (_palette.IsDark ? 0.08 : 0.16);

        using var shadow = RoundedRect(new Rectangle(bounds.X + 3, bounds.Y + 6, bounds.Width - 6, bounds.Height - 4), _config.Dock.CornerRadius);
        using var shadowBrush = new SolidBrush(WithAlpha(_palette.Shadow, shadowOpacity));
        graphics.FillPath(shadowBrush, shadow);

        using var path = RoundedRect(bounds, _config.Dock.CornerRadius);
        using var brush = new LinearGradientBrush(
            bounds,
            WithAlpha(_palette.DockTop, topOpacity),
            WithAlpha(_palette.DockBottom, bottomOpacity),
            IsVertical() ? 0f : 90f);
        using var pen = new Pen(WithAlpha(_palette.Border, borderOpacity));
        graphics.FillPath(brush, path);

        var shineBounds = IsVertical()
            ? new Rectangle(bounds.Left + 4, bounds.Top + 5, Math.Max(1, bounds.Width / 3), bounds.Height - 10)
            : new Rectangle(bounds.Left + 6, bounds.Top + 4, bounds.Width - 12, Math.Max(1, bounds.Height / 3));
        using var shine = new LinearGradientBrush(shineBounds, WithAlpha(Color.White, shineOpacity), WithAlpha(Color.White, 0), IsVertical() ? 0f : 90f);
        using var shinePath = RoundedRect(shineBounds, Math.Max(6, _config.Dock.CornerRadius / 2));
        graphics.FillPath(shine, shinePath);

        using var innerPen = new Pen(WithAlpha(Color.White, innerOpacity));
        using var inner = RoundedRect(Rectangle.Inflate(bounds, -2, -2), Math.Max(4, _config.Dock.CornerRadius - 2));
        graphics.DrawPath(innerPen, inner);
        graphics.DrawPath(pen, path);
    }

    private void DrawItems(Graphics graphics)
    {
        _itemRects.Clear();
        var size = _config.Dock.IconSize;
        for (var i = 0; i < _config.Items.Count; i++)
        {
            var item = _config.Items[i];
            var rect = GetItemRect(i);
            _itemRects.Add(rect);
            if (item.IsSeparator)
            {
                DrawSeparator(graphics, rect);
                continue;
            }

            var scale = GetHoverScale(i);
            var iconSize = (int)(size * scale);
            var iconRect = new Rectangle(rect.Left + (rect.Width - iconSize) / 2, rect.Top + (rect.Height - iconSize) / 2, iconSize, iconSize);

            if (scale > 1.01)
            {
                using var glowPath = RoundedRect(iconRect, Math.Max(10, iconSize / 5));
                using var glow = new LinearGradientBrush(iconRect, WithAlpha(Color.White, _palette.IsDark ? 0.16 : 0.46), WithAlpha(_palette.PanelAlt, _palette.IsDark ? 0.08 : 0.24), 90f);
                graphics.FillPath(glow, glowPath);
            }

            var shadowRect = IsVertical()
                ? new Rectangle(iconRect.Left + 5, iconRect.Bottom - 8, iconRect.Width - 10, 7)
                : new Rectangle(iconRect.Left + 6, iconRect.Bottom - 7, iconRect.Width - 12, 7);
            using var iconShadow = new SolidBrush(WithAlpha(Color.Black, _palette.IsDark ? 0.24 : 0.16));
            graphics.FillEllipse(iconShadow, shadowRect);

            var sourceSize = Math.Max(iconSize, (int)Math.Ceiling(size * _config.Dock.Magnification));
            var image = _iconService.GetIcon(item, sourceSize, _config.Dock.WrapCustomIcons);
            graphics.DrawImage(image, iconRect);
        }
    }

    private void DrawHoverLabel(Graphics graphics)
    {
        if (IsVertical() || _hoverIndex < 0 || _hoverIndex >= _config.Items.Count || _config.Items[_hoverIndex].IsSeparator)
        {
            return;
        }

        var text = _config.Items[_hoverIndex].Name;
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        using var font = new Font("Microsoft YaHei UI", 8.5F, FontStyle.Regular, GraphicsUnit.Point);
        var size = graphics.MeasureString(text, font);
        var itemRect = GetItemRect(_hoverIndex);
        var labelWidth = Math.Min((int)Math.Ceiling(size.Width) + 18, Math.Max(96, Width - 12));
        var labelHeight = 24;
        var y = _config.Dock.Position == "top" ? itemRect.Bottom + 2 : itemRect.Top - labelHeight - 2;
        y = Math.Clamp(y, 4, Height - labelHeight - 4);
        var x = itemRect.Left + (itemRect.Width - labelWidth) / 2;
        x = Math.Clamp(x, 6, Width - labelWidth - 6);
        var labelRect = new Rectangle(x, y, labelWidth, labelHeight);

        using var path = RoundedRect(labelRect, 12);
        using var background = new SolidBrush(WithAlpha(_palette.IsDark ? Color.FromArgb(20, 22, 26) : Color.White, _palette.IsDark ? 0.86 : 0.9));
        using var border = new Pen(WithAlpha(_palette.Border, _palette.IsDark ? 0.55 : 0.7));
        using var foreground = new SolidBrush(_palette.Text);
        graphics.FillPath(background, path);
        graphics.DrawPath(border, path);
        using var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter };
        graphics.DrawString(text, font, foreground, labelRect, format);
    }

    private void DrawSeparator(Graphics graphics, Rectangle rect)
    {
        using var pen = new Pen(WithAlpha(_palette.MutedText, 0.32), 1);
        if (IsVertical())
        {
            graphics.DrawLine(pen, rect.Left + 8, rect.Top + rect.Height / 2, rect.Right - 8, rect.Top + rect.Height / 2);
        }
        else
        {
            graphics.DrawLine(pen, rect.Left + rect.Width / 2, rect.Top + 8, rect.Left + rect.Width / 2, rect.Bottom - 8);
        }
    }

    private Rectangle GetItemRect(int index)
    {
        var padding = GetContentPadding();
        var slot = GetSlotSize();
        var gap = _config.Dock.IconGap;
        if (IsVertical())
        {
            return new Rectangle(padding.Left, padding.Top + index * (slot + gap), slot, slot);
        }
        return new Rectangle(padding.Left + index * (slot + gap), padding.Top, slot, slot);
    }

    private void RecalculateLayout()
    {
        var count = Math.Max(1, _config.Items.Count);
        if (_hoverIndex >= _config.Items.Count)
        {
            _hoverIndex = -1;
        }
        if (_pressedIndex >= _config.Items.Count)
        {
            _pressedIndex = -1;
        }
        EnsureScaleBuffers();
        SnapHoverScales();
        var slot = GetSlotSize();
        var gap = _config.Dock.IconGap;
        var padding = GetContentPadding();
        var width = IsVertical() ? slot + padding.Left + padding.Right : count * slot + Math.Max(0, count - 1) * gap + padding.Left + padding.Right;
        var height = IsVertical() ? count * slot + Math.Max(0, count - 1) * gap + padding.Top + padding.Bottom : slot + padding.Top + padding.Bottom;
        Size = new Size(width, height);
        Bounds = _layoutService.GetDockBounds(_config.Dock, Size);
        Region = new Region(new Rectangle(0, 0, Width, Height));
    }

    private int GetSlotSize()
    {
        return Math.Max(_config.Dock.IconSize + 12, (int)Math.Ceiling(_config.Dock.IconSize * _config.Dock.Magnification) + 8);
    }

    private Padding GetContentPadding()
    {
        if (IsVertical())
        {
            return new Padding(10, 14, 10, 14);
        }

        return _config.Dock.Position == "top"
            ? new Padding(14, 10, 14, 34)
            : new Padding(14, 34, 14, 10);
    }

    private Rectangle GetDockBackgroundBounds()
    {
        var padding = GetContentPadding();
        if (IsVertical())
        {
            return new Rectangle(1, 1, Width - 2, Height - 2);
        }

        var slot = GetSlotSize();
        var top = Math.Max(1, padding.Top - 9);
        return new Rectangle(1, top, Width - 2, Math.Min(Height - top - 1, slot + 18));
    }

    private double GetHoverScale(int index)
    {
        EnsureScaleBuffers();
        if (index < 0 || index >= _visualScales.Length)
        {
            return 1.0;
        }

        return _visualScales[index];
    }

    private double GetTargetHoverScale(int index)
    {
        if (_hoverIndex < 0 || !_hoverPoint.HasValue || index < 0 || index >= _config.Items.Count || _config.Items[index].IsSeparator)
        {
            return 1.0;
        }

        var rect = GetItemRect(index);
        var cursorAxis = IsVertical() ? _hoverPoint.Value.Y : _hoverPoint.Value.X;
        var centerAxis = IsVertical() ? rect.Top + (rect.Height / 2.0) : rect.Left + (rect.Width / 2.0);
        var unit = Math.Max(1, GetSlotSize() + _config.Dock.IconGap);
        var normalizedDistance = Math.Abs(cursorAxis - centerAxis) / unit;
        const double influenceRadius = 1.65;
        if (normalizedDistance >= influenceRadius)
        {
            return 1.0;
        }

        var weight = 0.5 + (0.5 * Math.Cos(Math.PI * normalizedDistance / influenceRadius));
        return 1 + ((_config.Dock.Magnification - 1) * weight);
    }

    private void EnsureScaleBuffers()
    {
        var count = _config.Items.Count;
        if (_visualScales.Length == count)
        {
            return;
        }

        var visual = new double[count];
        var start = new double[count];
        var target = new double[count];
        Array.Fill(visual, 1.0);
        Array.Fill(start, 1.0);
        Array.Fill(target, 1.0);

        var copy = Math.Min(count, _visualScales.Length);
        for (var i = 0; i < copy; i++)
        {
            visual[i] = _visualScales[i];
            start[i] = _visualScales[i];
            target[i] = i < _scaleTarget.Length ? _scaleTarget[i] : GetTargetHoverScale(i);
        }

        _visualScales = visual;
        _scaleStart = start;
        _scaleTarget = target;
    }

    private void SnapHoverScales()
    {
        EnsureScaleBuffers();
        for (var i = 0; i < _visualScales.Length; i++)
        {
            var scale = GetTargetHoverScale(i);
            _visualScales[i] = scale;
            _scaleStart[i] = scale;
            _scaleTarget[i] = scale;
        }
    }

    private void StartHoverAnimation()
    {
        EnsureScaleBuffers();
        for (var i = 0; i < _visualScales.Length; i++)
        {
            _scaleStart[i] = _visualScales[i];
            _scaleTarget[i] = GetTargetHoverScale(i);
        }

        _hoverAnimationClock.Restart();
        _hoverAnimationTimer.Stop();
        _hoverAnimationTimer.Start();
    }

    private void StepHoverAnimation()
    {
        var progress = Math.Clamp(_hoverAnimationClock.ElapsedMilliseconds / (double)HoverAnimationDurationMs, 0, 1);
        var eased = EaseOutCubic(progress);
        for (var i = 0; i < _visualScales.Length; i++)
        {
            _visualScales[i] = _scaleStart[i] + ((_scaleTarget[i] - _scaleStart[i]) * eased);
        }

        RenderLayeredDock();
        if (progress >= 1)
        {
            _hoverAnimationTimer.Stop();
            _hoverAnimationClock.Reset();
            for (var i = 0; i < _visualScales.Length; i++)
            {
                _visualScales[i] = _scaleTarget[i];
            }
        }
    }

    private static double EaseOutCubic(double value)
    {
        var t = 1 - Math.Clamp(value, 0, 1);
        return 1 - (t * t * t);
    }

    private void OnMouseMove(object? sender, MouseEventArgs e)
    {
        var hoverIndex = HitTestForHover(e.Location);
        SetHover(hoverIndex, hoverIndex >= 0 ? e.Location : null);
        var index = HitTest(e.Location);
        if (_config.Dock.EnableDragReorder && e.Button == MouseButtons.Left && _pressedIndex >= 0 && index >= 0 && index != _pressedIndex)
        {
            var item = _config.Items[_pressedIndex];
            _config.Items.RemoveAt(_pressedIndex);
            _config.Items.Insert(index, item);
            _pressedIndex = index;
            ApplySettings();
        }
    }

    private void OnMouseDown(object? sender, MouseEventArgs e)
    {
        _pressedIndex = HitTest(e.Location);
        if (e.Button == MouseButtons.Right)
        {
            ShowItemMenu(_pressedIndex, e.Location);
        }
    }

    private void OnMouseUp(object? sender, MouseEventArgs e)
    {
        var index = HitTest(e.Location);
        if (e.Button == MouseButtons.Left && index >= 0 && index == _pressedIndex)
        {
            ActivateItem(_config.Items[index], GetItemRect(index));
        }
        _pressedIndex = -1;
    }

    private int HitTest(Point point)
    {
        for (var i = 0; i < _config.Items.Count; i++)
        {
            if (GetItemRect(i).Contains(point))
            {
                return i;
            }
        }
        return -1;
    }

    private int HitTestForHover(Point point)
    {
        var direct = HitTest(point);
        if (direct >= 0)
        {
            return direct;
        }

        if (!GetDockBackgroundBounds().Contains(point))
        {
            return -1;
        }

        var nearest = -1;
        var nearestDistance = double.MaxValue;
        var unit = Math.Max(1, GetSlotSize() + _config.Dock.IconGap);
        for (var i = 0; i < _config.Items.Count; i++)
        {
            if (_config.Items[i].IsSeparator)
            {
                continue;
            }

            var rect = GetItemRect(i);
            var cursorAxis = IsVertical() ? point.Y : point.X;
            var centerAxis = IsVertical() ? rect.Top + (rect.Height / 2.0) : rect.Left + (rect.Width / 2.0);
            var distance = Math.Abs(cursorAxis - centerAxis);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearest = i;
            }
        }

        return nearestDistance <= unit * 0.72 ? nearest : -1;
    }

    private void SetHover(int index, Point? point)
    {
        if (_hoverIndex == index && IsSameHoverPoint(point))
        {
            return;
        }
        _hoverIndex = index;
        _hoverPoint = index >= 0 ? point : null;
        StartHoverAnimation();
    }

    private bool IsSameHoverPoint(Point? point)
    {
        if (!_hoverPoint.HasValue && !point.HasValue)
        {
            return true;
        }

        if (!_hoverPoint.HasValue || !point.HasValue)
        {
            return false;
        }

        var dx = _hoverPoint.Value.X - point.Value.X;
        var dy = _hoverPoint.Value.Y - point.Value.Y;
        return (dx * dx) + (dy * dy) < 9;
    }

    private void ActivateItem(DockItem item, Rectangle? itemRect = null)
    {
        try
        {
            if (item.IsSeparator)
            {
                return;
            }
            if (string.Equals(item.Type, DockItemTypes.Settings, StringComparison.OrdinalIgnoreCase))
            {
                ShowSettings();
                return;
            }
            if (string.Equals(item.Type, DockItemTypes.Folder, StringComparison.OrdinalIgnoreCase))
            {
                if (ShouldOpenFolderInExplorer(item))
                {
                    OpenFolderInExplorer(item);
                }
                else
                {
                    ShowFolderFlyout(item, itemRect);
                }
                return;
            }
            if (string.Equals(item.Type, DockItemTypes.Url, StringComparison.OrdinalIgnoreCase))
            {
                Process.Start(new ProcessStartInfo(item.Target) { UseShellExecute = true });
                return;
            }
            if (string.Equals(item.Type, DockItemTypes.Shell, StringComparison.OrdinalIgnoreCase))
            {
                OpenShellTarget(item.Target);
                return;
            }
            if (File.Exists(item.Target) || Directory.Exists(item.Target))
            {
                Process.Start(new ProcessStartInfo(item.Target) { UseShellExecute = true });
            }
            else
            {
                MessageBox.Show("\u76ee\u6807\u4e0d\u5b58\u5728\uff1a\n" + item.Target, "TidyDock", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        catch (Exception ex)
        {
            _log.Error(ex);
            MessageBox.Show(ex.Message, "TidyDock", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ShowFolderFlyout(DockItem item, Rectangle? itemRect)
    {
        if (!Directory.Exists(item.Target))
        {
            MessageBox.Show("\u6587\u4ef6\u5939\u4e0d\u5b58\u5728\uff1a\n" + item.Target, "TidyDock", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _folderFlyout ??= new FolderFlyoutForm(_config, _folderService, _iconService, _log);
        var anchor = itemRect ?? GetItemRect(Math.Max(0, _config.Items.IndexOf(item)));
        _folderFlyout.ShowFor(item.Target, RectangleToScreen(anchor), _config.Dock.Position);
    }

    private static bool ShouldOpenFolderInExplorer(DockItem item)
    {
        return string.Equals(item.OpenBehavior, DockOpenBehaviors.Explorer, StringComparison.OrdinalIgnoreCase);
    }

    private void OpenFolderInExplorer(DockItem item)
    {
        try
        {
            if (Directory.Exists(item.Target))
            {
                Process.Start(new ProcessStartInfo(item.Target) { UseShellExecute = true });
            }
            else
            {
                MessageBox.Show("\u6587\u4ef6\u5939\u4e0d\u5b58\u5728\uff1a\n" + item.Target, "TidyDock", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        catch (Exception ex)
        {
            _log.Error(ex);
            MessageBox.Show(ex.Message, "TidyDock", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ShowItemMenu(int index, Point location)
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("\u8bbe\u7f6e", null, delegate { ShowSettings(); });
        menu.Items.Add("\u6dfb\u52a0\u5e94\u7528/\u6587\u4ef6", null, delegate { AddAppOrFile(); });
        menu.Items.Add("\u6dfb\u52a0\u6587\u4ef6\u5939", null, delegate { AddFolder(); });
        menu.Items.Add("\u6dfb\u52a0\u7f51\u5740", null, delegate { AddUrl(); });
        menu.Items.Add("\u6dfb\u52a0\u6b64\u7535\u8111", null, delegate { AddThisPc(); });
        if (index >= 0)
        {
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("\u6253\u5f00", null, delegate { ActivateItem(_config.Items[index], GetItemRect(index)); });
            if (string.Equals(_config.Items[index].Type, DockItemTypes.Folder, StringComparison.OrdinalIgnoreCase))
            {
                menu.Items.Add("\u5728\u8d44\u6e90\u7ba1\u7406\u5668\u4e2d\u6253\u5f00", null, delegate { OpenFolderInExplorer(_config.Items[index]); });
            }
            menu.Items.Add("\u79fb\u9664", null, delegate
            {
                _config.Items.RemoveAt(index);
                ApplySettings();
            });
        }
        menu.Show(this, location);
    }

    private ContextMenuStrip BuildTrayMenu()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add(Visible ? "\u9690\u85cf Dock" : "\u663e\u793a Dock", null, delegate { ToggleVisible(); });
        menu.Items.Add("\u8bbe\u7f6e", null, delegate { ShowSettings(); });
        menu.Items.Add("\u6253\u5f00\u914d\u7f6e\u6587\u4ef6\u5939", null, delegate { Process.Start(new ProcessStartInfo(_configService.AppDirectory) { UseShellExecute = true }); });
        menu.Items.Add("\u9000\u51fa", null, delegate { Application.Exit(); });
        return menu;
    }

    private void ShowSettings()
    {
        if (_settings == null || _settings.IsDisposed)
        {
            _settings = new SettingsForm(_config, _iconService, _startupService, ApplySettings, AddAppOrFile, AddFolder, AddUrl, AddThisPc, AddSeparator);
        }
        _settings.Show();
        _settings.Activate();
    }

    private void AddAppOrFile()
    {
        using var dialog = new OpenFileDialog { Multiselect = true, Title = "\u9009\u62e9\u8981\u6dfb\u52a0\u5230 Dock \u7684\u5e94\u7528\u6216\u6587\u4ef6", Filter = "\u7a0b\u5e8f\u548c\u6587\u4ef6|*.exe;*.lnk;*.*|\u6240\u6709\u6587\u4ef6|*.*" };
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }
        foreach (var file in dialog.FileNames)
        {
            AddPath(file);
        }
    }

    private void AddFolder()
    {
        using var dialog = new FolderBrowserDialog { Description = "\u9009\u62e9\u8981\u6dfb\u52a0\u5230 TidyDock \u7684\u6587\u4ef6\u5939" };
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            AddPath(dialog.SelectedPath);
        }
    }

    private void AddUrl()
    {
        using var dialog = new InputDialog("\u6dfb\u52a0\u7f51\u5740", "\u7f51\u5740", "https://", _palette);
        if (dialog.ShowDialog(this) != DialogResult.OK || string.IsNullOrWhiteSpace(dialog.Value))
        {
            return;
        }
        using var name = new InputDialog("\u6dfb\u52a0\u7f51\u5740", "\u540d\u79f0", dialog.Value, _palette);
        if (name.ShowDialog(this) == DialogResult.OK)
        {
            _config.Items.Add(new DockItem { Type = DockItemTypes.Url, Name = name.Value, Target = dialog.Value });
            ApplySettings();
        }
    }

    private void AddSeparator()
    {
        _config.Items.Add(new DockItem { Type = DockItemTypes.Separator, Name = "\u5206\u9694\u7b26" });
        ApplySettings();
    }

    private void AddThisPc()
    {
        if (_config.Items.Any(item => string.Equals(item.Target, ThisPcTarget, StringComparison.OrdinalIgnoreCase)))
        {
            MessageBox.Show("\u6b64\u7535\u8111\u5df2\u7ecf\u5728 Dock \u4e2d\u3002", "TidyDock", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        _config.Items.Add(new DockItem
        {
            Type = DockItemTypes.Shell,
            Name = "\u6b64\u7535\u8111",
            Target = ThisPcTarget
        });
        ApplySettings();
    }

    private static void OpenShellTarget(string target)
    {
        if (string.IsNullOrWhiteSpace(target))
        {
            return;
        }

        if (target.StartsWith("shell:", StringComparison.OrdinalIgnoreCase) || target.StartsWith("::", StringComparison.OrdinalIgnoreCase))
        {
            Process.Start(new ProcessStartInfo("explorer.exe", target) { UseShellExecute = true });
            return;
        }

        Process.Start(new ProcessStartInfo(target) { UseShellExecute = true });
    }

    private void OnDragEnter(object? sender, DragEventArgs e)
    {
        e.Effect = e.Data?.GetDataPresent(DataFormats.FileDrop) == true ? DragDropEffects.Copy : DragDropEffects.None;
    }

    private void OnDragDrop(object? sender, DragEventArgs e)
    {
        if (e.Data?.GetData(DataFormats.FileDrop) is not string[] paths)
        {
            return;
        }
        foreach (var path in paths)
        {
            AddPath(path);
        }
    }

    private void ToggleVisible()
    {
        if (Visible)
        {
            Hide();
        }
        else
        {
            Show();
            RecalculateLayout();
            RenderLayeredDock();
        }
    }

    private void StartHideTimer()
    {
        if (_config.Dock.AutoHide)
        {
            _hideTimer.Stop();
            _hideTimer.Start();
        }
    }

    private void HideForAutoHide()
    {
        Hide();
        _hotZone.Bounds = _layoutService.GetHotZoneBounds(_config.Dock);
        _hotZone.Show();
    }

    private void ShowFromAutoHide()
    {
        _hotZone.Hide();
        Show();
        RecalculateLayout();
        RenderLayeredDock();
    }

    private void UpdateHotZone()
    {
        _hotZone.Bounds = _layoutService.GetHotZoneBounds(_config.Dock);
        if (!_config.Dock.AutoHide)
        {
            _hotZone.Hide();
        }
    }

    private void ScheduleRestore()
    {
        _restoreAttempts = 0;
        _restoreTimer.Stop();
        _restoreTimer.Start();
    }

    private void RestoreAfterShowDesktop()
    {
        _restoreAttempts++;
        if (WindowState == FormWindowState.Minimized)
        {
            WindowState = FormWindowState.Normal;
        }
        if (!Visible && !_config.Dock.AutoHide)
        {
            Show();
        }
        RecalculateLayout();
        RenderLayeredDock();
        if (_restoreAttempts >= 4)
        {
            _restoreTimer.Stop();
        }
    }

    private bool IsVertical()
    {
        return _config.Dock.Position is "left" or "right";
    }

    private static GraphicsPath RoundedRect(Rectangle bounds, int radius)
    {
        var path = new GraphicsPath();
        var d = radius * 2;
        path.AddArc(bounds.Left, bounds.Top, d, d, 180, 90);
        path.AddArc(bounds.Right - d, bounds.Top, d, d, 270, 90);
        path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
        path.AddArc(bounds.Left, bounds.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }

    private static Color WithAlpha(Color color, double opacity)
    {
        opacity = Math.Clamp(opacity, 0, 1);
        return Color.FromArgb((int)(opacity * 255), color);
    }

    private static Icon LoadAppIcon()
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "TidyDock.ico"),
            Path.Combine(AppContext.BaseDirectory, "assets", "TidyDock.ico")
        };

        foreach (var path in candidates)
        {
            if (File.Exists(path))
            {
                return new Icon(path);
            }
        }

        return Icon.ExtractAssociatedIcon(Application.ExecutablePath) ?? (Icon)SystemIcons.Application.Clone();
    }
}
