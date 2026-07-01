using TidyDock.WinForms.Core;
using TidyDock.WinForms.Services;

namespace TidyDock.WinForms.UI;

internal sealed class SettingsForm : Form
{
    private readonly AppConfig _config;
    private readonly StartupService _startupService;
    private readonly Action _apply;
    private readonly Action _addAppFile;
    private readonly Action _addFolder;
    private readonly Action _addUrl;
    private readonly Action _addSeparator;
    private readonly ListBox _items = new();
    private ThemePalette _palette;

    public SettingsForm(AppConfig config, StartupService startupService, Action apply, Action addAppFile, Action addFolder, Action addUrl, Action addSeparator)
    {
        _config = config;
        _startupService = startupService;
        _apply = apply;
        _addAppFile = addAppFile;
        _addFolder = addFolder;
        _addUrl = addUrl;
        _addSeparator = addSeparator;
        _palette = Theme.Get(_config.Dock.Theme);

        Text = "TidyDock Settings";
        Width = 520;
        Height = 680;
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(460, 560);
        ApplyTheme();
        Build();
        RefreshItems();
    }

    private void Build()
    {
        Controls.Clear();
        var root = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            Padding = new Padding(16)
        };
        Controls.Add(root);

        AddSection(root, "Appearance", panel =>
        {
            panel.Controls.Add(MakeCombo("Theme", ["system", "light", "dark"], _config.Dock.Theme, value =>
            {
                _config.Dock.Theme = value;
                _palette = Theme.Get(value);
                ApplyTheme();
                Build();
                _apply();
            }));
            panel.Controls.Add(MakeCombo("Position", ["bottom", "top", "left", "right"], _config.Dock.Position, value => { _config.Dock.Position = value; _apply(); }));
            panel.Controls.Add(MakeNumber("Icon size", _config.Dock.IconSize, 32, 96, value => { _config.Dock.IconSize = value; _apply(); }));
            panel.Controls.Add(MakeNumber("Icon gap", _config.Dock.IconGap, 0, 32, value => { _config.Dock.IconGap = value; _apply(); }));
            panel.Controls.Add(MakeNumber("Corner radius", _config.Dock.CornerRadius, 6, 40, value => { _config.Dock.CornerRadius = value; _apply(); }));
            panel.Controls.Add(MakePercent("Opacity", _config.Dock.Opacity, value => { _config.Dock.Opacity = value; _apply(); }));
            panel.Controls.Add(MakePercent("Magnification", _config.Dock.Magnification - 1, value => { _config.Dock.Magnification = 1 + value; _apply(); }));
        });

        AddSection(root, "Behavior", panel =>
        {
            panel.Controls.Add(MakeCheck("Auto hide", _config.Dock.AutoHide, value => { _config.Dock.AutoHide = value; _apply(); }));
            panel.Controls.Add(MakeCheck("Always on top", _config.Dock.AlwaysOnTop, value => { _config.Dock.AlwaysOnTop = value; _apply(); }));
            panel.Controls.Add(MakeCheck("Show on launch", _config.Dock.StartVisible, value => { _config.Dock.StartVisible = value; _apply(); }));
            panel.Controls.Add(MakeCheck("Show tray icon", _config.Dock.ShowTrayIcon, value => { _config.Dock.ShowTrayIcon = value; _apply(); }));
            panel.Controls.Add(MakeCheck("Start with Windows", _startupService.IsEnabled(), value => { _startupService.SetEnabled(value); }));
        });

