using TidyDock.WinForms.Core;
using TidyDock.WinForms.Services;
using TidyDock.WinForms.Shell;

namespace TidyDock.WinForms.UI;

internal sealed class FolderGroupManagerForm : Form
{
    private readonly DockItem _item;
    private readonly FolderPanelSettings _settings;
    private readonly ThemePalette _palette;
    private readonly List<FolderGroup> _groups;
    private readonly List<FolderEntry> _entries = [];
    private readonly ListBox _groupList = new();
    private readonly ListView _entryList = new();
    private readonly TextBox _searchBox = new();
    private readonly ToggleSwitch _showUngroupedOnly = new();
    private readonly ToggleSwitch _showCurrentGroupOnly = new();
    private readonly Label _status = new();
    private bool _updatingFilters;
    private string _entryReadNote = "";

    public FolderGroupManagerForm(DockItem item, FolderPanelSettings settings, ThemePalette palette)
    {
        _item = item;
        _settings = settings;
        _palette = palette;
        _groups = CloneGroups(item.FolderGroups);

        Text = "\u7ba1\u7406\u5206\u7ec4";
        Width = 760;
        Height = 520;
        MinimumSize = new Size(700, 460);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = _palette.Window;
        ForeColor = _palette.Text;
        Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular, GraphicsUnit.Point);

