using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using TidyDock.WinForms.Core;
using TidyDock.WinForms.Services;
using TidyDock.WinForms.Shell;

namespace TidyDock.WinForms.UI;

internal sealed class FolderFlyoutForm : Form
{
    private const int AnimationDurationMs = 140;
    private const int AnimationOffset = 10;
    private const int GroupHeaderHeight = 30;
    private const int GroupGap = 12;

    private readonly AppConfig _config;
    private readonly FolderService _folderService;
    private readonly IconService _iconService;
    private readonly LogService _log;
    private readonly System.Windows.Forms.Timer _closeTimer = new() { Interval = 650 };
    private readonly System.Windows.Forms.Timer _animationTimer = new() { Interval = 16 };
    private readonly Stopwatch _animationClock = new();
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
    private DockItem? _rootItem;
    private string _rootPath = "";
    private string _currentPath = "";
    private FolderReadResult? _result;
    private bool _loading;
    private int _loadVersion;
    private int _hoverIndex = -1;
    private int _scrollOffset;
    private string _shownToolTipText = "";
    private FlyoutAnimationState _animationState = FlyoutAnimationState.None;
    private Point _baseLocation;
    private Point _animationStartOffset;
    private Point _animationEndOffset;
    private Point _animationCurrentOffset;
    private Point _closeOffset = new(0, AnimationOffset);
    private byte _animationStartAlpha;
    private byte _animationAlpha = 255;

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
        MouseLeave += delegate { Cursor = Cursors.Default; HideToolTip(); StartCloseTimer(); };
        MouseEnter += delegate { _closeTimer.Stop(); };
        MouseDown += OnMouseDown;
        Deactivate += delegate { StartCloseTimer(); };
        KeyDown += delegate(object? _, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                CloseWithAnimation();
            }
        };
        _closeTimer.Tick += delegate
        {
            _closeTimer.Stop();
            CloseWithAnimation();
        };
        _animationTimer.Tick += delegate { StepAnimation(); };
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
            _animationTimer.Dispose();
            _animationClock.Stop();
            _nameToolTip.Dispose();
        }
        base.Dispose(disposing);
    }

    public void ShowFor(DockItem item, Rectangle anchorScreen, string dockPosition)
    {
        _palette = Theme.Get(_config.Dock.Theme);
        _rootItem = item;
        _rootPath = item.Target;
        _history.Clear();
        _currentPath = item.Target;
        _result = null;
        _loading = true;
        _scrollOffset = 0;
        _hoverIndex = -1;
        HideToolTip();
        PositionNear(anchorScreen, dockPosition);
        StartOpenAnimation(dockPosition);
        Show();
        BringToFront();
        RenderLayeredFlyout();
        LoadFolder(item.Target, addHistory: false);
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
        _scrollOffset = 0;
        _hoverIndex = -1;
        HideToolTip();
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
            var topPos = new NativeMethods.PointNative(
                _baseLocation.X + _animationCurrentOffset.X,
                _baseLocation.Y + _animationCurrentOffset.Y);
            var size = new NativeMethods.SizeNative(Width, Height);
            var source = new NativeMethods.PointNative(0, 0);
            var blend = new NativeMethods.BlendFunction
            {
                BlendOp = NativeMethods.AcSrcOver,
                BlendFlags = 0,
                SourceConstantAlpha = _animationAlpha,
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
        graphics.DrawString(GetHeaderSubtitle(), pathFont, muted, pathRect, titleFormat);
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

        if (ShouldShowGroupedSections())
        {
            DrawGroupedEntries(graphics, content, _result.Entries);
            return;
        }

        DrawEntryGrid(graphics, content, _result.Entries);
    }

    private void DrawEntryGrid(Graphics graphics, Rectangle content, IReadOnlyList<FolderEntry> entries)
    {
        var cellWidth = 92;
        var cellHeight = 88;
        var columns = Math.Max(1, content.Width / cellWidth);
        var xPad = (content.Width - columns * cellWidth) / 2;
        var rows = (int)Math.Ceiling(entries.Count / (double)columns);
        var bottomReserved = _result?.IsTruncated == true ? 34 : 18;
        var viewport = new Rectangle(content.Left, content.Top, content.Width - 10, Math.Max(1, content.Height - bottomReserved));
        var maxScroll = Math.Max(0, rows * cellHeight - viewport.Height);
        _scrollOffset = Math.Clamp(_scrollOffset, 0, maxScroll);
        using var nameFont = new Font("Microsoft YaHei UI", 8.2F, FontStyle.Regular, GraphicsUnit.Point);
        using var text = new SolidBrush(_palette.Text);
        using var muted = new SolidBrush(_palette.MutedText);

        var state = graphics.Save();
        graphics.SetClip(viewport, CombineMode.Replace);
        DrawEntryCells(graphics, entries, viewport, content.Left + xPad, content.Top - _scrollOffset, cellWidth, cellHeight, 0);
        graphics.Restore(state);

        DrawTruncatedHint(graphics, content, nameFont, muted);
        DrawScrollBar(graphics, content, viewport, maxScroll, rows * cellHeight);
    }

    private void DrawGroupedEntries(Graphics graphics, Rectangle content, IReadOnlyList<FolderEntry> entries)
    {
        var sections = BuildGroupSections(entries);
        var cellWidth = 92;
        var cellHeight = 88;
        var columns = Math.Max(1, content.Width / cellWidth);
        var xPad = (content.Width - columns * cellWidth) / 2;
        var bottomReserved = _result?.IsTruncated == true ? 34 : 18;
        var viewport = new Rectangle(content.Left, content.Top, content.Width - 10, Math.Max(1, content.Height - bottomReserved));
        var totalHeight = sections.Sum(section => GroupHeaderHeight + ((int)Math.Ceiling(section.Entries.Count / (double)columns) * cellHeight) + GroupGap);
        var maxScroll = Math.Max(0, totalHeight - viewport.Height);
        _scrollOffset = Math.Clamp(_scrollOffset, 0, maxScroll);

        using var nameFont = new Font("Microsoft YaHei UI", 8.2F, FontStyle.Regular, GraphicsUnit.Point);
        using var headerFont = new Font("Microsoft YaHei UI", 8.8F, FontStyle.Bold, GraphicsUnit.Point);
        using var text = new SolidBrush(_palette.Text);
        using var muted = new SolidBrush(_palette.MutedText);

        var state = graphics.Save();
        graphics.SetClip(viewport, CombineMode.Replace);
        var y = content.Top - _scrollOffset;
        var drawIndex = 0;
        foreach (var section in sections)
        {
            var headerRect = new Rectangle(content.Left + 2, y + 2, viewport.Width - 8, 24);
            if (headerRect.Bottom > viewport.Top && headerRect.Top < viewport.Bottom)
            {
                DrawGroupHeader(graphics, headerRect, section.Title, section.Entries.Count, headerFont, text, muted);
            }

            y += GroupHeaderHeight;
            DrawEntryCells(graphics, section.Entries, viewport, content.Left + xPad, y, cellWidth, cellHeight, drawIndex);
            drawIndex += section.Entries.Count;
            y += (int)Math.Ceiling(section.Entries.Count / (double)columns) * cellHeight + GroupGap;
        }
        graphics.Restore(state);

        DrawTruncatedHint(graphics, content, nameFont, muted);
        DrawScrollBar(graphics, content, viewport, maxScroll, totalHeight);
    }

    private void DrawEntryCells(Graphics graphics, IReadOnlyList<FolderEntry> entries, Rectangle viewport, int left, int top, int cellWidth, int cellHeight, int indexOffset)
    {
        using var nameFont = new Font("Microsoft YaHei UI", 8.2F, FontStyle.Regular, GraphicsUnit.Point);
        using var text = new SolidBrush(_palette.Text);
        var columns = Math.Max(1, viewport.Width / cellWidth);

        for (var i = 0; i < entries.Count; i++)
        {
            var row = i / columns;
            var col = i % columns;
            var rect = new Rectangle(left + col * cellWidth, top + row * cellHeight, cellWidth - 8, cellHeight - 6);
            if (rect.Bottom <= viewport.Top)
            {
                continue;
            }
            if (rect.Top >= viewport.Bottom)
            {
                break;
            }

            var entry = entries[i];
            var drawIndex = indexOffset + i;
            var hitBounds = Rectangle.Intersect(rect, viewport);
            if (!hitBounds.IsEmpty)
            {
                _entryHits.Add(new EntryHit(drawIndex, hitBounds, entry));
            }
            if (drawIndex == _hoverIndex)
            {
                using var hoverPath = RoundedRect(rect, 10);
                using var hover = new SolidBrush(WithAlpha(_palette.PanelAlt, _palette.IsDark ? 0.5 : 0.7));
                graphics.FillPath(hover, hoverPath);
            }

            var icon = _iconService.GetIcon(entry, 40);
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
    }

    private void DrawGroupHeader(Graphics graphics, Rectangle rect, string title, int count, Font font, Brush text, Brush muted)
    {
        using var fillPath = RoundedRect(new Rectangle(rect.Left, rect.Top + 1, rect.Width, rect.Height - 2), 9);
        using var fill = new SolidBrush(WithAlpha(_palette.PanelAlt, _palette.IsDark ? 0.38 : 0.56));
        using var border = new Pen(WithAlpha(_palette.Border, 0.5));
        graphics.FillPath(fill, fillPath);
        graphics.DrawPath(border, fillPath);

        using var titleFormat = new StringFormat { LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter };
        var titleRect = new Rectangle(rect.Left + 10, rect.Top, rect.Width - 72, rect.Height);
        graphics.DrawString(title, font, text, titleRect, titleFormat);

        using var countFormat = new StringFormat { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center };
        var countRect = new Rectangle(rect.Right - 64, rect.Top, 54, rect.Height);
        graphics.DrawString(count.ToString(), font, muted, countRect, countFormat);
    }

    private void DrawTruncatedHint(Graphics graphics, Rectangle content, Font font, Brush brush)
    {
        if (_result?.IsTruncated != true)
        {
            return;
        }

        var hint = string.Format("\u4ec5\u663e\u793a\u524d {0} \u9879\uff0c\u53ef\u5728\u8d44\u6e90\u7ba1\u7406\u5668\u4e2d\u67e5\u770b\u5168\u90e8", _config.FolderPanel.MaxItems);
        graphics.DrawString(hint, font, brush, new Rectangle(content.Left + 4, content.Bottom - 20, content.Width - 8, 18));
    }

    private void DrawScrollBar(Graphics graphics, Rectangle content, Rectangle viewport, int maxScroll, int totalContentHeight)
    {
        if (maxScroll <= 0)
        {
            return;
        }

        var track = new Rectangle(content.Right - 4, viewport.Top + 2, 3, viewport.Height - 4);
        var thumbHeight = Math.Max(24, (int)(track.Height * (viewport.Height / (double)Math.Max(1, totalContentHeight))));
        var thumbTop = track.Top + (int)((track.Height - thumbHeight) * (_scrollOffset / (double)maxScroll));
        using var scroll = new SolidBrush(WithAlpha(_palette.MutedText, 0.45));
        graphics.FillRectangle(scroll, new Rectangle(track.Left, thumbTop, track.Width, thumbHeight));
    }

    private List<GroupSection> BuildGroupSections(IReadOnlyList<FolderEntry> entries)
    {
        var sections = new List<GroupSection>();
        var usedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var group in _rootItem?.FolderGroups ?? [])
        {
            var groupKeys = group.EntryKeys.ToHashSet(StringComparer.OrdinalIgnoreCase);
            var groupEntries = entries.Where(entry => groupKeys.Contains(entry.EntryKey)).ToList();
            if (groupEntries.Count == 0)
            {
                continue;
            }

            foreach (var entry in groupEntries)
            {
                usedKeys.Add(entry.EntryKey);
            }
            sections.Add(new GroupSection(group.Name, groupEntries));
        }

        var ungrouped = entries.Where(entry => !usedKeys.Contains(entry.EntryKey)).ToList();
        if (ungrouped.Count > 0)
        {
            sections.Add(new GroupSection("\u672a\u5206\u7ec4", ungrouped));
        }

        return sections.Count > 0 ? sections : [new GroupSection("\u5168\u90e8", entries.ToList())];
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
        Cursor = Cursors.Default;
        var buttonTip = GetHeaderButtonToolTip(e.Location);
        if (!string.IsNullOrWhiteSpace(buttonTip))
        {
            if (_hoverIndex != -1)
            {
                _hoverIndex = -1;
                RenderLayeredFlyout();
            }
            ShowToolTip(buttonTip, e.Location);
            return;
        }

        var hit = _entryHits.FirstOrDefault(hit => hit.Bounds.Contains(e.Location));
        var index = hit?.Index ?? -1;
        if (_hoverIndex != index)
        {
            _hoverIndex = index;
            if (hit?.Entry is not null)
            {
                ShowToolTip(hit.Entry.Name, e.Location);
            }
            else
            {
                HideToolTip();
            }
            RenderLayeredFlyout();
        }
        else if (hit is null)
        {
            HideToolTip();
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
            CloseWithAnimation();
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
        HideToolTip();
        _scrollOffset = Math.Max(0, _scrollOffset - Math.Sign(e.Delta) * 80);
        RenderLayeredFlyout();
    }

    private void StartCloseTimer()
    {
        _closeTimer.Stop();
        _closeTimer.Start();
    }

    private void StartOpenAnimation(string dockPosition)
    {
        _closeTimer.Stop();
        _animationTimer.Stop();
        _animationClock.Restart();
        _animationState = FlyoutAnimationState.Opening;
        _baseLocation = Location;
        _animationStartOffset = GetOpenOffset(dockPosition);
        _closeOffset = _animationStartOffset;
        _animationEndOffset = Point.Empty;
        _animationCurrentOffset = _animationStartOffset;
        _animationStartAlpha = 0;
        _animationAlpha = 0;
        _animationTimer.Start();
    }

    private void CloseWithAnimation()
    {
        if (!Visible)
        {
            Hide();
            return;
        }

        if (_animationState == FlyoutAnimationState.Closing)
        {
            return;
        }

        _closeTimer.Stop();
        HideToolTip();
        _animationTimer.Stop();
        _animationClock.Restart();
        _animationState = FlyoutAnimationState.Closing;
        _animationStartOffset = _animationCurrentOffset;
        _animationEndOffset = _closeOffset;
        _animationStartAlpha = Math.Max(_animationAlpha, (byte)1);
        _animationAlpha = _animationStartAlpha;
        _animationTimer.Start();
    }

    private void StepAnimation()
    {
        var progress = Math.Clamp(_animationClock.ElapsedMilliseconds / (double)AnimationDurationMs, 0, 1);
        var eased = EaseOutCubic(progress);

        if (_animationState == FlyoutAnimationState.Opening)
        {
            _animationAlpha = (byte)Math.Clamp((int)Math.Round(_animationStartAlpha + ((255 - _animationStartAlpha) * eased)), 0, 255);
            _animationCurrentOffset = Lerp(_animationStartOffset, Point.Empty, eased);
            RenderLayeredFlyout();

            if (progress >= 1)
            {
                FinishAnimation();
            }

            return;
        }

        if (_animationState == FlyoutAnimationState.Closing)
        {
            _animationAlpha = (byte)Math.Clamp((int)Math.Round(_animationStartAlpha * (1 - eased)), 0, 255);
            _animationCurrentOffset = Lerp(_animationStartOffset, _animationEndOffset, eased);
            RenderLayeredFlyout();

            if (progress >= 1)
            {
                _animationTimer.Stop();
                _animationClock.Reset();
                _animationState = FlyoutAnimationState.None;
                _animationCurrentOffset = Point.Empty;
                _animationAlpha = 255;
                Hide();
            }
        }
    }

    private void FinishAnimation()
    {
        _animationTimer.Stop();
        _animationClock.Reset();
        _animationState = FlyoutAnimationState.None;
        _animationCurrentOffset = Point.Empty;
        _animationAlpha = 255;
        Location = _baseLocation;
        RenderLayeredFlyout();
    }

    private static Point GetOpenOffset(string dockPosition)
    {
        return dockPosition switch
        {
            "top" => new Point(0, -AnimationOffset),
            "left" => new Point(-AnimationOffset, 0),
            "right" => new Point(AnimationOffset, 0),
            _ => new Point(0, AnimationOffset)
        };
    }

    private static Point Lerp(Point from, Point to, double progress)
    {
        return new Point(
            (int)Math.Round(from.X + ((to.X - from.X) * progress)),
            (int)Math.Round(from.Y + ((to.Y - from.Y) * progress)));
    }

    private static double EaseOutCubic(double value)
    {
        var t = 1 - Math.Clamp(value, 0, 1);
        return 1 - (t * t * t);
    }

    private string GetTitle()
    {
        var name = Path.GetFileName(_currentPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        return string.IsNullOrWhiteSpace(name) ? _currentPath : name;
    }

    private string GetHeaderSubtitle()
    {
        var path = ShortPathLabel(_currentPath, 42);
        if (_loading)
        {
            return "\u6b63\u5728\u8bfb\u53d6... \u00b7 " + path;
        }

        if (_result is null || !string.IsNullOrWhiteSpace(_result.ErrorMessage))
        {
            return path;
        }

        var count = _result.IsTruncated ? _result.Entries.Count + "+" : _result.Entries.Count.ToString();
        return count + " \u9879 \u00b7 " + path;
    }

    private string GetHeaderButtonToolTip(Point location)
    {
        if (_backRect.Contains(location))
        {
            return _history.Count > 0 ? "\u8fd4\u56de\u4e0a\u4e00\u7ea7" : "\u65e0\u53ef\u8fd4\u56de\u7684\u6587\u4ef6\u5939";
        }

        if (_explorerRect.Contains(location))
        {
            return "\u5728\u8d44\u6e90\u7ba1\u7406\u5668\u4e2d\u6253\u5f00";
        }

        if (_closeRect.Contains(location))
        {
            return "\u5173\u95ed";
        }

        return "";
    }

    private void ShowToolTip(string text, Point location)
    {
        if (string.Equals(_shownToolTipText, text, StringComparison.Ordinal))
        {
            return;
        }

        _nameToolTip.Hide(this);
        _shownToolTipText = text;
        _nameToolTip.Show(text, this, location.X + 12, location.Y + 18);
    }

    private void HideToolTip()
    {
        _shownToolTipText = "";
        _nameToolTip.Hide(this);
    }

    private static string ShortPathLabel(string path, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(path) || path.Length <= maxLength)
        {
            return path;
        }

        try
        {
            var root = Path.GetPathRoot(path) ?? "";
            var trimmed = path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var name = Path.GetFileName(trimmed);
            if (!string.IsNullOrWhiteSpace(root) && !string.IsNullOrWhiteSpace(name))
            {
                var label = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + "\\...\\" + name;
                return label.Length <= maxLength ? label : "...\\" + name;
            }
        }
        catch
        {
        }

        return "..." + path[^Math.Min(path.Length, Math.Max(8, maxLength - 3))..];
    }

    private bool ShouldShowGroupedSections()
    {
        return IsRootFolder()
            && _rootItem?.FolderGroups.Count > 0
            && _result is not null
            && string.IsNullOrWhiteSpace(_result.ErrorMessage);
    }

    private bool IsRootFolder()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_rootPath) || string.IsNullOrWhiteSpace(_currentPath))
            {
                return false;
            }

            return string.Equals(
                Path.GetFullPath(_rootPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                Path.GetFullPath(_currentPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
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
    private sealed record GroupSection(string Title, List<FolderEntry> Entries);

    private enum FlyoutAnimationState
    {
        None,
        Opening,
        Closing
    }
}
