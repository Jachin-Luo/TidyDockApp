using System.Drawing.Drawing2D;

namespace TidyDock.WinForms.UI;

internal sealed class CardPanel : Panel
{
    public CardPanel()
    {
        DoubleBuffered = true;
        Padding = new Padding(14);
        Margin = new Padding(0, 0, 0, 14);
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
    }

    public Color BorderColor { get; set; }
    public Color FillColor { get; set; }
    public int Radius { get; set; } = 12;

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        using var brush = new SolidBrush(PaintSurface.ResolveBackColor(Parent, BackColor));
        e.Graphics.FillRectangle(brush, ClientRectangle);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var bounds = new Rectangle(0, 0, Width - 1, Height - 1);
        using var path = RoundedRect(bounds, Radius);
        using var background = new SolidBrush(FillColor.IsEmpty ? BackColor : FillColor);
        using var border = new Pen(BorderColor);
        e.Graphics.FillPath(background, path);
        e.Graphics.DrawPath(border, path);
        base.OnPaint(e);
    }

    protected override void OnResize(EventArgs eventargs)
    {
        base.OnResize(eventargs);
        Invalidate();
    }

    public static GraphicsPath RoundedRect(Rectangle bounds, int radius)
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
}

internal sealed class GlassButton : Control
{
    private bool _hovered;
    private bool _pressed;

    public GlassButton()
    {
        Cursor = Cursors.Hand;
        Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
        Size = new Size(96, 34);
        Margin = new Padding(0, 0, 8, 0);
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
    }

    public ThemePalette Palette { get; set; } = Theme.Get("system");
    public bool Primary { get; set; }
    public bool Danger { get; set; }

    protected override void OnMouseEnter(EventArgs e)
    {
        _hovered = true;
        Invalidate();
        base.OnMouseEnter(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        _hovered = false;
        _pressed = false;
        Invalidate();
        base.OnMouseLeave(e);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            _pressed = true;
            Invalidate();
        }
        base.OnMouseDown(e);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        _pressed = false;
        Invalidate();
        base.OnMouseUp(e);
    }

    protected override void OnEnabledChanged(EventArgs e)
    {
        _hovered = false;
        _pressed = false;
        Invalidate();
        base.OnEnabledChanged(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.Clear(PaintSurface.ResolveBackColor(this, Palette.Panel));
        var bounds = new Rectangle(0, 0, Width - 1, Height - 1);
        using var path = CardPanel.RoundedRect(bounds, 10);

        var baseColor = Primary ? Palette.Accent : Palette.PanelAlt;
        if (Danger && !Primary)
        {
            baseColor = Palette.IsDark ? Color.FromArgb(62, 42, 46) : Color.FromArgb(255, 244, 244);
        }
        if (!Enabled)
        {
            baseColor = Palette.IsDark ? Color.FromArgb(40, 43, 48) : Color.FromArgb(229, 233, 238);
        }
        var fill = _pressed ? ControlPaint.Dark(baseColor, 0.08F) : _hovered ? ControlPaint.Light(baseColor, 0.08F) : baseColor;
        var borderColor = !Enabled ? Palette.Border : Primary ? Palette.Accent : Danger ? Palette.Danger : Palette.Border;
        using var brush = new SolidBrush(fill);
        using var border = new Pen(borderColor);
        e.Graphics.FillPath(brush, path);
        e.Graphics.DrawPath(border, path);

        var textColor = !Enabled ? Palette.MutedText : Primary ? Palette.AccentText : Danger ? Palette.Danger : Palette.Text;
        using var textBrush = new SolidBrush(textColor);
        using var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter };
        e.Graphics.DrawString(Text, Font, textBrush, bounds, format);
    }

    protected override void OnPaintBackground(PaintEventArgs pevent)
    {
    }
}

internal sealed class ToggleSwitch : Control
{
    private bool _checked;
    private bool _hovered;

    public ToggleSwitch()
    {
        Cursor = Cursors.Hand;
        Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
        Size = new Size(260, 32);
        Margin = new Padding(0, 1, 0, 7);
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
    }

    public event EventHandler? CheckedChanged;
    public ThemePalette Palette { get; set; } = Theme.Get("system");

