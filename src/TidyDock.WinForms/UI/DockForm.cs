using System.Diagnostics;
using System.Drawing.Drawing2D;
using TidyDock.WinForms.Core;
using TidyDock.WinForms.Services;
using TidyDock.WinForms.Shell;

namespace TidyDock.WinForms.UI;

internal sealed class DockForm : Form
{
    private readonly AppConfig _config;
    private readonly ConfigService _configService;
    private readonly IconService _iconService;
    private readonly ShortcutService _shortcutService;
    private readonly StartupService _startupService;
    private readonly DockLayoutService _layoutService;
    private readonly LogService _log;
    private readonly NotifyIcon _tray;
    private readonly System.Windows.Forms.Timer _hideTimer = new() { Interval = 650 };
    private readonly System.Windows.Forms.Timer _restoreTimer = new() { Interval = 220 };
    private readonly HotZoneForm _hotZone = new();
    private readonly List<Rectangle> _itemRects = [];
    private ThemePalette _palette;
    private int _hoverIndex = -1;
    private int _pressedIndex = -1;
    private SettingsForm? _settings;
    private int _restoreAttempts;

    public DockForm(
        AppConfig config,
        ConfigService configService,
        IconService iconService,
        ShortcutService shortcutService,
        StartupService startupService,
        DockLayoutService layoutService,
        LogService log)
    {
        _config = config;
        _configService = configService;
        _iconService = iconService;
        _shortcutService = shortcutService;
        _startupService = startupService;
        _layoutService = layoutService;
        _log = log;
        _palette = Theme.Get(_config.Dock.Theme);

        Text = "TidyDock";
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        DoubleBuffered = true;
        AllowDrop = true;
        BackColor = Color.Magenta;
        TransparencyKey = Color.Magenta;

        _tray = new NotifyIcon
        {
            Text = "TidyDock",
            Icon = LoadAppIcon(),
            Visible = _config.Dock.ShowTrayIcon,
            ContextMenuStrip = BuildTrayMenu()
        };
        _tray.DoubleClick += delegate { ToggleVisible(); };

        DragEnter += OnDragEnter;
        DragDrop += OnDragDrop;
        MouseMove += OnMouseMove;
        MouseLeave += delegate { SetHover(-1); StartHideTimer(); };
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
            cp.ExStyle &= ~NativeMethods.WsExAppWindow;
            return cp;
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _tray.Dispose();
            _hideTimer.Dispose();
            _restoreTimer.Dispose();
            _hotZone.Dispose();
            _settings?.Dispose();
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
        Invalidate();
    }

    public void AddPath(string path)
    {
        try
        {
            var imported = _shortcutService.ImportIfShortcut(path);
            var item = new DockItem
            {
                Target = imported,
                Name = Path.GetFileNameWithoutExtension(path),
                Type = Directory.Exists(path) ? DockItemTypes.Folder : DockItemTypes.File
            };

            if (File.Exists(path) && string.Equals(Path.GetExtension(path), ".exe", StringComparison.OrdinalIgnoreCase))
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
        DrawDockBackground(e.Graphics);
        DrawItems(e.Graphics);
    }

    private void DrawDockBackground(Graphics graphics)
    {
        var bounds = new Rectangle(1, 1, Width - 2, Height - 2);
        using var shadow = RoundedRect(new Rectangle(bounds.X + 2, bounds.Y + 5, bounds.Width - 4, bounds.Height - 3), _config.Dock.CornerRadius);
        using var shadowBrush = new SolidBrush(Color.FromArgb(_palette.IsDark ? 90 : 42, _palette.Shadow));
        graphics.FillPath(shadowBrush, shadow);

        using var path = RoundedRect(bounds, _config.Dock.CornerRadius);
        using var brush = new LinearGradientBrush(bounds, WithAlpha(_palette.DockTop, _config.Dock.Opacity * 0.9), WithAlpha(_palette.DockBottom, _config.Dock.Opacity), IsVertical() ? 0f : 90f);
        using var pen = new Pen(WithAlpha(_palette.Border, _palette.IsDark ? 0.58 : 0.78));
        graphics.FillPath(brush, path);
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

            var active = i == _hoverIndex;
            var scale = active ? _config.Dock.Magnification : 1.0;
            var iconSize = (int)(size * scale);
            var iconRect = new Rectangle(rect.Left + (rect.Width - iconSize) / 2, rect.Top + (rect.Height - iconSize) / 2, iconSize, iconSize);

            if (active)
            {
                using var glowPath = RoundedRect(iconRect, Math.Max(10, iconSize / 5));
                using var glow = new LinearGradientBrush(iconRect, WithAlpha(Color.White, _palette.IsDark ? 0.18 : 0.58), WithAlpha(_palette.PanelAlt, _palette.IsDark ? 0.12 : 0.38), 90f);
                graphics.FillPath(glow, glowPath);
            }

            var image = _iconService.GetIcon(item, size);
            graphics.DrawImage(image, iconRect);
        }
    }

