namespace TidyDock.WinForms.Shell;

internal sealed class HotZoneForm : Form
{
    public HotZoneForm()
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        TopMost = true;
        StartPosition = FormStartPosition.Manual;
        BackColor = Color.Magenta;
        TransparencyKey = Color.Magenta;
        Opacity = 0.01;
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
}
