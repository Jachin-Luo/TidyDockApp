namespace TidyDock.WinForms.UI;

internal sealed class InputDialog : Form
{
    private readonly TextBox _input = new();

    public InputDialog(string title, string label, string value, ThemePalette palette)
    {
        Text = title;
        Width = 400;
        Height = 170;
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = palette.Window;
        ForeColor = palette.Text;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(14),
            RowCount = 3,
            ColumnCount = 1
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        Controls.Add(root);

        root.Controls.Add(new Label { Text = label, AutoSize = true, ForeColor = palette.MutedText, Margin = new Padding(0, 0, 0, 8) }, 0, 0);
        _input.Text = value ?? "";
        _input.Dock = DockStyle.Top;
        _input.BackColor = palette.PanelAlt;
        _input.ForeColor = palette.Text;
        _input.BorderStyle = BorderStyle.FixedSingle;
        root.Controls.Add(_input, 0, 1);

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Bottom, FlowDirection = FlowDirection.RightToLeft, Height = 38 };
        root.Controls.Add(buttons, 0, 2);
        buttons.Controls.Add(MakeButton("确定", palette, true, delegate { DialogResult = DialogResult.OK; }));
        buttons.Controls.Add(MakeButton("取消", palette, false, delegate { DialogResult = DialogResult.Cancel; }));

        Shown += delegate
        {
            _input.Focus();
            _input.SelectAll();
        };
    }

    public string Value => _input.Text;

    private static GlassButton MakeButton(string text, ThemePalette palette, bool primary, EventHandler click)
    {
        var button = new GlassButton
        {
            Text = text,
            Width = 82,
            Height = 30,
            Primary = primary,
            Palette = palette,
            Margin = new Padding(6, 6, 0, 0)
        };
        button.Click += click;
        return button;
    }
}
