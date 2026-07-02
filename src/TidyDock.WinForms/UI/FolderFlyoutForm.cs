using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using TidyDock.WinForms.Core;
using TidyDock.WinForms.Services;
using TidyDock.WinForms.Shell;

namespace TidyDock.WinForms.UI;

internal sealed class FolderFlyoutForm : Form
{
    private readonly AppConfig _config;
    private readonly FolderService _folderService;
    private readonly IconService _iconService;
    private readonly LogService _log;
    private readonly System.Windows.Forms.Timer _closeTimer = new() { Interval = 650 };
    private readonly ToolTip _nameToolTip = new()
    {
        AutoPopDelay = 6000,
        InitialDelay = 350,
        ReshowDelay = 100,
        ShowAlways = true
    };
    private readonly Stack<string> _history = new();
    private readonly List<EntryHit> _entryHits = [];
    private ThemePalette _palette;
    private Rectangle _backRect;
    private Rectangle _explorerRect;
    private Rectangle _closeRect;
    private string _currentPath = "";
    private FolderReadResult? _result;
    private bool _loading;
    private int _loadVersion;
    private int _hoverIndex = -1;
    private int _scrollOffset;

    public FolderFlyoutForm(AppConfig config, FolderService folderService, IconService iconService, LogService log)
    {
        _config = config;
        _folderService = folderService;
        _iconService = iconService;
        _log = log;
        _palette = Theme.Get(_config.Dock.Theme);

        Text = "TidyDock";
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        DoubleBuffered = true;
        KeyPreview = true;
        Width = 430;
        Height = 360;

        MouseMove += OnMouseMove;
        MouseWheel += OnMouseWheel;
        MouseLeave += delegate { _nameToolTip.Hide(this); StartCloseTimer(); };
        MouseEnter += delegate { _closeTimer.Stop(); };
        MouseDown += OnMouseDown;
        Deactivate += delegate { StartCloseTimer(); };
        KeyDown += delegate(object? _, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                Hide();
            }
        };
        _closeTimer.Tick += delegate
        {
            _closeTimer.Stop();
            Hide();
        };
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

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _closeTimer.Dispose();
            _nameToolTip.Dispose();
        }
        base.Dispose(disposing);
    }

    public void ShowFor(string path, Rectangle anchorScreen, string dockPosition)
    {
        _palette = Theme.Get(_config.Dock.Theme);
        _history.Clear();
        _currentPath = path;
        _result = null;
        _loading = true;
        _scrollOffset = 0;
        _hoverIndex = -1;
        _nameToolTip.Hide(this);
        PositionNear(anchorScreen, dockPosition);
        Show();
        BringToFront();
        RenderLayeredFlyout();
        LoadFolder(path, addHistory: false);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Draw(e.Graphics);
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
    }

    private async void LoadFolder(string path, bool addHistory)
    {
        var version = ++_loadVersion;
        if (addHistory && !string.IsNullOrWhiteSpace(_currentPath))
        {
            _history.Push(_currentPath);
        }
        _currentPath = path;
        _result = null;
        _loading = true;
        _hoverIndex = -1;
        _nameToolTip.Hide(this);
        RenderLayeredFlyout();

        FolderReadResult result;
        try
        {
            result = await Task.Run(() => _folderService.Read(path, _config.FolderPanel));
        }
        catch (Exception ex)
        {
            _log.Error(ex);
            result = new FolderReadResult { ErrorMessage = ex.Message };
        }

        if (IsDisposed || version != _loadVersion)
        {
            return;
        }

        _loading = false;
        _result = result;
        RenderLayeredFlyout();
    }

    private void PositionNear(Rectangle anchor, string dockPosition)
    {
        var maxHeight = Math.Clamp(_config.FolderPanel.MaxHeight, 240, 760);
        Size = new Size(430, maxHeight);

        var screen = Screen.FromRectangle(anchor).WorkingArea;
        var x = anchor.Left + (anchor.Width - Width) / 2;
        var y = anchor.Top - Height - 10;

        if (dockPosition == "top")
        {
            y = anchor.Bottom + 10;
        }
        else if (dockPosition == "left")
        {
            x = anchor.Right + 10;
            y = anchor.Top + (anchor.Height - Height) / 2;
        }
        else if (dockPosition == "right")
        {
            x = anchor.Left - Width - 10;
            y = anchor.Top + (anchor.Height - Height) / 2;
        }

        x = Math.Clamp(x, screen.Left + 8, screen.Right - Width - 8);
        y = Math.Clamp(y, screen.Top + 8, screen.Bottom - Height - 8);
        Location = new Point(x, y);
    }

    private void RenderLayeredFlyout()
    {
        if (!IsHandleCreated || Width <= 0 || Height <= 0)
        {
            return;
        }

        using var bitmap = new Bitmap(Width, Height, PixelFormat.Format32bppPArgb);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            Draw(graphics);
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

    private void Draw(Graphics graphics)
    {
        graphics.Clear(Color.Transparent);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
        _entryHits.Clear();

        var bounds = new Rectangle(1, 1, Width - 2, Height - 2);
        using var shadowPath = RoundedRect(new Rectangle(bounds.X + 4, bounds.Y + 8, bounds.Width - 8, bounds.Height - 7), 18);
        using var shadow = new SolidBrush(Color.FromArgb(_palette.IsDark ? 86 : 44, _palette.Shadow));
        graphics.FillPath(shadow, shadowPath);

        using var path = RoundedRect(bounds, 18);
        using var fill = new LinearGradientBrush(bounds, WithAlpha(_palette.Panel, _palette.IsDark ? 0.92 : 0.9), WithAlpha(_palette.PanelAlt, _palette.IsDark ? 0.82 : 0.86), 90f);
        using var border = new Pen(WithAlpha(_palette.Border, _palette.IsDark ? 0.74 : 0.86));
        graphics.FillPath(fill, path);
        graphics.DrawPath(border, path);

        DrawHeader(graphics);
        DrawContent(graphics);
    }

    private void DrawHeader(Graphics graphics)
    {
        var header = new Rectangle(14, 12, Width - 28, 42);
        using var line = new Pen(WithAlpha(_palette.Border, 0.55));
        graphics.DrawLine(line, header.Left, header.Bottom, header.Right, header.Bottom);

        _backRect = new Rectangle(header.Left, header.Top + 6, 30, 28);
        _explorerRect = new Rectangle(header.Right - 68, header.Top + 6, 30, 28);
        _closeRect = new Rectangle(header.Right - 32, header.Top + 6, 30, 28);
        DrawIconButton(graphics, _backRect, "<", _history.Count > 0);
        DrawIconButton(graphics, _explorerRect, "\u2197", true);
        DrawIconButton(graphics, _closeRect, "x", true);

        using var titleFont = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold, GraphicsUnit.Point);
        using var text = new SolidBrush(_palette.Text);
        using var muted = new SolidBrush(_palette.MutedText);
        using var titleFormat = new StringFormat { Trimming = StringTrimming.EllipsisCharacter, LineAlignment = StringAlignment.Center };
        var titleRect = new Rectangle(_backRect.Right + 10, header.Top, _explorerRect.Left - _backRect.Right - 18, 26);
        graphics.DrawString(GetTitle(), titleFont, text, titleRect, titleFormat);
        using var pathFont = new Font("Microsoft YaHei UI", 7.8F, FontStyle.Regular, GraphicsUnit.Point);
        var pathRect = new Rectangle(titleRect.Left, header.Top + 22, titleRect.Width, 18);
        graphics.DrawString(_currentPath, pathFont, muted, pathRect, titleFormat);
    }

    private void DrawContent(Graphics graphics)
    {
        var content = new Rectangle(18, 66, Width - 36, Height - 84);
        if (_loading)
        {
            DrawStatus(graphics, content, "\u6b63\u5728\u8bfb\u53d6...");
            return;
        }

        if (_result == null)
        {
            DrawStatus(graphics, content, "");
            return;
        }

        if (!string.IsNullOrWhiteSpace(_result.ErrorMessage))
        {
            DrawStatus(graphics, content, _result.ErrorMessage);
            return;
        }

        if (_result.Entries.Count == 0)
        {
            DrawStatus(graphics, content, "\u6587\u4ef6\u5939\u4e3a\u7a7a");
            return;
        }

        var cellWidth = 92;
        var cellHeight = 88;
        var columns = Math.Max(1, content.Width / cellWidth);
        var xPad = (content.Width - columns * cellWidth) / 2;
        var rows = (int)Math.Ceiling(_result.Entries.Count / (double)columns);
        var bottomReserved = _result.IsTruncated ? 34 : 18;
        var viewport = new Rectangle(content.Left, content.Top, content.Width - 10, Math.Max(1, content.Height - bottomReserved));
        var maxScroll = Math.Max(0, rows * cellHeight - viewport.Height);
        _scrollOffset = Math.Clamp(_scrollOffset, 0, maxScroll);
        using var nameFont = new Font("Microsoft YaHei UI", 8.2F, FontStyle.Regular, GraphicsUnit.Point);
        using var text = new SolidBrush(_palette.Text);
        using var muted = new SolidBrush(_palette.MutedText);

        var state = graphics.Save();
        graphics.SetClip(viewport, CombineMode.Replace);
        for (var i = 0; i < _result.Entries.Count; i++)
        {
            var row = i / columns;
            var col = i % columns;
            var rect = new Rectangle(content.Left + xPad + col * cellWidth, content.Top + row * cellHeight - _scrollOffset, cellWidth - 8, cellHeight - 6);
            if (rect.Bottom <= viewport.Top)
            {
                continue;
            }
            if (rect.Top >= viewport.Bottom)
            {
                break;
            }

            var entry = _result.Entries[i];
            var hitBounds = Rectangle.Intersect(rect, viewport);
            if (!hitBounds.IsEmpty)
            {
                _entryHits.Add(new EntryHit(i, hitBounds, entry));
            }
            if (i == _hoverIndex)
            {
                using var hoverPath = RoundedRect(rect, 10);
                using var hover = new SolidBrush(WithAlpha(_palette.PanelAlt, _palette.IsDark ? 0.5 : 0.7));
                graphics.FillPath(hover, hoverPath);
            }

            var icon = _iconService.GetIcon(_result.Entries[i], 40);
            var iconRect = new Rectangle(rect.Left + (rect.Width - 40) / 2, rect.Top + 8, 40, 40);
            graphics.DrawImage(icon, iconRect);

            var nameRect = new Rectangle(rect.Left + 4, rect.Top + 55, rect.Width - 8, 20);
            using var nameFormat = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center,
                Trimming = StringTrimming.EllipsisCharacter,
                FormatFlags = StringFormatFlags.NoWrap
            };
            graphics.DrawString(entry.Name, nameFont, text, nameRect, nameFormat);
        }
        graphics.Restore(state);

        if (_result.IsTruncated)
        {
            var hint = string.Format("\u4ec5\u663e\u793a\u524d {0} \u9879\uff0c\u53ef\u5728\u8d44\u6e90\u7ba1\u7406\u5668\u4e2d\u67e5\u770b\u5168\u90e8", _config.FolderPanel.MaxItems);
            graphics.DrawString(hint, nameFont, muted, new Rectangle(content.Left + 4, content.Bottom - 20, content.Width - 8, 18));
        }

        if (maxScroll > 0)
        {
            var track = new Rectangle(content.Right - 4, viewport.Top + 2, 3, viewport.Height - 4);
            var thumbHeight = Math.Max(24, (int)(track.Height * (viewport.Height / (double)(rows * cellHeight))));
            var thumbTop = track.Top + (int)((track.Height - thumbHeight) * (_scrollOffset / (double)maxScroll));
            using var scroll = new SolidBrush(WithAlpha(_palette.MutedText, 0.45));
            graphics.FillRectangle(scroll, new Rectangle(track.Left, thumbTop, track.Width, thumbHeight));
        }
    }

    private void DrawIconButton(Graphics graphics, Rectangle rect, string text, bool enabled)
    {
        using var path = RoundedRect(rect, 8);
        using var fill = new SolidBrush(WithAlpha(_palette.PanelAlt, enabled ? 0.82 : 0.35));
        using var border = new Pen(WithAlpha(_palette.Border, 0.65));
        graphics.FillPath(fill, path);
        graphics.DrawPath(border, path);
        using var font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point);
        using var brush = new SolidBrush(enabled ? _palette.Text : _palette.MutedText);
        using var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        graphics.DrawString(text, font, brush, rect, format);
    }

    private void DrawStatus(Graphics graphics, Rectangle bounds, string text)
    {
        using var font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
        using var brush = new SolidBrush(_palette.MutedText);
        using var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter };
        graphics.DrawString(text, font, brush, bounds, format);
    }

    private void OnMouseMove(object? sender, MouseEventArgs e)
    {
        _closeTimer.Stop();
        var hit = _entryHits.FirstOrDefault(hit => hit.Bounds.Contains(e.Location));
        var index = hit?.Index ?? -1;
        if (_hoverIndex != index)
        {
            _hoverIndex = index;
            _nameToolTip.Hide(this);
            if (hit?.Entry is not null)
            {
                _nameToolTip.Show(hit.Entry.Name, this, e.Location.X + 12, e.Location.Y + 18);
            }
            RenderLayeredFlyout();
        }
    }

    private void OnMouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left)
        {
            return;
        }

        if (_closeRect.Contains(e.Location))
        {
            Hide();
            return;
        }

        if (_explorerRect.Contains(e.Location))
        {
            OpenPath(_currentPath);
            return;
        }

        if (_backRect.Contains(e.Location) && _history.Count > 0)
        {
            LoadFolder(_history.Pop(), addHistory: false);
            return;
        }

        var hit = _entryHits.FirstOrDefault(item => item.Bounds.Contains(e.Location));
        if (hit == null)
        {
            return;
        }

        if (hit.Entry.IsDirectory)
        {
            LoadFolder(hit.Entry.Path, addHistory: true);
            return;
        }

        OpenPath(hit.Entry.Path);
    }

    private void OpenPath(string path)
    {
        try
        {
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            _log.Error(ex);
            _result = new FolderReadResult { ErrorMessage = ex.Message };
            _loading = false;
            RenderLayeredFlyout();
        }
    }

    private void OnMouseWheel(object? sender, MouseEventArgs e)
    {
        _nameToolTip.Hide(this);
        _scrollOffset = Math.Max(0, _scrollOffset - Math.Sign(e.Delta) * 80);
        RenderLayeredFlyout();
    }

    private void StartCloseTimer()
    {
        _closeTimer.Stop();
        _closeTimer.Start();
    }

    private string GetTitle()
    {
        var name = Path.GetFileName(_currentPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        return string.IsNullOrWhiteSpace(name) ? _currentPath : name;
    }

    private static GraphicsPath RoundedRect(Rectangle bounds, int radius)
    {
        var path = new GraphicsPath();
        var d = Math.Max(1, radius * 2);
        path.AddArc(bounds.Left, bounds.Top, d, d, 180, 90);
        path.AddArc(bounds.Right - d, bounds.Top, d, d, 270, 90);
        path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
        path.AddArc(bounds.Left, bounds.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }

    private static Color WithAlpha(Color color, double opacity)
    {
        return Color.FromArgb((int)(Math.Clamp(opacity, 0, 1) * 255), color);
    }

    private sealed record EntryHit(int Index, Rectangle Bounds, FolderEntry Entry);
}
