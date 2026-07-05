using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using TidyDock.WinForms.Core;
using TidyDock.WinForms.Services;
using TidyDock.WinForms.Shell;

namespace TidyDock.WinForms.UI;

internal sealed class SettingsForm : Form
{
    private const string ThisPcTarget = "shell:MyComputerFolder";
    private const int FormDefaultWidth = 960;
    private const int FormMinWidth = 860;
    private const int CardWidth = 860;
    private const int SectionWidth = 830;
    private const int RowWidth = 830;
    private const int ItemTableHeaderHeight = 30;
    private const int ItemTableRowHeight = 34;
    private const int ItemTableMinRows = 4;

    private readonly AppConfig _config;
    private readonly AppConfig _originalConfig;
    private readonly IconService _iconService;
    private readonly ShortcutService _shortcutService;
    private readonly StartupService _startupService;
    private readonly DataBackupService _backupService;
    private readonly Action<AppConfig> _preview;
    private readonly Action<AppConfig> _commit;
    private readonly Action<AppConfig> _cancel;
    private AppConfig _lastPreviewConfig;
    private bool _completed;
    private bool _hasPreviewed;
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
    private ListView _items = new();
    private FlowLayoutPanel? _scrollRoot;
    private Label? _statusLabel;
    private GlassButton? _previewButton;
    private GlassButton? _confirmButton;
    private GlassButton? _renameButton;
    private GlassButton? _manageGroupsButton;
    private GlassButton? _removeButton;
    private GlassButton? _moreItemButton;
    private ThemePalette _palette;

    public SettingsForm(AppConfig config, AppConfig originalConfig, IconService iconService, ShortcutService shortcutService, StartupService startupService, DataBackupService backupService, Action<AppConfig> preview, Action<AppConfig> commit, Action<AppConfig> cancel)
    {
        _config = config;
        _originalConfig = originalConfig;
        _iconService = iconService;
        _shortcutService = shortcutService;
        _startupService = startupService;
        _backupService = backupService;
        _preview = preview;
        _commit = commit;
        _cancel = cancel;
        _lastPreviewConfig = ConfigService.Clone(originalConfig);
        _palette = Theme.Get(_config.Dock.Theme);

        Text = "TidyDock";
        Width = FormDefaultWidth;
        Height = 650;
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(FormMinWidth, 560);
        KeyPreview = true;
        AccessibleName = "TidyDock 设置";
        AccessibleRole = AccessibleRole.Dialog;
        Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
        ApplyTheme();
        Build();
        RefreshItems();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
        }

        base.Dispose(disposing);
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (!_completed)
        {
            if (ShouldConfirmClose())
            {
                var result = MessageBox.Show(
                    "\u653e\u5f03\u672c\u6b21\u4fee\u6539\u5e76\u6062\u590d\u6253\u5f00\u8bbe\u7f6e\u524d\u7684 Dock \u72b6\u6001\uff1f",
                    "TidyDock",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning,
                    MessageBoxDefaultButton.Button2);
                if (result != DialogResult.Yes)
                {
                    e.Cancel = true;
                    return;
                }
            }

            _completed = true;
            _cancel(_originalConfig);
        }