        LoadEntries();
        Build();
        RefreshGroups();
        RefreshEntries();
    }

    public List<FolderGroup> Groups => CloneGroups(_groups);

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        _ = DwmBackdrop.TryApply(Handle, _palette.IsDark);
        _ = DwmBackdrop.TryApplyControlTheme(_entryList.Handle, _palette.IsDark);
        _ = DwmBackdrop.TryApplyControlTheme(_groupList.Handle, _palette.IsDark);
    }

    private void Build()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(16),
            ColumnCount = 2,
            RowCount = 3,
            BackColor = _palette.Window
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 210));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        Controls.Add(root);

        var title = new Label
        {
            Text = "\u7ba1\u7406\u5206\u7ec4\uff1a" + (string.IsNullOrWhiteSpace(_item.Name) ? "\u6587\u4ef6\u5939" : _item.Name),
            Dock = DockStyle.Fill,
            ForeColor = _palette.Text,
            BackColor = _palette.Window,
            Font = new Font(Font, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        };
        root.Controls.Add(title, 0, 0);
        root.SetColumnSpan(title, 2);

        root.Controls.Add(BuildGroupPanel(), 0, 1);
        root.Controls.Add(BuildEntryPanel(), 1, 1);

        var footer = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            BackColor = _palette.Window
        };
        footer.Controls.Add(MakeButton("\u786e\u5b9a", delegate { DialogResult = DialogResult.OK; }, primary: true));
        footer.Controls.Add(MakeButton("\u53d6\u6d88", delegate { DialogResult = DialogResult.Cancel; }));
        root.Controls.Add(footer, 0, 2);
        root.SetColumnSpan(footer, 2);
    }

    private Control BuildGroupPanel()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 3,
            ColumnCount = 1,
            BackColor = _palette.Panel,
            Padding = new Padding(10),
            Margin = new Padding(0, 0, 12, 0)
        };
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 116));

        panel.Controls.Add(MakeTitle("\u5206\u7ec4"), 0, 0);

        _groupList.Dock = DockStyle.Fill;
        _groupList.BackColor = _palette.PanelAlt;
        _groupList.ForeColor = _palette.Text;
        _groupList.BorderStyle = BorderStyle.None;
        _groupList.SelectedIndexChanged += delegate
        {
            UpdateFilterControlState();
            if (_showCurrentGroupOnly.Checked)
            {
                RefreshEntries();
            }
            else
            {
                UpdateEntryRows();
            }
        };
        panel.Controls.Add(_groupList, 0, 1);

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            BackColor = _palette.Panel
        };
        buttons.Controls.Add(MakeButton("\u65b0\u5efa", AddGroup));
        buttons.Controls.Add(MakeButton("\u91cd\u547d\u540d", RenameGroup));
        buttons.Controls.Add(MakeButton("\u5220\u9664", DeleteGroup, danger: true));
        buttons.Controls.Add(MakeButton("\u4e0a\u79fb", delegate { MoveGroup(-1); }));
        buttons.Controls.Add(MakeButton("\u4e0b\u79fb", delegate { MoveGroup(1); }));
        panel.Controls.Add(buttons, 0, 2);
        return panel;
    }

    private Control BuildEntryPanel()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 5,
            ColumnCount = 1,
            BackColor = _palette.Panel,
            Padding = new Padding(10),
            Margin = new Padding(0)
        };
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));

        panel.Controls.Add(MakeTitle("\u5f53\u524d\u6587\u4ef6\u5939\u9879\u76ee"), 0, 0);
        panel.Controls.Add(BuildFilterRow(), 0, 1);
        _status.Dock = DockStyle.Fill;
        _status.ForeColor = _palette.MutedText;
        _status.BackColor = _palette.Panel;
        _status.TextAlign = ContentAlignment.MiddleLeft;
        panel.Controls.Add(_status, 0, 2);

        _entryList.Dock = DockStyle.Fill;
        _entryList.View = View.Details;
        _entryList.FullRowSelect = true;
        _entryList.MultiSelect = true;
        _entryList.CheckBoxes = true;
        _entryList.HideSelection = false;
        _entryList.BackColor = _palette.PanelAlt;
        _entryList.ForeColor = _palette.Text;
        _entryList.BorderStyle = BorderStyle.None;
        _entryList.Columns.Add("\u540d\u79f0", 245);
        _entryList.Columns.Add("\u7c7b\u578b", 72);
        _entryList.Columns.Add("\u6240\u5c5e\u5206\u7ec4", 150);
        _entryList.ItemChecked += delegate { UpdateEntryStatus(); };
        panel.Controls.Add(_entryList, 0, 3);

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            BackColor = _palette.Panel
        };
        buttons.Controls.Add(MakeButton("\u52a0\u5165\u5f53\u524d\u5206\u7ec4", AssignSelectedToGroup, primary: true));
        buttons.Controls.Add(MakeButton("\u79fb\u51fa\u5206\u7ec4", RemoveSelectedFromGroups));
        buttons.Controls.Add(MakeButton("\u5168\u9009", CheckAllEntries));
        buttons.Controls.Add(MakeButton("\u6e05\u7a7a\u9009\u62e9", ClearEntryChecks));
        panel.Controls.Add(buttons, 0, 4);
        return panel;
    }

    private Control BuildFilterRow()
    {
        var row = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = _palette.Panel,
            Margin = new Padding(0, 0, 0, 6)
        };
        row.Controls.Add(new Label
        {
            Text = "\u641c\u7d22",
            Width = 42,
            Height = 30,
            ForeColor = _palette.MutedText,
            BackColor = _palette.Panel,
            TextAlign = ContentAlignment.MiddleLeft
        });

        _searchBox.Width = 180;
        _searchBox.Height = 28;
        _searchBox.BorderStyle = BorderStyle.FixedSingle;
        _searchBox.BackColor = _palette.PanelAlt;
        _searchBox.ForeColor = _palette.Text;
        _searchBox.Margin = new Padding(0, 2, 12, 0);
        _searchBox.TextChanged += delegate { RefreshEntries(); };
        row.Controls.Add(_searchBox);

        _showUngroupedOnly.Text = "\u53ea\u770b\u672a\u5206\u7ec4";
        _showUngroupedOnly.Width = 138;
        _showUngroupedOnly.Palette = _palette;
        _showUngroupedOnly.BackColor = _palette.Panel;
        _showUngroupedOnly.CheckedChanged += delegate
        {
            if (_updatingFilters)
            {
                return;
            }

            if (_showUngroupedOnly.Checked && _showCurrentGroupOnly.Checked)
            {
                _updatingFilters = true;
                _showCurrentGroupOnly.Checked = false;
                _updatingFilters = false;
            }
            RefreshEntries();
        };
        row.Controls.Add(_showUngroupedOnly);

        _showCurrentGroupOnly.Text = "\u53ea\u770b\u5f53\u524d\u5206\u7ec4";
        _showCurrentGroupOnly.Width = 150;
        _showCurrentGroupOnly.Palette = _palette;
        _showCurrentGroupOnly.BackColor = _palette.Panel;
        _showCurrentGroupOnly.CheckedChanged += delegate
        {
            if (_updatingFilters)
            {
                return;
            }

            if (_showCurrentGroupOnly.Checked && _showUngroupedOnly.Checked)
            {
                _updatingFilters = true;
                _showUngroupedOnly.Checked = false;
                _updatingFilters = false;
            }
            RefreshEntries();
        };
        row.Controls.Add(_showCurrentGroupOnly);
        return row;
    }

    private void LoadEntries()
    {
        var result = new FolderService().Read(_item.Target, _settings);
        if (!string.IsNullOrWhiteSpace(result.ErrorMessage))
        {
            _entryReadNote = result.ErrorMessage;
            _status.Text = _entryReadNote;
            return;
        }

        _entries.Clear();
        _entries.AddRange(result.Entries);
        _entryReadNote = result.IsTruncated
            ? string.Format("\u4ec5\u8bfb\u53d6\u524d {0} \u9879\uff0c\u8d85\u51fa\u90e8\u5206\u4e0d\u5728\u6b64\u5904\u7ba1\u7406", _settings.MaxItems)
            : "";
    }

    private void RefreshGroups()
    {
        var selected = _groupList.SelectedIndex;
        _groupList.BeginUpdate();
        _groupList.Items.Clear();
        foreach (var group in _groups)
        {
            _groupList.Items.Add(group.Name);
        }
        _groupList.EndUpdate();

        if (_groups.Count == 0)
        {
            _groupList.SelectedIndex = -1;
        }
        else
        {
            _groupList.SelectedIndex = Math.Clamp(selected, 0, _groups.Count - 1);
        }
        UpdateFilterControlState();
    }

    private void RefreshEntries()
    {
        var topKey = (_entryList.TopItem?.Tag as FolderEntry)?.EntryKey;
        var focusedKey = (_entryList.FocusedItem?.Tag as FolderEntry)?.EntryKey;
        var selectedKeys = _entryList.SelectedItems
            .Cast<ListViewItem>()
            .Select(row => (row.Tag as FolderEntry)?.EntryKey)
            .Where(key => !string.IsNullOrWhiteSpace(key))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var checkedKeys = _entryList.CheckedItems
            .Cast<ListViewItem>()
            .Select(row => (row.Tag as FolderEntry)?.EntryKey)
            .Where(key => !string.IsNullOrWhiteSpace(key))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var entries = FilteredEntries().ToList();

        _entryList.BeginUpdate();
        _entryList.Items.Clear();
        foreach (var entry in entries)
        {
            var row = new ListViewItem(entry.Name) { Tag = entry, ToolTipText = entry.Path };
            row.SubItems.Add(entry.IsDirectory ? "\u6587\u4ef6\u5939" : "\u6587\u4ef6");
            row.SubItems.Add(GetGroupName(entry.EntryKey));
            row.Checked = checkedKeys.Contains(entry.EntryKey);
            row.Selected = selectedKeys.Contains(entry.EntryKey);
            _entryList.Items.Add(row);
        }
        _entryList.EndUpdate();
        RestoreEntryListPosition(topKey, focusedKey);
        UpdateEntryStatus(entries.Count);
    }

    private void AddGroup(object? sender, EventArgs e)
    {
        using var dialog = new InputDialog("\u65b0\u5efa\u5206\u7ec4", "\u5206\u7ec4\u540d\u79f0", "", _palette);
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        var name = dialog.Value.Trim();
        if (string.IsNullOrWhiteSpace(name) || _groups.Any(group => string.Equals(group.Name, name, StringComparison.CurrentCultureIgnoreCase)))
        {
            return;
        }

        _groups.Add(new FolderGroup { Name = name });
        RefreshGroups();
        _groupList.SelectedIndex = _groups.Count - 1;
    }

    private void RenameGroup(object? sender, EventArgs e)
    {
        var group = SelectedGroup();
        if (group is null)
        {
            return;
        }

        using var dialog = new InputDialog("\u91cd\u547d\u540d\u5206\u7ec4", "\u5206\u7ec4\u540d\u79f0", group.Name, _palette);
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        var name = dialog.Value.Trim();
        if (string.IsNullOrWhiteSpace(name) || _groups.Any(other => !ReferenceEquals(other, group) && string.Equals(other.Name, name, StringComparison.CurrentCultureIgnoreCase)))
        {
            return;
        }

        group.Name = name;
        RefreshGroups();
        RefreshEntryAssignments();
    }

    private void DeleteGroup(object? sender, EventArgs e)
    {
        var index = _groupList.SelectedIndex;
        if (index < 0 || index >= _groups.Count)
        {
            return;
        }

        _groups.RemoveAt(index);
        RefreshGroups();
        RefreshEntryAssignments();
    }

    private void MoveGroup(int delta)
    {
        var index = _groupList.SelectedIndex;
        var next = index + delta;
        if (index < 0 || next < 0 || next >= _groups.Count)
        {
            return;
        }

        var group = _groups[index];
        _groups.RemoveAt(index);
        _groups.Insert(next, group);
        RefreshGroups();
        _groupList.SelectedIndex = next;
    }

    private void AssignSelectedToGroup(object? sender, EventArgs e)
    {
        var group = SelectedGroup();
        var rows = TargetEntryRows();
        if (group is null || rows.Count == 0)
        {
            return;
        }

        var changedKeys = new List<string>();
        foreach (var row in rows)
        {
            if (row.Tag is not FolderEntry entry)
            {
                continue;
            }

            RemoveKeyFromAllGroups(entry.EntryKey);
            if (!group.EntryKeys.Contains(entry.EntryKey, StringComparer.OrdinalIgnoreCase))
            {
                group.EntryKeys.Add(entry.EntryKey);
            }
            changedKeys.Add(entry.EntryKey);
        }

        RefreshEntryAssignments(changedKeys);
        ClearEntryChecks();
    }

    private void RemoveSelectedFromGroups(object? sender, EventArgs e)
    {
        var rows = TargetEntryRows();
        if (rows.Count == 0)
        {
            return;
        }

        var changedKeys = new List<string>();
        foreach (var row in rows)
        {
            if (row.Tag is FolderEntry entry)
            {
                RemoveKeyFromAllGroups(entry.EntryKey);
                changedKeys.Add(entry.EntryKey);
            }
        }

        RefreshEntryAssignments(changedKeys);
        ClearEntryChecks();
    }

    private void RefreshEntryAssignments(IEnumerable<string>? entryKeys = null)
    {
        if (HasActiveEntryFilter())
        {
            RefreshEntries();
            return;
        }

        UpdateEntryRows(entryKeys);
    }

    private IEnumerable<FolderEntry> FilteredEntries()
    {
        var query = _searchBox.Text.Trim();
        var currentGroup = SelectedGroup();
        var onlyUngrouped = _showUngroupedOnly.Checked;
        var onlyCurrentGroup = _showCurrentGroupOnly.Checked && currentGroup is not null;

        foreach (var entry in _entries)
        {
            if (!string.IsNullOrWhiteSpace(query)
                && !entry.Name.Contains(query, StringComparison.CurrentCultureIgnoreCase)
                && !entry.Path.Contains(query, StringComparison.CurrentCultureIgnoreCase))
            {
                continue;
            }

            if (onlyUngrouped && !string.Equals(GetGroupName(entry.EntryKey), "\u672a\u5206\u7ec4", StringComparison.Ordinal))
            {
                continue;
            }

            if (onlyCurrentGroup && !currentGroup!.EntryKeys.Contains(entry.EntryKey, StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            yield return entry;
        }
    }

    private bool HasActiveEntryFilter()
    {
        return !string.IsNullOrWhiteSpace(_searchBox.Text)
            || _showUngroupedOnly.Checked
            || _showCurrentGroupOnly.Checked;
    }

    private void UpdateEntryStatus(int? visibleCount = null)
    {
        if (_status.IsDisposed)
        {
            return;
        }

        var visible = visibleCount ?? _entryList.Items.Count;
        var checkedCount = _entryList.CheckedItems.Count;
        var note = string.IsNullOrWhiteSpace(_entryReadNote) ? "" : "\uff0c" + _entryReadNote;
        _status.Text = $"\u663e\u793a {visible} / {_entries.Count} \u9879\uff0c\u5df2\u52fe\u9009 {checkedCount} \u9879{note}";
    }

    private void UpdateFilterControlState()
    {
        var hasGroup = SelectedGroup() is not null;
        _showCurrentGroupOnly.Enabled = hasGroup;
        if (!hasGroup && _showCurrentGroupOnly.Checked)
        {
            _updatingFilters = true;
            _showCurrentGroupOnly.Checked = false;
            _updatingFilters = false;
            RefreshEntries();
        }
    }

    private List<ListViewItem> TargetEntryRows()
    {
        if (_entryList.CheckedItems.Count > 0)
        {
            return _entryList.CheckedItems.Cast<ListViewItem>().ToList();
        }

        return _entryList.SelectedItems.Cast<ListViewItem>().ToList();
    }

    private void CheckAllEntries(object? sender, EventArgs e)
    {
        _entryList.BeginUpdate();
        foreach (ListViewItem row in _entryList.Items)
        {
            row.Checked = true;
        }
        _entryList.EndUpdate();
        UpdateEntryStatus();
    }

    private void ClearEntryChecks(object? sender, EventArgs e)
    {
        ClearEntryChecks();
    }

    private void ClearEntryChecks()
    {
        _entryList.BeginUpdate();
        foreach (ListViewItem row in _entryList.Items)
        {
            row.Checked = false;
            row.Selected = false;
        }
        _entryList.EndUpdate();
        UpdateEntryStatus();
    }

    private void UpdateEntryRows(IEnumerable<string>? entryKeys = null)
    {
        if (_entryList.Items.Count == 0)
        {
            return;
        }

        var keys = entryKeys?.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var topKey = (_entryList.TopItem?.Tag as FolderEntry)?.EntryKey;
        var focusedKey = (_entryList.FocusedItem?.Tag as FolderEntry)?.EntryKey;

        _entryList.BeginUpdate();
        foreach (ListViewItem row in _entryList.Items)
        {
            if (row.Tag is not FolderEntry entry)
            {
                continue;
            }

            if (keys is not null && !keys.Contains(entry.EntryKey))
            {
                continue;
            }

            row.SubItems[2].Text = GetGroupName(entry.EntryKey);
        }
        _entryList.EndUpdate();

        RestoreEntryListPosition(topKey, focusedKey);
        UpdateEntryStatus();
    }

    private void RestoreEntryListPosition(string? topKey, string? focusedKey)
    {
        if (!string.IsNullOrWhiteSpace(focusedKey))
        {
            var focused = FindEntryRow(focusedKey);
            if (focused is not null)
            {
                focused.Focused = true;
            }
        }

        if (!string.IsNullOrWhiteSpace(topKey))
        {
            var top = FindEntryRow(topKey);
            if (top is not null)
            {
                _entryList.TopItem = top;
            }
        }
    }

    private ListViewItem? FindEntryRow(string entryKey)
    {
        foreach (ListViewItem row in _entryList.Items)
        {
            if (row.Tag is FolderEntry entry && string.Equals(entry.EntryKey, entryKey, StringComparison.OrdinalIgnoreCase))
            {
                return row;
            }
        }

        return null;
    }

    private FolderGroup? SelectedGroup()
    {
        var index = _groupList.SelectedIndex;
        return index >= 0 && index < _groups.Count ? _groups[index] : null;
    }

    private string GetGroupName(string key)
    {
        var group = _groups.FirstOrDefault(item => item.EntryKeys.Contains(key, StringComparer.OrdinalIgnoreCase));
        return group?.Name ?? "\u672a\u5206\u7ec4";
    }

    private void RemoveKeyFromAllGroups(string key)
    {
        foreach (var group in _groups)
        {
            group.EntryKeys.RemoveAll(item => string.Equals(item, key, StringComparison.OrdinalIgnoreCase));
        }
    }

    private Label MakeTitle(string text)
    {
        return new Label
        {
            Text = text,
            Dock = DockStyle.Fill,
            ForeColor = _palette.Text,
            BackColor = _palette.Panel,
            Font = new Font(Font, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        };
    }

    private GlassButton MakeButton(string text, EventHandler click, bool primary = false, bool danger = false)
    {
        var button = new GlassButton
        {
            Text = text,
            Width = Math.Max(72, TextRenderer.MeasureText(text, Font).Width + 26),
            Height = 30,
            Palette = _palette,
            Primary = primary,
            Danger = danger,
            BackColor = _palette.Panel,
            Margin = new Padding(0, 5, 8, 0)
        };
        button.Click += click;
        return button;
    }

    private static List<FolderGroup> CloneGroups(IEnumerable<FolderGroup> groups)
    {
        return groups
            .Select(group => new FolderGroup
            {
                Id = string.IsNullOrWhiteSpace(group.Id) ? Guid.NewGuid().ToString("N") : group.Id,
                Name = group.Name,
                EntryKeys = group.EntryKeys
                    .Where(key => !string.IsNullOrWhiteSpace(key))
                    .Select(key => key.Trim().Replace('\\', '/').TrimStart('/'))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList()
            })
            .Where(group => !string.IsNullOrWhiteSpace(group.Name))
            .ToList();
    }
}