        AddSection(root, "Dock Items", panel =>
        {
            _items.Height = 170;
            _items.Width = 440;
            _items.DrawMode = DrawMode.OwnerDrawFixed;
            _items.ItemHeight = 24;
            _items.DrawItem += DrawListItem;
            ApplyListTheme();
            panel.Controls.Add(_items);

            var addRow = MakeRow();
            addRow.Controls.Add(MakeButton("Add app/file", delegate { _addAppFile(); RefreshItems(); }));
            addRow.Controls.Add(MakeButton("Add folder", delegate { _addFolder(); RefreshItems(); }));
            addRow.Controls.Add(MakeButton("Add URL", delegate { _addUrl(); RefreshItems(); }));
            addRow.Controls.Add(MakeButton("Separator", delegate { _addSeparator(); RefreshItems(); }));
            panel.Controls.Add(addRow);

            var editRow = MakeRow();
            editRow.Controls.Add(MakeButton("Up", delegate { MoveSelected(-1); }));
            editRow.Controls.Add(MakeButton("Down", delegate { MoveSelected(1); }));
            editRow.Controls.Add(MakeButton("Rename", RenameSelected));
            editRow.Controls.Add(MakeButton("Remove", RemoveSelected));
            panel.Controls.Add(editRow);
        });
    }

    private void AddSection(Control parent, string title, Action<FlowLayoutPanel> build)
    {
        var panel = new FlowLayoutPanel
        {
            Width = 460,
            AutoSize = true,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Padding = new Padding(12),
            Margin = new Padding(0, 0, 0, 12),
            BackColor = _palette.Panel
        };
        panel.Controls.Add(new Label { Text = title, AutoSize = true, Font = new Font(Font, FontStyle.Bold), ForeColor = _palette.Text, Margin = new Padding(0, 0, 0, 8) });
        build(panel);
        parent.Controls.Add(panel);
    }

    private Control MakeCombo(string label, string[] values, string current, Action<string> change)
    {
        var row = MakeLabeledRow(label);
        var combo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 220, DrawMode = DrawMode.OwnerDrawFixed };
        combo.Items.AddRange(values);
        combo.SelectedItem = values.Contains(current) ? current : values[0];
        combo.BackColor = _palette.PanelAlt;
        combo.ForeColor = _palette.Text;
        combo.DrawItem += DrawComboItem;
        combo.SelectedIndexChanged += delegate { change(combo.SelectedItem?.ToString() ?? values[0]); };
        row.Controls.Add(combo);
        return row;
    }

    private Control MakeNumber(string label, int value, int min, int max, Action<int> change)
    {
        var row = MakeLabeledRow(label);
        var number = new NumericUpDown { Width = 90, Minimum = min, Maximum = max, Value = value, BackColor = _palette.PanelAlt, ForeColor = _palette.Text };
        number.ValueChanged += delegate { change((int)number.Value); };
        row.Controls.Add(number);
        return row;
    }

    private Control MakePercent(string label, double value, Action<double> change)
    {
        var row = MakeLabeledRow(label);
        var track = new TrackBar { Width = 220, Minimum = 0, Maximum = 100, TickFrequency = 10, Value = Math.Clamp((int)(value * 100), 0, 100) };
        track.ValueChanged += delegate { change(track.Value / 100.0); };
        row.Controls.Add(track);
        return row;
    }

    private CheckBox MakeCheck(string text, bool value, Action<bool> change)
    {
        var check = new CheckBox { Text = text, Checked = value, AutoSize = true, ForeColor = _palette.Text, Margin = new Padding(0, 4, 0, 4) };
        check.CheckedChanged += delegate { change(check.Checked); };
        return check;
    }

    private FlowLayoutPanel MakeLabeledRow(string label)
    {
        var row = MakeRow();
        row.Controls.Add(new Label { Text = label, Width = 140, ForeColor = _palette.MutedText, TextAlign = ContentAlignment.MiddleLeft });
        return row;
    }

    private FlowLayoutPanel MakeRow()
    {
        return new FlowLayoutPanel { Width = 430, Height = 36, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, Margin = new Padding(0, 0, 0, 6) };
    }

    private Button MakeButton(string text, EventHandler click)
    {
        var button = new Button { Text = text, Height = 30, AutoSize = true, FlatStyle = FlatStyle.Flat, BackColor = _palette.PanelAlt, ForeColor = _palette.Text, Margin = new Padding(0, 0, 6, 0) };
        button.FlatAppearance.BorderColor = _palette.Border;
        button.Click += click;
        return button;
    }

    private void RefreshItems()
    {
        _items.Items.Clear();
        foreach (var item in _config.Items)
        {
            _items.Items.Add(item);
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
        _apply();
        RefreshItems();
        _items.SelectedIndex = newIndex;
    }

    private void RenameSelected(object? sender, EventArgs e)
    {
        if (_items.SelectedItem is not DockItem item)
        {
            return;
        }
        using var dialog = new InputDialog("Rename", "Name", item.Name, _palette);
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            item.Name = dialog.Value;
            _apply();
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
        _apply();
        RefreshItems();
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
        using var background = new SolidBrush(selected ? _palette.Accent : _palette.PanelAlt);
        using var foreground = new SolidBrush(selected ? _palette.AccentText : _palette.Text);
        e.Graphics.FillRectangle(background, e.Bounds);
        e.Graphics.DrawString(_items.Items[e.Index].ToString(), e.Font ?? Font, foreground, e.Bounds.Left + 6, e.Bounds.Top + 4);
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
        e.Graphics.DrawString(combo.Items[e.Index]?.ToString() ?? "", e.Font ?? Font, foreground, e.Bounds.Left + 6, e.Bounds.Top + 3);
    }
}