    public bool Checked
    {
        get => _checked;
        set
        {
            if (_checked == value)
            {
                return;
            }
            _checked = value;
            Invalidate();
            CheckedChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    protected override void OnClick(EventArgs e)
    {
        Checked = !Checked;
        base.OnClick(e);
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        _hovered = true;
        Invalidate();
        base.OnMouseEnter(e);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        _hovered = false;
        Invalidate();
        base.OnMouseLeave(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.Clear(PaintSurface.ResolveBackColor(this, Palette.Panel));
        var switchBounds = new Rectangle(0, 5, 44, 22);
        using var trackPath = CardPanel.RoundedRect(switchBounds, 11);
        var trackColor = Checked ? Palette.Accent : (_hovered ? ControlPaint.Light(Palette.PanelAlt, 0.08F) : Palette.PanelAlt);
        using var track = new SolidBrush(trackColor);
        using var border = new Pen(Checked ? Palette.Accent : Palette.Border);
        e.Graphics.FillPath(track, trackPath);
        e.Graphics.DrawPath(border, trackPath);

        var knobX = Checked ? switchBounds.Right - 19 : switchBounds.Left + 3;
        var knobRect = new Rectangle(knobX, switchBounds.Top + 3, 16, 16);
        using var knob = new SolidBrush(Checked ? Palette.AccentText : Palette.MutedText);
        e.Graphics.FillEllipse(knob, knobRect);

        using var text = new SolidBrush(Palette.Text);
        using var format = new StringFormat { LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter };
        e.Graphics.DrawString(Text, Font, text, new Rectangle(56, 0, Width - 56, Height), format);
    }

    protected override void OnPaintBackground(PaintEventArgs pevent)
    {
    }
}

internal sealed class GlassSlider : Control
{
    private bool _dragging;
    private int _value;

    public GlassSlider()
    {
        Cursor = Cursors.Hand;
        Size = new Size(300, 32);
        Margin = new Padding(0, 0, 0, 0);
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
    }

    public event EventHandler? ValueChanged;
    public ThemePalette Palette { get; set; } = Theme.Get("system");
    public int Minimum { get; set; }
    public int Maximum { get; set; } = 100;

    public int Value
    {
        get => _value;
        set
        {
            var next = Math.Clamp(value, Minimum, Maximum);
            if (_value == next)
            {
                return;
            }
            _value = next;
            Invalidate();
            ValueChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            _dragging = true;
            SetValueFromX(e.X);
        }
        base.OnMouseDown(e);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        if (_dragging)
        {
            SetValueFromX(e.X);
        }
        base.OnMouseMove(e);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        _dragging = false;
        base.OnMouseUp(e);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.Clear(PaintSurface.ResolveBackColor(this, Palette.Panel));
        var track = new Rectangle(2, Height / 2 - 3, Width - 4, 6);
        using var trackPath = CardPanel.RoundedRect(track, 3);
        using var trackBrush = new SolidBrush(Palette.PanelAlt);
        e.Graphics.FillPath(trackBrush, trackPath);

        var percent = Maximum == Minimum ? 0 : (Value - Minimum) / (double)(Maximum - Minimum);
        var fillWidth = Math.Max(6, (int)Math.Round(track.Width * percent));
        var fillRect = new Rectangle(track.Left, track.Top, fillWidth, track.Height);
        using var fillPath = CardPanel.RoundedRect(fillRect, 3);
        using var fillBrush = new SolidBrush(Palette.Accent);
        e.Graphics.FillPath(fillBrush, fillPath);

        var knobX = track.Left + fillWidth - 8;
        var knob = new Rectangle(Math.Clamp(knobX, track.Left, track.Right - 16), Height / 2 - 8, 16, 16);
        using var shadow = new SolidBrush(Color.FromArgb(Palette.IsDark ? 90 : 45, Palette.Shadow));
        e.Graphics.FillEllipse(shadow, knob.X + 1, knob.Y + 2, knob.Width, knob.Height);
        using var knobBrush = new SolidBrush(Palette.Accent);
        e.Graphics.FillEllipse(knobBrush, knob);
    }

    private void SetValueFromX(int x)
    {
        var trackWidth = Math.Max(1, Width - 4);
        var percent = Math.Clamp((x - 2) / (double)trackWidth, 0, 1);
        Value = Minimum + (int)Math.Round((Maximum - Minimum) * percent);
    }

    protected override void OnPaintBackground(PaintEventArgs pevent)
    {
    }
}

internal static class PaintSurface
{
    public static Color ResolveBackColor(Control? control, Color fallback)
    {
        while (control is not null)
        {
            if (!control.BackColor.IsEmpty && control.BackColor != Color.Transparent)
            {
                return Color.FromArgb(255, control.BackColor);
            }

            control = control.Parent;
        }

        return fallback.IsEmpty || fallback == Color.Transparent ? SystemColors.Control : Color.FromArgb(255, fallback);
    }
}

internal static class ThemedMenu
{
    public static ContextMenuStrip Create(ThemePalette palette)
    {
        var menu = new ContextMenuStrip
        {
            Font = new Font("Microsoft YaHei UI", 10.5F, FontStyle.Regular, GraphicsUnit.Point),
            ShowImageMargin = false,
            Padding = new Padding(6),
            BackColor = palette.Panel,
            ForeColor = palette.Text,
            Renderer = new ThemedMenuRenderer(palette)
        };
        menu.Opening += delegate { ApplyMetrics(menu.Items, menu.Font, palette); };
        return menu;
    }

    private static void ApplyMetrics(ToolStripItemCollection items, Font font, ThemePalette palette)
    {
        foreach (ToolStripItem item in items)
        {
            item.BackColor = palette.Panel;
            item.ForeColor = item.Enabled ? palette.Text : palette.MutedText;

            if (item is ToolStripSeparator separator)
            {
                separator.AutoSize = false;
                separator.Height = 8;
                continue;
            }

            var textWidth = TextRenderer.MeasureText(item.Text, font).Width;
            var extraWidth = item is ToolStripMenuItem menuItem && menuItem.DropDownItems.Count > 0 ? 68 : 44;
            item.AutoSize = false;
            item.TextAlign = ContentAlignment.MiddleLeft;
            item.Size = new Size(Math.Max(224, textWidth + extraWidth), 36);

            if (item is ToolStripMenuItem childMenu && childMenu.DropDownItems.Count > 0)
            {
                if (childMenu.DropDown is ToolStripDropDownMenu dropDown)
                {
                    dropDown.ShowImageMargin = false;
                    dropDown.Padding = new Padding(6);
                    dropDown.BackColor = palette.Panel;
                    dropDown.ForeColor = palette.Text;
                    dropDown.Renderer = new ThemedMenuRenderer(palette);
                }

                ApplyMetrics(childMenu.DropDownItems, font, palette);
            }
        }
    }
}

internal sealed class ThemedMenuRenderer : ToolStripProfessionalRenderer
{
    private readonly ThemePalette _palette;

    public ThemedMenuRenderer(ThemePalette palette)
        : base(new ThemedMenuColorTable(palette))
    {
        _palette = palette;
    }

    protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
    {
        using var background = new SolidBrush(_palette.Panel);
        e.Graphics.FillRectangle(background, e.AffectedBounds);
    }

    protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
    {
        var bounds = new Rectangle(Point.Empty, e.Item.Size);
        using var background = new SolidBrush(e.Item.Selected && e.Item.Enabled ? _palette.PanelAlt : _palette.Panel);
        e.Graphics.FillRectangle(background, bounds);

        if (e.Item.Selected && e.Item.Enabled)
        {
            using var border = new Pen(_palette.Border);
            e.Graphics.DrawRectangle(border, new Rectangle(1, 1, bounds.Width - 3, bounds.Height - 3));
        }
    }

    protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
    {
        var selected = e.Item.Selected && e.Item.Enabled;
        var color = !e.Item.Enabled
            ? _palette.MutedText
            : selected
                ? _palette.Text
                : _palette.Text;
        var rectangle = new Rectangle(
            e.TextRectangle.Left + 4,
            0,
            Math.Max(1, e.TextRectangle.Width - 8),
            e.Item.Height);
        var flags = TextFormatFlags.Left
            | TextFormatFlags.VerticalCenter
            | TextFormatFlags.SingleLine
            | TextFormatFlags.EndEllipsis
            | TextFormatFlags.PreserveGraphicsClipping;
        TextRenderer.DrawText(e.Graphics, e.Text, e.TextFont, rectangle, color, flags);
    }

    protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
    {
        e.ArrowColor = e.Item?.Enabled == false ? _palette.MutedText : _palette.Text;
        base.OnRenderArrow(e);
    }

    protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
    {
        var y = e.Item.Height / 2;
        using var line = new Pen(_palette.Border);
        e.Graphics.DrawLine(line, 8, y, e.Item.Width - 8, y);
    }

    protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
    {
        using var border = new Pen(_palette.Border);
        e.Graphics.DrawRectangle(border, new Rectangle(0, 0, e.ToolStrip.Width - 1, e.ToolStrip.Height - 1));
    }
}

internal sealed class ThemedMenuColorTable : ProfessionalColorTable
{
    private readonly ThemePalette _palette;

    public ThemedMenuColorTable(ThemePalette palette)
    {
        _palette = palette;
    }

    public override Color ToolStripDropDownBackground => _palette.Panel;
    public override Color MenuItemSelected => _palette.PanelAlt;
    public override Color MenuItemBorder => _palette.Border;
    public override Color MenuBorder => _palette.Border;
    public override Color ImageMarginGradientBegin => _palette.Panel;
    public override Color ImageMarginGradientMiddle => _palette.Panel;
    public override Color ImageMarginGradientEnd => _palette.Panel;
    public override Color SeparatorDark => _palette.Border;
    public override Color SeparatorLight => _palette.PanelAlt;
}
