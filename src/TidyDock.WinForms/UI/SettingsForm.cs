using System.Drawing.Drawing2D;
using TidyDock.WinForms.Core;
using TidyDock.WinForms.Services;
using TidyDock.WinForms.Shell;

namespace TidyDock.WinForms.UI;

internal sealed class SettingsForm : Form
{
    private readonly AppConfig _config;
    private readonly IconService _iconService;
    private readonly StartupService _startupService;
    private readonly Action _apply;
    private readonly Action _addAppFile;
    private readonly Action _addFolder;
    private readonly Action _addUrl;
    private readonly Action _addThisPc;
    private readonly Action _addSeparator;
    private readonly ComboOption[] _themeOptions =
    [
        new("system", "\u8ddf\u968f\u7cfb\u7edf"),
        new("light", "\u6d45\u8272"),
        new("dark", "\u6df1\u8272")
    ];
    private readonly ComboOption[] _positionOptions =
    [
        new("bottom", "\u5e95\u90e8"),
        new("top", "\u9876\u90e8"),
        new("left", "\u5de6\u4fa7"),
        new("right", "\u53f3\u4fa7")
    ];
    private readonly ComboOption[] _openBehaviorOptions =
    [
        new(DockOpenBehaviors.Flyout, "\u60ac\u6d6e\u9762\u677f"),
        new(DockOpenBehaviors.Explorer, "\u8d44\u6e90\u7ba1\u7406\u5668")
    ];
    private ListBox _items = new();
    private ComboBox? _openBehaviorCombo;
    private FlowLayoutPanel? _scrollRoot;
    private bool _updatingItemControls;
    private ThemePalette _palette;

    public SettingsForm(AppConfig config, IconService iconService, StartupService startupService, Action apply, Action addAppFile, Action addFolder, Action addUrl, Action addThisPc, Action addSeparator)
    {
        _config = config;
        _iconService = iconService;
        _startupService = startupService;
        _apply = apply;
        _addAppFile = addAppFile;
        _addFolder = addFolder;
        _addUrl = addUrl;
        _addThisPc = addThisPc;
        _addSeparator = addSeparator;
        _palette = Theme.Get(_config.Dock.Theme);

        Text = "TidyDock";
        Width = 760;
        Height = 650;
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(700, 560);
        Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
        ApplyTheme();
        Build();
        RefreshItems();
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        _ = DwmBackdrop.TryApply(Handle, _palette.IsDark);
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        using var background = new LinearGradientBrush(
            ClientRectangle,
            _palette.IsDark ? Color.FromArgb(17, 19, 23) : Color.FromArgb(236, 243, 247),
            _palette.IsDark ? Color.FromArgb(31, 35, 41) : Color.FromArgb(248, 250, 252),
            90f);
        e.Graphics.FillRectangle(background, ClientRectangle);
    }

    private void Build()
    {
        Controls.Clear();
        ApplyTheme();

        var shell = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 2,
            ColumnCount = 1,
            BackColor = _palette.Window
        };
        shell.RowStyles.Add(new RowStyle(SizeType.Absolute, 92));
        shell.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        Controls.Add(shell);