        base.OnFormClosing(e);
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        _ = DwmBackdrop.TryApply(Handle, _palette.IsDark);
        ApplyNativeScrollTheme();
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

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == Keys.Escape)
        {
            CancelSettings(this, EventArgs.Empty);
            return true;
        }

        if (keyData == (Keys.Control | Keys.P))
        {
            if (_previewButton?.Enabled == true)
            {
                PreviewSettings(this, EventArgs.Empty);
            }
            return true;
        }

        if (keyData == (Keys.Control | Keys.S))
        {
            if (_confirmButton?.Enabled == true)
            {
                ConfirmSettings(this, EventArgs.Empty);
            }
            return true;
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }

    private void Build()
    {
        Controls.Clear();
        ApplyTheme();

        var shell = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 3,
            ColumnCount = 1,
            BackColor = _palette.Window
        };
        shell.RowStyles.Add(new RowStyle(SizeType.Absolute, 92));
        shell.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        shell.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));
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
        ApplyNativeScrollTheme();

        BuildDockItemsSection(root);

        AddSection(root, "\u5916\u89c2", panel =>
        {
            panel.Controls.Add(MakeCombo("\u4e3b\u9898", _themeOptions, _config.Dock.Theme, value =>
            {
                _config.Dock.Theme = value;
                _palette = Theme.Get(value);
                ApplyTheme();
                var scrollY = CurrentScrollY();
                Build();
                RestoreScrollLater(scrollY);
            }));
            panel.Controls.Add(MakeCombo("\u4f4d\u7f6e", _positionOptions, _config.Dock.Position, value => { _config.Dock.Position = value; }));
            panel.Controls.Add(MakeCombo("\u663e\u793a\u5668", GetDisplayOptions(), _config.Dock.Display, value => { _config.Dock.Display = value; }));
            panel.Controls.Add(MakeNumber("\u56fe\u6807\u5927\u5c0f", _config.Dock.IconSize, 32, 96, "\u50cf\u7d20", value => { _config.Dock.IconSize = value; }));
            panel.Controls.Add(MakeNumber("\u56fe\u6807\u95f4\u8ddd", _config.Dock.IconGap, 0, 32, "\u50cf\u7d20", value => { _config.Dock.IconGap = value; }));
            panel.Controls.Add(MakeNumber("\u5706\u89d2\u534a\u5f84", _config.Dock.CornerRadius, 6, 40, "\u50cf\u7d20", value => { _config.Dock.CornerRadius = value; }));
            panel.Controls.Add(MakePercent("\u900f\u660e\u5ea6", _config.Dock.Opacity, value => { _config.Dock.Opacity = value; }));
            panel.Controls.Add(MakePercent("\u60ac\u505c\u653e\u5927", _config.Dock.Magnification - 1, value => { _config.Dock.Magnification = 1 + value; }));
            panel.Controls.Add(MakeCheck("\u589e\u5f3a\u5e95\u5ea7\u5bf9\u6bd4\u5ea6", _config.Dock.HighContrastDock, value => { _config.Dock.HighContrastDock = value; }));
        });

        AddSection(root, "\u884c\u4e3a", panel =>
        {
            panel.Controls.Add(MakeCheck("\u81ea\u52a8\u9690\u85cf", _config.Dock.AutoHide, value => { _config.Dock.AutoHide = value; }));
            panel.Controls.Add(MakeCheck("\u59cb\u7ec8\u7f6e\u9876", _config.Dock.AlwaysOnTop, value => { _config.Dock.AlwaysOnTop = value; }));
            panel.Controls.Add(MakeCheck("\u542f\u52a8\u540e\u663e\u793a Dock", _config.Dock.StartVisible, value => { _config.Dock.StartVisible = value; }));
            panel.Controls.Add(MakeCheck("\u663e\u793a\u6258\u76d8\u56fe\u6807", _config.Dock.ShowTrayIcon, value => { _config.Dock.ShowTrayIcon = value; }));
            panel.Controls.Add(MakeCheck("\u5141\u8bb8\u62d6\u62fd\u6392\u5e8f", _config.Dock.EnableDragReorder, value => { _config.Dock.EnableDragReorder = value; }));
            panel.Controls.Add(MakeCheck("\u8ddf\u968f Windows \u542f\u52a8", _startupService.IsEnabled(), value => { _startupService.SetEnabled(value); }, trackDraft: false));
        });

        AddSection(root, "\u6587\u4ef6\u5939\u9762\u677f", panel =>
        {
            panel.Controls.Add(MakeNumber("\u6700\u5927\u5c55\u793a\u6570", _config.FolderPanel.MaxItems, 20, 1000, "\u9879", value => { _config.FolderPanel.MaxItems = value; }));
            panel.Controls.Add(MakeNumber("\u9762\u677f\u6700\u5927\u9ad8\u5ea6", _config.FolderPanel.MaxHeight, 240, 760, "\u50cf\u7d20", value => { _config.FolderPanel.MaxHeight = value; }));
            panel.Controls.Add(MakeCheck("\u663e\u793a\u9690\u85cf\u6587\u4ef6", _config.FolderPanel.ShowHiddenFiles, value => { _config.FolderPanel.ShowHiddenFiles = value; }));
        });

        AddSection(root, "\u6570\u636e\u4e0e\u5907\u4efd", panel =>
        {
            panel.Controls.Add(MakeInfoText($"\u914d\u7f6e\u4f4d\u7f6e\uff1a{_backupService.DataDirectory}"));
            var backupRow = MakeRow();
            backupRow.Controls.Add(MakeButton("\u6253\u5f00\u914d\u7f6e\u6587\u4ef6\u5939", OpenConfigFolder));
            backupRow.Controls.Add(MakeButton("\u5bfc\u51fa\u5907\u4efd", ExportBackup, "primary"));
            backupRow.Controls.Add(MakeButton("\u5bfc\u5165\u5907\u4efd", ImportBackup));
            backupRow.Controls.Add(MakeButton("\u6062\u590d\u6700\u8fd1\u5907\u4efd", RestoreLatestBackup));
            backupRow.Controls.Add(MakeButton("\u590d\u5236\u8bca\u65ad\u4fe1\u606f", CopyDiagnosticInfo));
            panel.Controls.Add(backupRow);
        });

        shell.Controls.Add(BuildFooter(), 0, 2);
        UpdateFooterState();
    }

    private Control BuildFooter()
    {
        var footer = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Padding = new Padding(22, 10, 22, 10),
            BackColor = _palette.Window
        };
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        _statusLabel = new Label
        {
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = _palette.MutedText,
            BackColor = _palette.Window,
            AutoEllipsis = true
        };
        footer.Controls.Add(_statusLabel, 0, 0);

        var buttons = new FlowLayoutPanel
        {
            AutoSize = true,
            Dock = DockStyle.Right,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            BackColor = _palette.Window
        };
        _confirmButton = MakeFooterButton("\u4fdd\u5b58\u5e76\u5173\u95ed", ConfirmSettings, primary: true);
        _previewButton = MakeFooterButton("\u9884\u89c8\u5230 Dock", PreviewSettings);
        buttons.Controls.Add(_confirmButton);
        buttons.Controls.Add(MakeFooterButton("\u53d6\u6d88", CancelSettings));
        buttons.Controls.Add(_previewButton);
        footer.Controls.Add(buttons, 1, 0);
        return footer;
    }

    private void BuildDockItemsSection(Control root)
    {
        AddSection(root, "Dock \u9879", panel =>
        {
            _items = new ListView
            {
                Height = 260,
                Width = RowWidth,
                View = View.Details,
                FullRowSelect = true,
                HideSelection = false,
                MultiSelect = false,
                OwnerDraw = true,
                Scrollable = false,
                ShowItemToolTips = true,
                UseCompatibleStateImageBehavior = false,
                HeaderStyle = ColumnHeaderStyle.Nonclickable,
                BorderStyle = BorderStyle.None,
                SmallImageList = new ImageList { ImageSize = new Size(1, ItemTableRowHeight), ColorDepth = ColorDepth.Depth32Bit }
            };
            _items.AccessibleName = "Dock 项列表";
            _items.AccessibleRole = AccessibleRole.Table;
            _items.Columns.Add("\u7c7b\u578b", 78);
            _items.Columns.Add("\u540d\u79f0", 170);
            _items.Columns.Add("\u76ee\u6807", 362);
            _items.Columns.Add("\u5706\u89d2", 110);
            _items.Columns.Add("\u6253\u5f00", 110);
            _items.DrawColumnHeader += DrawItemTableHeader;
            _items.DrawSubItem += DrawItemTableCell;
            _items.MouseDown += OnItemsMouseDown;
            _items.SelectedIndexChanged += delegate { UpdateItemControls(); };
            ApplyListTheme();
            panel.Controls.Add(_items);

            var addRow = MakeRow();
            addRow.Controls.Add(MakeButton("\u6dfb\u52a0\u5e94\u7528/\u6587\u4ef6", AddAppOrFile, "primary"));
            addRow.Controls.Add(MakeButton("\u6dfb\u52a0\u6587\u4ef6\u5939", AddFolder));
            addRow.Controls.Add(MakeButton("\u6dfb\u52a0\u7f51\u5740", AddUrl));
            addRow.Controls.Add(MakeButton("\u66f4\u591a\u6dfb\u52a0", ShowMoreAddMenu));
            panel.Controls.Add(addRow);

            var editRow = MakeRow();
            _renameButton = MakeButton("\u91cd\u547d\u540d", RenameSelected);
            _manageGroupsButton = MakeButton("\u7ba1\u7406\u5206\u7ec4", ManageFolderGroupsSelected);
            _removeButton = MakeButton("\u79fb\u9664", RemoveSelected, "danger");
            _moreItemButton = MakeButton("\u66f4\u591a\u64cd\u4f5c", ShowMoreItemMenu);
            editRow.Controls.Add(_renameButton);
            editRow.Controls.Add(_manageGroupsButton);
            editRow.Controls.Add(_removeButton);
            editRow.Controls.Add(_moreItemButton);
            panel.Controls.Add(editRow);
            panel.Controls.Add(MakeCheck("\u4e00\u952e\u64cd\u4f5c\u5305\u542b\u81ea\u5b9a\u4e49\u56fe\u6807", _config.Dock.WrapCustomIcons, value =>
            {
                _config.Dock.WrapCustomIcons = value;
                _iconService.ClearMemoryCache();
                RefreshItems();
            }));

            RefreshItems();
        });
    }

    private void AddSection(Control parent, string title, Action<FlowLayoutPanel> build)
    {
        var card = new CardPanel
        {
            Width = CardWidth,
            Height = 120,
            BackColor = _palette.Panel,
            FillColor = Color.FromArgb(_palette.IsDark ? 246 : 245, _palette.Panel),
            BorderColor = _palette.Border,
            Radius = 14
        };

        var panel = new FlowLayoutPanel
        {
            Width = SectionWidth,
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
        var combo = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 320, Height = 32, DrawMode = DrawMode.OwnerDrawFixed, ItemHeight = 26, AccessibleName = label, AccessibleRole = AccessibleRole.ComboBox };
        combo.Items.AddRange(values);
        combo.SelectedItem = values.FirstOrDefault(value => string.Equals(value.Value, current, StringComparison.OrdinalIgnoreCase)) ?? values[0];
        combo.BackColor = _palette.PanelAlt;
        combo.ForeColor = _palette.Text;
        combo.DrawItem += DrawComboItem;
        combo.SelectedIndexChanged += delegate
        {
            change(combo.SelectedItem is ComboOption option ? option.Value : values[0].Value);
            MarkDraftChanged();
        };
        row.Controls.Add(combo);
        return row;
    }

    private Control MakeNumber(string label, int value, int min, int max, string suffix, Action<int> change)
    {
        var row = MakeLabeledRow(label);
        var slider = new GlassSlider { Width = 420, Height = 32, Palette = _palette, Minimum = min, Maximum = max, Value = Math.Clamp(value, min, max), BackColor = _palette.Panel };
        slider.AccessibleName = label;
        slider.AccessibleRole = AccessibleRole.Slider;
        var valueLabel = new Label { Width = 78, Height = 32, ForeColor = _palette.MutedText, BackColor = _palette.Panel, TextAlign = ContentAlignment.MiddleRight, Text = FormatSliderValue(slider.Value, suffix) };
        slider.ValueChanged += delegate
        {
            valueLabel.Text = FormatSliderValue(slider.Value, suffix);
            change(slider.Value);
            MarkDraftChanged();
        };
        row.Controls.Add(slider);
        row.Controls.Add(valueLabel);
        return row;
    }

    private Control MakePercent(string label, double value, Action<double> change)
    {
        var row = MakeLabeledRow(label);
        var slider = new GlassSlider { Width = 420, Height = 32, Palette = _palette, Minimum = 0, Maximum = 100, Value = Math.Clamp((int)(value * 100), 0, 100), BackColor = _palette.Panel };
        slider.AccessibleName = label;
        slider.AccessibleRole = AccessibleRole.Slider;
        var valueLabel = new Label { Width = 54, Height = 32, ForeColor = _palette.MutedText, BackColor = _palette.Panel, TextAlign = ContentAlignment.MiddleRight, Text = slider.Value + "%" };
        slider.ValueChanged += delegate
        {
            valueLabel.Text = slider.Value + "%";
            change(slider.Value / 100.0);
            MarkDraftChanged();
        };
        row.Controls.Add(slider);
        row.Controls.Add(valueLabel);
        return row;
    }

    private ToggleSwitch MakeCheck(string text, bool value, Action<bool> change, bool trackDraft = true)
    {
        var check = new ToggleSwitch { Text = text, Checked = value, Palette = _palette, BackColor = _palette.Panel };
        check.AccessibleName = text;
        check.AccessibleRole = AccessibleRole.CheckButton;
        check.CheckedChanged += delegate
        {
            change(check.Checked);
            if (trackDraft)
            {
                MarkDraftChanged();
            }
        };
        return check;
    }

    private FlowLayoutPanel MakeLabeledRow(string label)
    {
        var row = MakeRow();
        row.Controls.Add(new Label { Text = label, Width = 170, Height = 32, ForeColor = _palette.MutedText, BackColor = _palette.Panel, TextAlign = ContentAlignment.MiddleLeft });
        return row;
    }

    private Control MakeInfoText(string text)
    {
        return new Label
        {
            Text = text,
            Width = RowWidth,
            Height = 32,
            ForeColor = _palette.MutedText,
            BackColor = _palette.Panel,
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true,
            Margin = new Padding(0, 0, 0, 8)
        };
    }

    private FlowLayoutPanel MakeRow()
    {
        return new FlowLayoutPanel { Width = RowWidth, Height = 38, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, Margin = new Padding(0, 0, 0, 8), BackColor = _palette.Panel };
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
        button.AccessibleName = text;
        button.AccessibleRole = AccessibleRole.PushButton;
        button.Click += click;
        return button;
    }

    private GlassButton MakeFooterButton(string text, EventHandler click, bool primary = false)
    {
        var button = new GlassButton
        {
            Text = text,
            Height = 34,
            Width = Math.Max(92, TextRenderer.MeasureText(text, Font).Width + 34),
            Palette = _palette,
            Primary = primary,
            BackColor = _palette.Window,
            Margin = new Padding(8, 0, 0, 0)
        };
        button.AccessibleName = text;
        button.AccessibleRole = AccessibleRole.PushButton;
        button.Click += click;
        return button;
    }

    private ContextMenuStrip CreateContextMenu()
    {
        return new ContextMenuStrip
        {
            Font = new Font("Microsoft YaHei UI", 10.5F, FontStyle.Regular, GraphicsUnit.Point),
            ShowImageMargin = false,
            Padding = new Padding(6),
            BackColor = _palette.Panel,
            ForeColor = _palette.Text
        };
    }

    private void ShowContextMenu(object? sender, ContextMenuStrip menu)
    {
        menu.Closed += delegate
        {
            try
            {
                if (!IsDisposed && IsHandleCreated)
                {
                    BeginInvoke(new Action(menu.Dispose));
                }
                else
                {
                    menu.Dispose();
                }
            }
            catch
            {
                menu.Dispose();
            }
        };
        if (sender is Control control)
        {
            menu.Show(control, new Point(0, control.Height + 2));
            return;
        }

        menu.Show(this, PointToClient(Cursor.Position));
    }

    private void PreviewSettings(object? sender, EventArgs e)
    {
        _preview(ConfigService.Clone(_config));
        _lastPreviewConfig = ConfigService.Clone(_config);
        _hasPreviewed = true;
        UpdateFooterState();
    }

    private void ConfirmSettings(object? sender, EventArgs e)
    {
        _completed = true;
        _commit(ConfigService.Clone(_config));
        Close();
    }

    private void CancelSettings(object? sender, EventArgs e)
    {
        _completed = true;
        _cancel(_originalConfig);
        Close();
    }

    private void OpenConfigFolder(object? sender, EventArgs e)
    {
        Directory.CreateDirectory(_backupService.DataDirectory);
        Process.Start(new ProcessStartInfo(_backupService.DataDirectory) { UseShellExecute = true });
    }

    private void CopyDiagnosticInfo(object? sender, EventArgs e)
    {
        try
        {
            using var process = Process.GetCurrentProcess();
            process.Refresh();
            var configPath = Path.Combine(_backupService.DataDirectory, "settings.json");
            var logPath = Path.Combine(_backupService.DataDirectory, "logs", "tidydock.log");
            var uptime = DateTime.Now - AppRuntime.StartedAt;
            var version = typeof(SettingsForm).Assembly.GetName().Version?.ToString() ?? "unknown";
            var text = string.Join(Environment.NewLine,
                "TidyDock 诊断摘要",
                $"版本：{version}",
                $"进程 ID：{process.Id}",
                $"启动耗时：{Math.Round(AppRuntime.StartupElapsed.TotalMilliseconds)} ms",
                $"运行时长：{FormatDuration(uptime)}",
                $"系统：{RuntimeInformation.OSDescription}",
                $".NET：{RuntimeInformation.FrameworkDescription}",
                $"配置目录：{_backupService.DataDirectory}",
                $"配置文件：{configPath}",
                $"日志文件：{logPath}",
                $"Dock 项数量：{_config.Items.Count}",
                $"文件夹项数量：{_config.Items.Count(item => string.Equals(item.Type, DockItemTypes.Folder, StringComparison.OrdinalIgnoreCase))}",
                $"自定义图标项数量：{_config.Items.Count(item => !string.IsNullOrWhiteSpace(item.IconPath))}",
                $"Private Memory：{Math.Round(process.PrivateMemorySize64 / 1024d / 1024d, 2)} MB",
                $"Working Set：{Math.Round(process.WorkingSet64 / 1024d / 1024d, 2)} MB",
                $"句柄数：{process.HandleCount}",
                $"生成时间：{DateTime.Now:yyyy-MM-dd HH:mm:ss}");

            Clipboard.SetText(text);
            MessageBox.Show("\u8bca\u65ad\u4fe1\u606f\u5df2\u590d\u5236\u3002", "TidyDock", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "TidyDock", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void ExportBackup(object? sender, EventArgs e)
    {
        using var dialog = new SaveFileDialog
        {
            Title = "\u5bfc\u51fa TidyDock \u5907\u4efd",
            Filter = "TidyDock \u5907\u4efd|*.zip",
            FileName = $"tidydock-backup-{DateTime.Now:yyyyMMdd-HHmmss}.zip"
        };
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        try
        {
            _backupService.ExportBackup(dialog.FileName, _config);
            MessageBox.Show("\u5907\u4efd\u5df2\u5bfc\u51fa\u3002", "TidyDock", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "TidyDock", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void ImportBackup(object? sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog
        {
            Title = "\u5bfc\u5165 TidyDock \u5907\u4efd",
            Filter = "TidyDock \u5907\u4efd|*.zip|\u6240\u6709\u6587\u4ef6|*.*"
        };
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        ImportBackupFromPath(dialog.FileName);
    }

    private void RestoreLatestBackup(object? sender, EventArgs e)
    {
        var latest = _backupService.GetLatestBackup();
        if (string.IsNullOrWhiteSpace(latest))
        {
            MessageBox.Show("\u6682\u65e0\u53ef\u6062\u590d\u7684\u81ea\u52a8\u5907\u4efd\u3002", "TidyDock", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        ImportBackupFromPath(latest);
    }

    private void ImportBackupFromPath(string path)
    {
        var result = MessageBox.Show(
            "\u5bfc\u5165\u540e\u4f1a\u66f4\u65b0\u8bbe\u7f6e\u8349\u7a3f\uff0c\u9700\u8981\u70b9\u51fb\u201c\u9884\u89c8\u201d\u6216\u201c\u786e\u5b9a\u201d\u624d\u4f1a\u5e94\u7528\u5230 Dock\u3002\u7ee7\u7eed\u5417\uff1f",
            "TidyDock",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question,
            MessageBoxDefaultButton.Button2);
        if (result != DialogResult.Yes)
        {
            return;
        }

        try
        {
            _backupService.CreateAutomaticBackup();
            var imported = _backupService.ReadConfigFromBackup(path);
            _backupService.ImportResources(path);
            ConfigService.CopyInto(_config, imported);
            _iconService.ClearMemoryCache();
            _palette = Theme.Get(_config.Dock.Theme);
            var scrollY = CurrentScrollY();
            Build();
            RestoreScrollLater(scrollY);
            MarkDraftChanged();
            MessageBox.Show("\u5907\u4efd\u5df2\u5bfc\u5165\u5230\u8349\u7a3f\uff0c\u8fd8\u9700\u8981\u70b9\u51fb\u201c\u9884\u89c8\u5230 Dock\u201d\u6216\u201c\u4fdd\u5b58\u5e76\u5173\u95ed\u201d\u624d\u4f1a\u751f\u6548\u3002", "TidyDock", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "TidyDock", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void MarkDraftChanged()
    {
        UpdateFooterState();
    }

    private void UpdateFooterState()
    {
        if (_statusLabel is null || _previewButton is null || _confirmButton is null)
        {
            return;
        }

        var draftMatchesOriginal = ConfigService.AreEquivalent(_config, _originalConfig);
        var draftMatchesPreview = ConfigService.AreEquivalent(_config, _lastPreviewConfig);
        var previewMatchesOriginal = ConfigService.AreEquivalent(_lastPreviewConfig, _originalConfig);

        _previewButton.Enabled = !draftMatchesPreview;
        _confirmButton.Enabled = !draftMatchesOriginal;

        if (!draftMatchesPreview)
        {
            _statusLabel.Text = "\u6709\u672a\u9884\u89c8\u4fee\u6539";
            _statusLabel.ForeColor = _palette.Accent;
        }
        else if (_hasPreviewed && !previewMatchesOriginal)
        {
            _statusLabel.Text = "\u5df2\u9884\u89c8\uff0c\u5c1a\u672a\u4fdd\u5b58";
            _statusLabel.ForeColor = _palette.Accent;
        }
        else
        {
            _statusLabel.Text = "\u65e0\u672a\u4fdd\u5b58\u4fee\u6539";
            _statusLabel.ForeColor = _palette.MutedText;
        }
    }

    private bool ShouldConfirmClose()
    {
        var draftMatchesOriginal = ConfigService.AreEquivalent(_config, _originalConfig);
        var previewMatchesOriginal = ConfigService.AreEquivalent(_lastPreviewConfig, _originalConfig);
        return !draftMatchesOriginal || (_hasPreviewed && !previewMatchesOriginal);
    }

    private void RefreshItems()
    {
        var selectedId = SelectedDockItem()?.Id ?? "";
        _items.BeginUpdate();
        _items.Items.Clear();
        foreach (var item in _config.Items)
        {
            _items.Items.Add(CreateDockItemRow(item));
        }
        if (!string.IsNullOrWhiteSpace(selectedId))
        {
            for (var i = 0; i < _items.Items.Count; i++)
            {
                if (_items.Items[i].Tag is DockItem item && item.Id == selectedId)
                {
                    SelectItemIndex(i);
                    break;
                }
            }
        }
        _items.EndUpdate();
        UpdateItemsTableHeight();
        UpdateItemControls();
        UpdateFooterState();
    }

    private void UpdateItemsTableHeight()
    {
        if (_items.IsDisposed)
        {
            return;
        }

        var rows = Math.Max(ItemTableMinRows, _items.Items.Count);
        _items.Height = ItemTableHeaderHeight + rows * ItemTableRowHeight + 2;

        if (_items.Parent is FlowLayoutPanel panel)
        {
            panel.Height = panel.PreferredSize.Height;
            if (panel.Parent is CardPanel card)
            {
                card.Height = panel.Height + 28;
            }
        }

        _scrollRoot?.PerformLayout();
    }

    private void AddAppOrFile(object? sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog
        {
            Multiselect = true,
            Title = "\u9009\u62e9\u8981\u6dfb\u52a0\u5230 Dock \u7684\u5e94\u7528\u6216\u6587\u4ef6",
            Filter = "\u7a0b\u5e8f\u548c\u6587\u4ef6|*.exe;*.lnk;*.*|\u6240\u6709\u6587\u4ef6|*.*"
        };
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        foreach (var file in dialog.FileNames)
        {
            AddPathToDraft(file);
        }
        RefreshItems();
    }

    private void AddFolder(object? sender, EventArgs e)
    {
        using var dialog = new FolderBrowserDialog { Description = "\u9009\u62e9\u8981\u6dfb\u52a0\u5230 TidyDock \u7684\u6587\u4ef6\u5939" };
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        AddPathToDraft(dialog.SelectedPath);
        RefreshItems();
    }

    private void AddUrl(object? sender, EventArgs e)
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
            RefreshItems();
        }
    }

    private void AddThisPc(object? sender, EventArgs e)
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
        RefreshItems();
    }

    private void ShowMoreAddMenu(object? sender, EventArgs e)
    {
        var menu = CreateContextMenu();
        menu.Items.Add("\u6dfb\u52a0\u6b64\u7535\u8111", null, AddThisPc);
        menu.Items.Add("\u6dfb\u52a0\u5206\u9694\u7b26", null, AddSeparator);
        ShowContextMenu(sender, menu);
    }

    private void ShowMoreItemMenu(object? sender, EventArgs e)
    {
        var menu = CreateContextMenu();
        var selected = SelectedDockItem();
        var selectedIndex = SelectedDockIndex();
        var hasSelection = selected is not null;
        var canChangeIcon = hasSelection && !selected!.IsSeparator;
        menu.Items.Add("\u4e0a\u79fb", null, delegate { MoveSelected(-1); }).Enabled = selectedIndex > 0;
        menu.Items.Add("\u4e0b\u79fb", null, delegate { MoveSelected(1); }).Enabled = selectedIndex >= 0 && selectedIndex < _config.Items.Count - 1;
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("\u66f4\u6362\u56fe\u6807", null, ChangeIconSelected).Enabled = canChangeIcon;
        menu.Items.Add("\u6062\u590d\u9ed8\u8ba4\u56fe\u6807", null, ResetIconSelected).Enabled = canChangeIcon && !string.IsNullOrWhiteSpace(selected?.IconPath);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("\u4e00\u952e\u5957\u5706\u89d2\u56fe\u6807", null, delegate { ApplyIconStyleToAll(DockIconStyles.RoundedRect); }).Enabled = _config.Items.Any(item => !item.IsSeparator);
        menu.Items.Add("\u6062\u590d\u539f\u59cb\u6837\u5f0f", null, delegate { ApplyIconStyleToAll(DockIconStyles.Default); }).Enabled = _config.Items.Any(item => !item.IsSeparator);
        ShowContextMenu(sender, menu);
    }

    private void AddSeparator(object? sender, EventArgs e)
    {
        _config.Items.Add(new DockItem { Type = DockItemTypes.Separator, Name = "\u5206\u9694\u7b26" });
        RefreshItems();
    }

    private void AddPathToDraft(string path)
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
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "TidyDock", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void MoveSelected(int delta)
    {
        var index = SelectedDockIndex();
        var newIndex = index + delta;
        if (index < 0 || newIndex < 0 || newIndex >= _config.Items.Count)
        {
            return;
        }
        var item = _config.Items[index];
        _config.Items.RemoveAt(index);
        _config.Items.Insert(newIndex, item);
        RefreshItems();
        SelectItemIndex(newIndex);
    }

    private void RenameSelected(object? sender, EventArgs e)
    {
        var item = SelectedDockItem();
        if (item is null)
        {
            return;
        }
        using var dialog = new InputDialog("\u91cd\u547d\u540d", "\u540d\u79f0", item.Name, _palette);
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            item.Name = dialog.Value;
            RefreshItems();
        }
    }

    private void RemoveSelected(object? sender, EventArgs e)
    {
        var index = SelectedDockIndex();
        if (index < 0)
        {
            return;
        }
        _config.Items.RemoveAt(index);
        RefreshItems();
        UpdateItemControls();
    }

    private void ChangeIconSelected(object? sender, EventArgs e)
    {
        var item = SelectedDockItem();
        if (item is null || item.IsSeparator)
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
        RefreshItems();
    }

    private void ResetIconSelected(object? sender, EventArgs e)
    {
        var item = SelectedDockItem();
        if (item is null || item.IsSeparator)
        {
            return;
        }

        item.IconPath = "";
        _iconService.ClearMemoryCache();
        RefreshItems();
    }

    private void ManageFolderGroupsSelected(object? sender, EventArgs e)
    {
        var item = SelectedDockItem();
        if (item is null || !string.Equals(item.Type, DockItemTypes.Folder, StringComparison.OrdinalIgnoreCase))
        {
            MessageBox.Show("\u8bf7\u5148\u9009\u4e2d\u4e00\u4e2a\u6587\u4ef6\u5939 Dock \u9879\u3002", "TidyDock", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var dialog = new FolderGroupManagerForm(item, _config.FolderPanel, _palette);
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        item.FolderGroups = dialog.Groups;
        RefreshItems();
    }

    private void ApplyIconStyleToAll(string iconStyle)
    {
        foreach (var item in _config.Items.Where(item => !item.IsSeparator && (_config.Dock.WrapCustomIcons || string.IsNullOrWhiteSpace(item.IconPath))))
        {
            item.IconStyle = iconStyle;
        }

        _iconService.ClearMemoryCache();
        RefreshItems();
    }

    private void OnItemsMouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left)
        {
            return;
        }

        var hit = _items.HitTest(e.Location);
        if (hit.Item?.Tag is not DockItem item || hit.SubItem is null)
        {
            return;
        }

        var columnIndex = hit.Item.SubItems.IndexOf(hit.SubItem);
        if (columnIndex == 3)
        {
            ToggleIconStyle(item);
            return;
        }

        if (columnIndex == 4)
        {
            ToggleOpenBehavior(item);
        }
    }

    private void ToggleIconStyle(DockItem item)
    {
        if (item.IsSeparator)
        {
            return;
        }

        item.IconStyle = string.Equals(item.IconStyle, DockIconStyles.RoundedRect, StringComparison.OrdinalIgnoreCase)
            ? DockIconStyles.Default
            : DockIconStyles.RoundedRect;
        _iconService.ClearMemoryCache();
        RefreshItems();
    }

    private void ToggleOpenBehavior(DockItem item)
    {
        if (!string.Equals(item.Type, DockItemTypes.Folder, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        item.OpenBehavior = string.Equals(item.OpenBehavior, DockOpenBehaviors.Explorer, StringComparison.OrdinalIgnoreCase)
            ? DockOpenBehaviors.Flyout
            : DockOpenBehaviors.Explorer;
        RefreshItems();
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

        var target = Math.Max(0, scrollY);
        if (_scrollRoot.VerticalScroll.Visible)
        {
            var maxValue = Math.Max(_scrollRoot.VerticalScroll.Minimum, _scrollRoot.VerticalScroll.Maximum - _scrollRoot.VerticalScroll.LargeChange + 1);
            target = Math.Clamp(target, _scrollRoot.VerticalScroll.Minimum, maxValue);
            _scrollRoot.VerticalScroll.Value = target;
        }

        _scrollRoot.AutoScrollPosition = new Point(0, target);
        _scrollRoot.PerformLayout();
    }

    private void RestoreScrollLater(int scrollY)
    {
        if (IsDisposed || !IsHandleCreated)
        {
            return;
        }

        BeginInvoke(new Action(() =>
        {
            RestoreScroll(scrollY);
            if (!IsDisposed && IsHandleCreated)
            {
                BeginInvoke(new Action(() => RestoreScroll(scrollY)));
            }
        }));
    }

    private void UpdateItemControls()
    {
        var item = SelectedDockItem();
        var hasSelection = item is not null;
        var isFolder = item is not null && string.Equals(item.Type, DockItemTypes.Folder, StringComparison.OrdinalIgnoreCase);

        if (_renameButton is not null)
        {
            _renameButton.Enabled = hasSelection;
        }

        if (_manageGroupsButton is not null)
        {
            _manageGroupsButton.Enabled = isFolder;
        }

        if (_removeButton is not null)
        {
            _removeButton.Enabled = hasSelection;
        }

        if (_moreItemButton is not null)
        {
            _moreItemButton.Enabled = hasSelection;
        }
    }

    private void ApplyTheme()
    {
        BackColor = _palette.Window;
        ForeColor = _palette.Text;
    }

    private void ApplyNativeScrollTheme()
    {
        if (_scrollRoot is null || _scrollRoot.IsDisposed)
        {
            return;
        }

        _ = DwmBackdrop.TryApplyControlTheme(_scrollRoot.Handle, _palette.IsDark);
        _scrollRoot.Invalidate();
    }

    private void ApplyListTheme()
    {
        _items.BackColor = _palette.PanelAlt;
        _items.ForeColor = _palette.Text;
        _items.BorderStyle = BorderStyle.None;
    }

    private void DrawItemTableHeader(object? sender, DrawListViewColumnHeaderEventArgs e)
    {
        using var background = new SolidBrush(_palette.Panel);
        using var border = new Pen(_palette.Border);
        using var text = new SolidBrush(_palette.MutedText);
        e.Graphics.FillRectangle(background, e.Bounds);
        e.Graphics.DrawLine(border, e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);
        using var font = new Font(Font, FontStyle.Bold);
        using var format = new StringFormat { LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter };
        e.Graphics.DrawString(e.Header?.Text ?? "", font, text, new Rectangle(e.Bounds.Left + 8, e.Bounds.Top, e.Bounds.Width - 12, e.Bounds.Height), format);
    }

    private void DrawItemTableCell(object? sender, DrawListViewSubItemEventArgs e)
    {
        if (e.Item?.Tag is not DockItem item || e.SubItem is null)
        {
            return;
        }

        var selected = e.Item.Selected;
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var background = new SolidBrush(selected ? _palette.Accent : (e.ItemIndex % 2 == 0 ? _palette.PanelAlt : _palette.Panel));
        var broken = IsBrokenItem(item);
        var warningColor = _palette.IsDark ? Color.FromArgb(248, 180, 96) : Color.FromArgb(190, 64, 48);
        var textColor = selected
            ? _palette.AccentText
            : broken && e.ColumnIndex == 2
                ? warningColor
                : e.ColumnIndex is 2 or 3 or 4 ? _palette.MutedText : _palette.Text;
        using var foreground = new SolidBrush(textColor);
        e.Graphics.FillRectangle(background, e.Bounds);

        if (e.ColumnIndex == 0)
        {
            var pill = new Rectangle(e.Bounds.Left + 7, e.Bounds.Top + 7, Math.Min(58, e.Bounds.Width - 14), e.Bounds.Height - 14);
            using var pillPath = CardPanel.RoundedRect(pill, 9);
            using var pillBrush = new SolidBrush(selected ? Color.FromArgb(60, _palette.AccentText) : Color.FromArgb(_palette.IsDark ? 44 : 32, _palette.Accent));
            e.Graphics.FillPath(pillBrush, pillPath);
            using var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter };
            e.Graphics.DrawString(GetTypeLabel(item), e.SubItem.Font ?? Font, foreground, pill, format);
            return;
        }

        if (e.ColumnIndex == 3)
        {
            if (item.IsSeparator)
            {
                DrawCenteredText(e.Graphics, "-", e.Bounds, e.SubItem.Font ?? Font, foreground);
                return;
            }

            var rounded = string.Equals(item.IconStyle, DockIconStyles.RoundedRect, StringComparison.OrdinalIgnoreCase);
            DrawInlineSwitch(e.Graphics, e.Bounds, rounded, rounded ? "\u5706\u89d2" : "\u539f\u59cb", enabled: true, selected);
            return;
        }

        if (e.ColumnIndex == 4)
        {
            if (!string.Equals(item.Type, DockItemTypes.Folder, StringComparison.OrdinalIgnoreCase))
            {
                DrawCenteredText(e.Graphics, "-", e.Bounds, e.SubItem.Font ?? Font, foreground);
                return;
            }

            var flyout = !string.Equals(item.OpenBehavior, DockOpenBehaviors.Explorer, StringComparison.OrdinalIgnoreCase);
            DrawInlineSwitch(e.Graphics, e.Bounds, flyout, flyout ? "\u60ac\u6d6e" : "\u76f4\u5f00", enabled: true, selected);
            return;
        }

        using var textFormat = new StringFormat { LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter };
        e.Graphics.DrawString(e.SubItem.Text, e.SubItem.Font ?? Font, foreground, new Rectangle(e.Bounds.Left + 8, e.Bounds.Top, e.Bounds.Width - 12, e.Bounds.Height), textFormat);
    }

    private void DrawInlineSwitch(Graphics graphics, Rectangle bounds, bool enabledState, string text, bool enabled, bool selected)
    {
        var switchBounds = new Rectangle(bounds.Left + 10, bounds.Top + 7, Math.Min(86, bounds.Width - 20), bounds.Height - 14);
        using var path = CardPanel.RoundedRect(switchBounds, 10);
        var fillColor = enabledState
            ? (selected ? Color.FromArgb(210, _palette.AccentText) : _palette.Accent)
            : (selected ? Color.FromArgb(70, _palette.AccentText) : _palette.PanelAlt);
        using var fill = new SolidBrush(enabled ? fillColor : Color.FromArgb(80, _palette.PanelAlt));
        using var border = new Pen(enabledState ? _palette.Accent : _palette.Border);
        graphics.FillPath(fill, path);
        graphics.DrawPath(border, path);

        using var font = new Font(Font.FontFamily, 8F, FontStyle.Bold, GraphicsUnit.Point);
        var textColor = enabledState
            ? (selected ? _palette.Accent : _palette.AccentText)
            : (selected ? _palette.AccentText : _palette.MutedText);
        using var foreground = new SolidBrush(textColor);
        using var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter };
        graphics.DrawString(text, font, foreground, switchBounds, format);
    }

    private static void DrawCenteredText(Graphics graphics, string text, Rectangle bounds, Font font, Brush brush)
    {
        using var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter };
        graphics.DrawString(text, font, brush, bounds, format);
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

    private ListViewItem CreateDockItemRow(DockItem item)
    {
        var row = new ListViewItem(GetTypeLabel(item)) { Tag = item, ToolTipText = GetTargetTooltip(item) };
        row.SubItems.Add(string.IsNullOrWhiteSpace(item.Name) ? "\u672a\u547d\u540d" : item.Name);
        row.SubItems.Add(GetTargetDisplay(item));
        row.SubItems.Add(GetIconStyleLabel(item));
        row.SubItems.Add(GetOpenBehaviorLabel(item));
        return row;
    }

    private DockItem? SelectedDockItem()
    {
        return _items.SelectedItems.Count > 0 ? _items.SelectedItems[0].Tag as DockItem : null;
    }

    private int SelectedDockIndex()
    {
        return _items.SelectedItems.Count > 0 ? _items.SelectedItems[0].Index : -1;
    }

    private void SelectItemIndex(int index)
    {
        if (index < 0 || index >= _items.Items.Count)
        {
            return;
        }

        _items.Items[index].Selected = true;
        _items.Items[index].Focused = true;
        _items.EnsureVisible(index);
    }

    private static string FormatSliderValue(int value, string suffix)
    {
        return string.IsNullOrWhiteSpace(suffix) ? value.ToString() : $"{value} {suffix}";
    }

    private static string FormatDuration(TimeSpan duration)
    {
        return duration.TotalHours >= 1
            ? $"{(int)duration.TotalHours} 小时 {duration.Minutes} 分钟"
            : $"{duration.Minutes} 分钟 {duration.Seconds} 秒";
    }

    private static ComboOption[] GetDisplayOptions()
    {
        var options = new List<ComboOption> { new("primary", "\u4e3b\u5c4f\u5e55") };
        var index = 1;
        foreach (var screen in Screen.AllScreens)
        {
            var primary = screen.Primary ? "\uff0c\u4e3b\u5c4f" : "";
            var label = $"\u663e\u793a\u5668 {index}\uff08{screen.Bounds.Width}x{screen.Bounds.Height}{primary}\uff09";
            options.Add(new ComboOption(screen.DeviceName, label));
            index++;
        }

        return options
            .GroupBy(option => option.Value, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToArray();
    }

    private static string GetTypeLabel(DockItem item)
    {
        if (item.IsSeparator)
        {
            return "\u5206\u9694";
        }

        return item.Type switch
        {
            DockItemTypes.App => "\u5e94\u7528",
            DockItemTypes.File => "\u6587\u4ef6",
            DockItemTypes.Folder => "\u6587\u4ef6\u5939",
            DockItemTypes.Url => "\u7f51\u5740",
            DockItemTypes.Shell => "\u7cfb\u7edf\u4f4d\u7f6e",
            DockItemTypes.Settings => "\u8bbe\u7f6e",
            _ => "\u9879\u76ee"
        };
    }

    private static string GetTargetDisplay(DockItem item)
    {
        if (item.IsSeparator)
        {
            return "\u89c6\u89c9\u5206\u7ec4";
        }

        if (string.IsNullOrWhiteSpace(item.Target))
        {
            return "-";
        }

        if (string.Equals(item.Target, ThisPcTarget, StringComparison.OrdinalIgnoreCase))
        {
            return "\u6b64\u7535\u8111";
        }

        if (string.Equals(item.Type, DockItemTypes.Url, StringComparison.OrdinalIgnoreCase)
            && Uri.TryCreate(item.Target, UriKind.Absolute, out var uri)
            && !string.IsNullOrWhiteSpace(uri.Host))
        {
            return uri.Host;
        }

        if (item.Target.StartsWith("shell:", StringComparison.OrdinalIgnoreCase)
            || item.Target.StartsWith("::", StringComparison.OrdinalIgnoreCase))
        {
            return item.Target;
        }

        var shortcutDisplay = GetShortcutDisplay(item);
        if (!string.IsNullOrWhiteSpace(shortcutDisplay))
        {
            return shortcutDisplay;
        }

        return ShortPathLabel(item.Target);
    }

    private static string GetTargetTooltip(DockItem item)
    {
        if (item.IsSeparator)
        {
            return "\u89c6\u89c9\u5206\u7ec4";
        }

        if (string.IsNullOrWhiteSpace(item.Target))
        {
            return "-";
        }

        var resolved = GetShortcutResolvedTarget(item.Target);
        var targetText = string.IsNullOrWhiteSpace(resolved)
            ? item.Target
            : $"快捷方式：{item.Target}{Environment.NewLine}指向：{resolved}";

        return IsBrokenItem(item)
            ? "\u76ee\u6807\u4e0d\u5b58\u5728\uff1a" + targetText
            : targetText;
    }

    private static string GetShortcutDisplay(DockItem item)
    {
        if (!ShortcutService.IsShortcutFile(item.Target))
        {
            return "";
        }

        var resolved = GetShortcutResolvedTarget(item.Target);
        if (!string.IsNullOrWhiteSpace(resolved))
        {
            return ShortPathLabel(resolved);
        }

        return string.IsNullOrWhiteSpace(item.Name)
            ? ShortPathLabel(item.Target)
            : $"{item.Name}\uff08\u5feb\u6377\u65b9\u5f0f\uff09";
    }

    private static string GetShortcutResolvedTarget(string target)
    {
        if (!ShortcutService.IsShortcutFile(target)
            || !string.Equals(Path.GetExtension(target), ".lnk", StringComparison.OrdinalIgnoreCase))
        {
            return "";
        }

        return ShortcutService.ResolveShortcutTarget(target);
    }

    private static string ShortPathLabel(string target)
    {
        try
        {
            var trimmed = target.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var name = Path.GetFileName(trimmed);
            if (!string.IsNullOrWhiteSpace(name))
            {
                return name;
            }
        }
        catch
        {
        }

        return target.Length > 48 ? "..." + target[^45..] : target;
    }

    private static bool IsBrokenItem(DockItem item)
    {
        if (item.IsSeparator
            || string.Equals(item.Type, DockItemTypes.Settings, StringComparison.OrdinalIgnoreCase)
            || string.Equals(item.Type, DockItemTypes.Url, StringComparison.OrdinalIgnoreCase)
            || string.Equals(item.Type, DockItemTypes.Shell, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return string.Equals(item.Type, DockItemTypes.Folder, StringComparison.OrdinalIgnoreCase)
            ? !Directory.Exists(item.Target)
            : !File.Exists(item.Target) && !Directory.Exists(item.Target);
    }

    private static string GetIconStyleLabel(DockItem item)
    {
        if (item.IsSeparator)
        {
            return "-";
        }

        if (!string.IsNullOrWhiteSpace(item.IconPath))
        {
            return "\u81ea\u5b9a\u4e49";
        }

        return string.Equals(item.IconStyle, DockIconStyles.RoundedRect, StringComparison.OrdinalIgnoreCase)
            ? "\u5706\u89d2"
            : "\u539f\u59cb";
    }

    private static string GetOpenBehaviorLabel(DockItem item)
    {
        if (!string.Equals(item.Type, DockItemTypes.Folder, StringComparison.OrdinalIgnoreCase))
        {
            return "-";
        }

        return string.Equals(item.OpenBehavior, DockOpenBehaviors.Explorer, StringComparison.OrdinalIgnoreCase)
            ? "\u8d44\u6e90\u7ba1\u7406\u5668"
            : "\u60ac\u6d6e\u9762\u677f";
    }

    private sealed record ComboOption(string Value, string Label)
    {
        public override string ToString()
        {
            return Label;
        }
    }
}
