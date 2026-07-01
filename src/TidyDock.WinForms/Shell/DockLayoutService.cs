using TidyDock.WinForms.Core;

namespace TidyDock.WinForms.Shell;

internal sealed class DockLayoutService
{
    public Screen GetScreen(DockSettings settings)
    {
        if (!string.IsNullOrWhiteSpace(settings.Display) && settings.Display != "primary")
        {
            foreach (var screen in Screen.AllScreens)
            {
                if (string.Equals(screen.DeviceName, settings.Display, StringComparison.OrdinalIgnoreCase))
                {
                    return screen;
                }
            }
        }

        return Screen.PrimaryScreen ?? Screen.AllScreens[0];
    }

    public Rectangle GetDockBounds(DockSettings settings, Size dockSize)
    {
        var work = GetScreen(settings).WorkingArea;
        const int margin = 18;
        return settings.Position switch
        {
            "top" => new Rectangle(work.Left + (work.Width - dockSize.Width) / 2, work.Top + margin, dockSize.Width, dockSize.Height),
            "left" => new Rectangle(work.Left + margin, work.Top + (work.Height - dockSize.Height) / 2, dockSize.Width, dockSize.Height),
            "right" => new Rectangle(work.Right - dockSize.Width - margin, work.Top + (work.Height - dockSize.Height) / 2, dockSize.Width, dockSize.Height),
            _ => new Rectangle(work.Left + (work.Width - dockSize.Width) / 2, work.Bottom - dockSize.Height - margin, dockSize.Width, dockSize.Height)
        };
    }

    public Rectangle GetHotZoneBounds(DockSettings settings)
    {
        var work = GetScreen(settings).WorkingArea;
        return settings.Position switch
        {
            "top" => new Rectangle(work.Left, work.Top, work.Width, 4),
            "left" => new Rectangle(work.Left, work.Top, 4, work.Height),
            "right" => new Rectangle(work.Right - 4, work.Top, 4, work.Height),
            _ => new Rectangle(work.Left, work.Bottom - 4, work.Width, 4)
        };
    }
}