        var header = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(22, 16, 22, 12),
            BackColor = _palette.Window
        };
        shell.Controls.Add(header, 0, 0);

        var title = new Label
        {
            Text = "TidyDock",
            AutoSize = true,
            Font = new Font("Segoe UI", 18F, FontStyle.Bold, GraphicsUnit.Point),
            ForeColor = _palette.Text,
            BackColor = _palette.Window,
            Location = new Point(22, 16)
        };
        header.Controls.Add(title);

        var subtitle = new Label
        {
            Text = "\u4f4e\u8d44\u6e90\u5360\u7528\u7684 Windows Dock",
            AutoSize = true,
            Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point),
            ForeColor = _palette.MutedText,
            BackColor = _palette.Window,
            Location = new Point(24, 50)
        };
        header.Controls.Add(subtitle);

        var badge = new Label
        {
            Text = "WinForms v0.2 \u9884\u89c8\u7248",
            AutoSize = true,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point),
            BackColor = _palette.Accent,
            ForeColor = _palette.AccentText,
            Padding = new Padding(10, 5, 10, 5),
            Location = new Point(560, 24),
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        header.Controls.Add(badge);
        header.Resize += delegate { badge.Left = Math.Max(22, header.ClientSize.Width - badge.Width - 32); };

        var root = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            Padding = new Padding(22, 0, 22, 18),
            BackColor = _palette.Window
        };
        _scrollRoot = root;
        shell.Controls.Add(root, 0, 1);

        AddSection(root, "\u5916\u89c2", panel =>
        {
            panel.Controls.Add(MakeCombo("\u4e3b\u9898", _themeOptions, _config.Dock.Theme, value =>
            {
                _config.Dock.Theme = value;
                _palette = Theme.Get(value);
                ApplyTheme();
                RunKeepingScroll(() =>
                {
                    Build();
                    _apply();
                });
            }));
            panel.Controls.Add(MakeCombo("\u4f4d\u7f6e", _positionOptions, _config.Dock.Position, value => { _config.Dock.Position = value; ApplyKeepingScroll(); }));
            panel.Controls.Add(MakeNumber("\u56fe\u6807\u5927\u5c0f", _config.Dock.IconSize, 32, 96, value => { _config.Dock.IconSize = value; ApplyKeepingScroll(); }));
            panel.Controls.Add(MakeNumber("\u56fe\u6807\u95f4\u8ddd", _config.Dock.IconGap, 0, 32, value => { _config.Dock.IconGap = value; ApplyKeepingScroll(); }));
            panel.Controls.Add(MakeNumber("\u5706\u89d2\u534a\u5f84", _config.Dock.CornerRadius, 6, 40, value => { _config.Dock.CornerRadius = value; ApplyKeepingScroll(); }));
            panel.Controls.Add(MakePercent("\u900f\u660e\u5ea6", _config.Dock.Opacity, value => { _config.Dock.Opacity = value; ApplyKeepingScroll(); }));
            panel.Controls.Add(MakePercent("\u60ac\u505c\u653e\u5927", _config.Dock.Magnification - 1, value => { _config.Dock.Magnification = 1 + value; ApplyKeepingScroll(); }));
        });

        AddSection(root, "\u884c\u4e3a", panel =>
        {
            panel.Controls.Add(MakeCheck("\u81ea\u52a8\u9690\u85cf", _config.Dock.AutoHide, value => { _config.Dock.AutoHide = value; ApplyKeepingScroll(); }));
            panel.Controls.Add(MakeCheck("\u59cb\u7ec8\u7f6e\u9876", _config.Dock.AlwaysOnTop, value => { _config.Dock.AlwaysOnTop = value; ApplyKeepingScroll(); }));
            panel.Controls.Add(MakeCheck("\u542f\u52a8\u540e\u663e\u793a Dock", _config.Dock.StartVisible, value => { _config.Dock.StartVisible = value; ApplyKeepingScroll(); }));
            panel.Controls.Add(MakeCheck("\u663e\u793a\u6258\u76d8\u56fe\u6807", _config.Dock.ShowTrayIcon, value => { _config.Dock.ShowTrayIcon = value; ApplyKeepingScroll(); }));
            panel.Controls.Add(MakeCheck("\u5141\u8bb8\u62d6\u62fd\u6392\u5e8f", _config.Dock.EnableDragReorder, value => { _config.Dock.EnableDragReorder = value; ApplyKeepingScroll(); }));
            panel.Controls.Add(MakeCheck("\u8ddf\u968f Windows \u542f\u52a8", _startupService.IsEnabled(), value => { _startupService.SetEnabled(value); }));
        });

        AddSection(root, "\u6587\u4ef6\u5939\u9762\u677f", panel =>
        {
            panel.Controls.Add(MakeNumber("\u6700\u5927\u5c55\u793a\u6570", _config.FolderPanel.MaxItems, 20, 1000, value => { _config.FolderPanel.MaxItems = value; ApplyKeepingScroll(); }));
            panel.Controls.Add(MakeNumber("\u9762\u677f\u6700\u5927\u9ad8\u5ea6", _config.FolderPanel.MaxHeight, 240, 760, value => { _config.FolderPanel.MaxHeight = value; ApplyKeepingScroll(); }));
            panel.Controls.Add(MakeCheck("\u663e\u793a\u9690\u85cf\u6587\u4ef6", _config.FolderPanel.ShowHiddenFiles, value => { _config.FolderPanel.ShowHiddenFiles = value; ApplyKeepingScroll(); }));
        });

        AddSection(root, "Dock \u9879", panel =>
        {
            _items = new ListBox
            {
                Height = 170,
                Width = 636,
                DrawMode = DrawMode.OwnerDrawFixed,
                ItemHeight = 34
            };
            _items.DrawItem += DrawListItem;
            _items.SelectedIndexChanged += delegate { UpdateItemControls(); };
            ApplyListTheme();
            panel.Controls.Add(_items);

            var addRow = MakeRow();
            addRow.Controls.Add(MakeButton("\u6dfb\u52a0\u5e94\u7528/\u6587\u4ef6", delegate { _addAppFile(); RefreshItems(); }, "primary"));
            addRow.Controls.Add(MakeButton("\u6dfb\u52a0\u6587\u4ef6\u5939", delegate { _addFolder(); RefreshItems(); }));
            addRow.Controls.Add(MakeButton("\u6dfb\u52a0\u7f51\u5740", delegate { _addUrl(); RefreshItems(); }));
            addRow.Controls.Add(MakeButton("\u6dfb\u52a0\u6b64\u7535\u8111", delegate { _addThisPc(); RefreshItems(); }));
            addRow.Controls.Add(MakeButton("\u6dfb\u52a0\u5206\u9694\u7b26", delegate { _addSeparator(); RefreshItems(); }));
            panel.Controls.Add(addRow);

            var editRow = MakeRow();
            editRow.Controls.Add(MakeButton("\u4e0a\u79fb", delegate { MoveSelected(-1); }));
            editRow.Controls.Add(MakeButton("\u4e0b\u79fb", delegate { MoveSelected(1); }));
            editRow.Controls.Add(MakeButton("\u91cd\u547d\u540d", RenameSelected));
            editRow.Controls.Add(MakeButton("\u79fb\u9664", RemoveSelected, "danger"));
            panel.Controls.Add(editRow);

            var styleRow = MakeRow();
            styleRow.Controls.Add(MakeButton("\u4e00\u952e\u5957\u5706\u89d2\u56fe\u6807", delegate { ApplyIconStyleToAll(DockIconStyles.RoundedRect); }, "primary"));
            styleRow.Controls.Add(MakeButton("\u6062\u590d\u539f\u59cb\u6837\u5f0f", delegate { ApplyIconStyleToAll(DockIconStyles.Default); }));
            panel.Controls.Add(styleRow);
            panel.Controls.Add(MakeCheck("\u81ea\u5b9a\u4e49\u56fe\u6807\u4e5f\u5957\u5706\u89d2\u80cc\u666f", _config.Dock.WrapCustomIcons, value =>
            {
                _config.Dock.WrapCustomIcons = value;
                _iconService.ClearMemoryCache();
                ApplyKeepingScroll();
                RefreshItems();
            }));

            var iconRow = MakeRow();
            iconRow.Controls.Add(MakeButton("\u66f4\u6362\u56fe\u6807", ChangeIconSelected));
            iconRow.Controls.Add(MakeButton("\u6062\u590d\u9ed8\u8ba4\u56fe\u6807", ResetIconSelected));
            iconRow.Controls.Add(new Label { Text = "\u6253\u5f00\u65b9\u5f0f", Width = 78, Height = 32, ForeColor = _palette.MutedText, BackColor = _palette.Panel, TextAlign = ContentAlignment.MiddleLeft, Margin = new Padding(10, 0, 0, 0) });
            _openBehaviorCombo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 150, Height = 32, DrawMode = DrawMode.OwnerDrawFixed, ItemHeight = 26, BackColor = _palette.PanelAlt, ForeColor = _palette.Text };
            _openBehaviorCombo.Items.AddRange(_openBehaviorOptions);
            _openBehaviorCombo.DrawItem += DrawComboItem;
            _openBehaviorCombo.SelectedIndexChanged += OpenBehaviorChanged;
            iconRow.Controls.Add(_openBehaviorCombo);
            panel.Controls.Add(iconRow);

            RefreshItems();
            UpdateItemControls();
        });
    }

    private void AddSection(Control parent, string title, Action<FlowLayoutPanel> build)
    {
        var card = new CardPanel
        {
            Width = 660,
            Height = 120,
            BackColor = _palette.Panel,
            FillColor = Color.FromArgb(_palette.IsDark ? 246 : 245, _palette.Panel),
            BorderColor = _palette.Border,
            Radius = 14
        };

        var panel = new FlowLayoutPanel
        {
            Width = 630,
            AutoSize = true,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Padding = new Padding(0),
            Margin = new Padding(0),
            Location = new Point(14, 14),
            BackColor = _palette.Panel
        };
        panel.Controls.Add(new Label
        {
            Text = title,
            AutoSize = true,
            Font = new Font("Segoe UI", 11F, FontStyle.Bold, GraphicsUnit.Point),
            ForeColor = _palette.Text,
            BackColor = _palette.Panel,
            Margin = new Padding(0, 0, 0, 10)
        });
        build(panel);
        panel.Height = panel.PreferredSize.Height;
        card.Height = panel.Height + 28;
        card.Controls.Add(panel);
        parent.Controls.Add(card);
    }

    private Control MakeCombo(string label, ComboOption[] values, string current, Action<string> change)
    {
        var row = MakeLabeledRow(label);
        var combo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 260, Height = 32, DrawMode = DrawMode.OwnerDrawFixed, ItemHeight = 26 };
        combo.Items.AddRange(values);
        combo.SelectedItem = values.FirstOrDefault(value => string.Equals(value.Value, current, StringComparison.OrdinalIgnoreCase)) ?? values[0];
        combo.BackColor = _palette.PanelAlt;
        combo.ForeColor = _palette.Text;
        combo.DrawItem += DrawComboItem;
        combo.SelectedIndexChanged += delegate { change(combo.SelectedItem is ComboOption option ? option.Value : values[0].Value); };
        row.Controls.Add(combo);
        return row;
    }

    private Control MakeNumber(string label, int value, int min, int max, Action<int> change)
    {
        var row = MakeLabeledRow(label);
        var number = new NumericUpDown { Width = 108, Height = 30, Minimum = min, Maximum = max, Value = Math.Clamp(value, min, max), BackColor = _palette.PanelAlt, ForeColor = _palette.Text, BorderStyle = BorderStyle.FixedSingle };
        number.ValueChanged += delegate { change((int)number.Value); };
        row.Controls.Add(number);
        return row;
    }

    private Control MakePercent(string label, double value, Action<double> change)
    {
        var row = MakeLabeledRow(label);
        var slider = new GlassSlider { Width = 250, Height = 32, Palette = _palette, Minimum = 0, Maximum = 100, Value = Math.Clamp((int)(value * 100), 0, 100), BackColor = _palette.Panel };
        var valueLabel = new Label { Width = 54, Height = 32, ForeColor = _palette.MutedText, BackColor = _palette.Panel, TextAlign = ContentAlignment.MiddleRight, Text = slider.Value + "%" };
        slider.ValueChanged += delegate
        {
            valueLabel.Text = slider.Value + "%";
            change(slider.Value / 100.0);
        };
        row.Controls.Add(slider);
        row.Controls.Add(valueLabel);
        return row;
    }

    private ToggleSwitch MakeCheck(string text, bool value, Action<bool> change)
    {
        var check = new ToggleSwitch { Text = text, Checked = value, Palette = _palette, BackColor = _palette.Panel };
        check.CheckedChanged += delegate { change(check.Checked); };
        return check;
    }

    private FlowLayoutPanel MakeLabeledRow(string label)
    {
        var row = MakeRow();
        row.Controls.Add(new Label { Text = label, Width = 170, Height = 32, ForeColor = _palette.MutedText, BackColor = _palette.Panel, TextAlign = ContentAlignment.MiddleLeft });
        return row;
    }

    private FlowLayoutPanel MakeRow()
    {
        return new FlowLayoutPanel { Width = 636, Height = 38, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, Margin = new Padding(0, 0, 0, 8), BackColor = _palette.Panel };
    }

    private GlassButton MakeButton(string text, EventHandler click, string style = "normal")
    {
        var button = new GlassButton
        {
            Text = text,
            Height = 32,
            Width = TextRenderer.MeasureText(text, Font).Width + 30,
            Palette = _palette,
            Primary = style == "primary",
            Danger = style == "danger",
            BackColor = _palette.Panel
        };
        button.Click += click;
        return button;
    }

    private void RefreshItems()
    {
        var selectedId = _items.SelectedItem is DockItem selected ? selected.Id : "";
        _items.Items.Clear();
        foreach (var item in _config.Items)
        {
            _items.Items.Add(item);
        }
        if (!string.IsNullOrWhiteSpace(selectedId))
        {
            for (var i = 0; i < _items.Items.Count; i++)
            {
                if (_items.Items[i] is DockItem item && item.Id == selectedId)
                {
                    _items.SelectedIndex = i;
                    break;
                }
            }
        }
    }

    private void MoveSelected(int delta)
    {
        var index = _items.SelectedIndex;
        var newIndex = index + delta;
        if (index < 0 || newIndex < 0 || newIndex >= _config.Items.Count)
        {
            return;
        }
        var item = _config.Items[index];
        _config.Items.RemoveAt(index);
        _config.Items.Insert(newIndex, item);
        ApplyKeepingScroll();
        RefreshItems();
        _items.SelectedIndex = newIndex;
    }

    private void RenameSelected(object? sender, EventArgs e)
    {
        if (_items.SelectedItem is not DockItem item)
        {
            return;
        }
        using var dialog = new InputDialog("\u91cd\u547d\u540d", "\u540d\u79f0", item.Name, _palette);
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            item.Name = dialog.Value;
            ApplyKeepingScroll();
            RefreshItems();
        }
    }

    private void RemoveSelected(object? sender, EventArgs e)
    {
        if (_items.SelectedIndex < 0)
        {
            return;
        }
        _config.Items.RemoveAt(_items.SelectedIndex);
        ApplyKeepingScroll();
        RefreshItems();
        UpdateItemControls();
    }

    private void ChangeIconSelected(object? sender, EventArgs e)
    {
        if (_items.SelectedItem is not DockItem item || item.IsSeparator)
        {
            return;
        }

        using var dialog = new OpenFileDialog
        {
            Title = "\u9009\u62e9\u81ea\u5b9a\u4e49\u56fe\u6807",
            Filter = "\u56fe\u6807\u548c\u56fe\u7247|*.png;*.ico;*.jpg;*.jpeg;*.bmp;*.gif;*.webp;*.exe;*.lnk|\u6240\u6709\u6587\u4ef6|*.*"
        };
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        item.IconPath = _iconService.ImportCustomIcon(dialog.FileName);
        ApplyKeepingScroll();
        RefreshItems();
    }

    private void ResetIconSelected(object? sender, EventArgs e)
    {
        if (_items.SelectedItem is not DockItem item || item.IsSeparator)
        {
            return;
        }

        item.IconPath = "";
        _iconService.ClearMemoryCache();
        ApplyKeepingScroll();
        RefreshItems();
    }

    private void ApplyIconStyleToAll(string iconStyle)
    {
        foreach (var item in _config.Items.Where(item => !item.IsSeparator))
        {
            item.IconStyle = iconStyle;
        }

        _iconService.ClearMemoryCache();
        ApplyKeepingScroll();
        RefreshItems();
        _items.Invalidate();
    }

    private void OpenBehaviorChanged(object? sender, EventArgs e)
    {
        if (_updatingItemControls || _openBehaviorCombo?.SelectedItem is not ComboOption option)
        {
            return;
        }

        if (_items.SelectedItem is not DockItem item || !string.Equals(item.Type, DockItemTypes.Folder, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        item.OpenBehavior = option.Value;
        ApplyKeepingScroll();
        RefreshItems();
    }

    private void ApplyKeepingScroll()
    {
        RunKeepingScroll(_apply);
    }

    private void RunKeepingScroll(Action action)
    {
        var scrollY = CurrentScrollY();
        action();
        RestoreScroll(scrollY);
        if (!IsDisposed && IsHandleCreated)
        {
            BeginInvoke(new Action(() => RestoreScroll(scrollY)));
        }
    }

    private int CurrentScrollY()
    {
        if (_scrollRoot is null || _scrollRoot.IsDisposed)
        {
            return 0;
        }
        return -_scrollRoot.AutoScrollPosition.Y;
    }

    private void RestoreScroll(int scrollY)
    {
        if (_scrollRoot is null || _scrollRoot.IsDisposed)
        {
            return;
        }

        _scrollRoot.AutoScrollPosition = new Point(0, Math.Max(0, scrollY));
    }

    private void UpdateItemControls()
    {
        if (_openBehaviorCombo is null)
        {
            return;
        }

        _updatingItemControls = true;
        try
        {
            var item = _items.SelectedItem as DockItem;
            var isFolder = item is not null && string.Equals(item.Type, DockItemTypes.Folder, StringComparison.OrdinalIgnoreCase);
            _openBehaviorCombo.Enabled = isFolder;
            var behavior = isFolder && string.Equals(item!.OpenBehavior, DockOpenBehaviors.Explorer, StringComparison.OrdinalIgnoreCase)
                ? DockOpenBehaviors.Explorer
                : DockOpenBehaviors.Flyout;
            _openBehaviorCombo.SelectedItem = _openBehaviorOptions.First(option => option.Value == behavior);
        }
        finally
        {
            _updatingItemControls = false;
        }
    }

    private void ApplyTheme()
    {
        BackColor = _palette.Window;
        ForeColor = _palette.Text;
    }

    private void ApplyListTheme()
    {
        _items.BackColor = _palette.PanelAlt;
        _items.ForeColor = _palette.Text;
        _items.BorderStyle = BorderStyle.FixedSingle;
    }

    private void DrawListItem(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0)
        {
            return;
        }
        var selected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var background = new SolidBrush(selected ? _palette.Accent : (e.Index % 2 == 0 ? _palette.PanelAlt : _palette.Panel));
        using var foreground = new SolidBrush(selected ? _palette.AccentText : _palette.Text);
        e.Graphics.FillRectangle(background, e.Bounds);
        var item = _items.Items[e.Index] is DockItem dockItem ? FormatDockItem(dockItem) : _items.Items[e.Index].ToString() ?? "";
        e.Graphics.DrawString(item, e.Font ?? Font, foreground, e.Bounds.Left + 10, e.Bounds.Top + 9);
    }

    private void DrawComboItem(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0 || sender is not ComboBox combo)
        {
            return;
        }
        var selected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
        using var background = new SolidBrush(selected ? _palette.Accent : _palette.PanelAlt);
        using var foreground = new SolidBrush(selected ? _palette.AccentText : _palette.Text);
        e.Graphics.FillRectangle(background, e.Bounds);
        e.Graphics.DrawString(combo.Items[e.Index]?.ToString() ?? "", e.Font ?? Font, foreground, e.Bounds.Left + 8, e.Bounds.Top + 5);
    }

    private static string FormatDockItem(DockItem item)
    {
        if (item.IsSeparator)
        {
            return "[\u5206\u9694\u7b26] " + (string.IsNullOrWhiteSpace(item.Name) ? "\u5206\u9694\u7b26" : item.Name);
        }

        var type = item.Type switch
        {
            DockItemTypes.App => "\u5e94\u7528",
            DockItemTypes.File => "\u6587\u4ef6",
            DockItemTypes.Folder => "\u6587\u4ef6\u5939",
            DockItemTypes.Url => "\u7f51\u5740",
            DockItemTypes.Shell => "\u7cfb\u7edf\u4f4d\u7f6e",
            DockItemTypes.Settings => "\u8bbe\u7f6e",
            _ => "\u9879\u76ee"
        };
        var style = string.Equals(item.IconStyle, DockIconStyles.RoundedRect, StringComparison.OrdinalIgnoreCase)
            ? " \u00b7 \u5706\u89d2\u56fe\u6807"
            : "";
        return $"[{type}] {item.Name}{style}";
    }

    private sealed record ComboOption(string Value, string Label)
    {
        public override string ToString()
        {
            return Label;
        }
    }
}