    private void DrawSeparator(Graphics graphics, Rectangle rect)
    {
        using var pen = new Pen(WithAlpha(_palette.MutedText, 0.45), 1);
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
        var padX = IsVertical() ? 10 : 14;
        var padY = IsVertical() ? 14 : 10;
        var slot = _config.Dock.IconSize + 8;
        var gap = _config.Dock.IconGap;
        if (IsVertical())
        {
            return new Rectangle(padX, padY + index * (slot + gap), slot, slot);
        }
        return new Rectangle(padX + index * (slot + gap), padY, slot, slot);
    }

    private void RecalculateLayout()
    {
        var count = Math.Max(1, _config.Items.Count);
        var slot = _config.Dock.IconSize + 8;
        var gap = _config.Dock.IconGap;
        var padX = IsVertical() ? 10 : 14;
        var padY = IsVertical() ? 14 : 10;
        var width = IsVertical() ? slot + padX * 2 : count * slot + Math.Max(0, count - 1) * gap + padX * 2;
        var height = IsVertical() ? count * slot + Math.Max(0, count - 1) * gap + padY * 2 : slot + padY * 2;
        Size = new Size(width, height);
        Bounds = _layoutService.GetDockBounds(_config.Dock, Size);
        Region = new Region(RoundedRect(new Rectangle(0, 0, Width, Height), _config.Dock.CornerRadius + 2));
    }

    private void OnMouseMove(object? sender, MouseEventArgs e)
    {
        var index = HitTest(e.Location);
        SetHover(index);
        if (e.Button == MouseButtons.Left && _pressedIndex >= 0 && index >= 0 && index != _pressedIndex)
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
            ActivateItem(_config.Items[index]);
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

    private void SetHover(int index)
    {
        if (_hoverIndex == index)
        {
            return;
        }
        _hoverIndex = index;
        Invalidate();
    }

    private void ActivateItem(DockItem item)
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
            if (string.Equals(item.Type, DockItemTypes.Url, StringComparison.OrdinalIgnoreCase))
            {
                Process.Start(new ProcessStartInfo(item.Target) { UseShellExecute = true });
                return;
            }
            if (File.Exists(item.Target) || Directory.Exists(item.Target))
            {
                Process.Start(new ProcessStartInfo(item.Target) { UseShellExecute = true });
            }
            else
            {
                MessageBox.Show("Target does not exist:\n" + item.Target, "TidyDock", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
        menu.Items.Add("Settings", null, delegate { ShowSettings(); });
        menu.Items.Add("Add app/file", null, delegate { AddAppOrFile(); });
        menu.Items.Add("Add folder", null, delegate { AddFolder(); });
        menu.Items.Add("Add URL", null, delegate { AddUrl(); });
        if (index >= 0)
        {
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Open", null, delegate { ActivateItem(_config.Items[index]); });
            menu.Items.Add("Remove", null, delegate
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
        menu.Items.Add(Visible ? "Hide Dock" : "Show Dock", null, delegate { ToggleVisible(); });
        menu.Items.Add("Settings", null, delegate { ShowSettings(); });
        menu.Items.Add("Open config folder", null, delegate { Process.Start(new ProcessStartInfo(_configService.AppDirectory) { UseShellExecute = true }); });
        menu.Items.Add("Exit", null, delegate { Application.Exit(); });
        return menu;
    }

    private void ShowSettings()
    {
        if (_settings == null || _settings.IsDisposed)
        {
            _settings = new SettingsForm(_config, _startupService, ApplySettings, AddAppOrFile, AddFolder, AddUrl, AddSeparator);
        }
        _settings.Show();
        _settings.Activate();
    }

    private void AddAppOrFile()
    {
        using var dialog = new OpenFileDialog { Multiselect = true, Filter = "Programs and files|*.exe;*.lnk;*.*|All files|*.*" };
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
        using var dialog = new FolderBrowserDialog { Description = "Select folder for TidyDock" };
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            AddPath(dialog.SelectedPath);
        }
    }

    private void AddUrl()
    {
        using var dialog = new InputDialog("Add URL", "URL", "https://", _palette);
        if (dialog.ShowDialog(this) != DialogResult.OK || string.IsNullOrWhiteSpace(dialog.Value))
        {
            return;
        }
        using var name = new InputDialog("Add URL", "Name", dialog.Value, _palette);
        if (name.ShowDialog(this) == DialogResult.OK)
        {
            _config.Items.Add(new DockItem { Type = DockItemTypes.Url, Name = name.Value, Target = dialog.Value });
            ApplySettings();
        }
    }

    private void AddSeparator()
    {
        _config.Items.Add(new DockItem { Type = DockItemTypes.Separator, Name = "Separator" });
        ApplySettings();
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
        var path = Path.Combine(AppContext.BaseDirectory, "assets", "TidyDock.ico");
        return File.Exists(path) ? new Icon(path) : SystemIcons.Application;
    }
}
