using System.Runtime.InteropServices;

namespace TidyDock.WinForms.Shell;

internal static class DwmBackdrop
{
    private const int DwmwaUseImmersiveDarkMode = 20;
    private const int DwmwaSystemBackdropType = 38;
    private const int DwmSbtTransientWindow = 3;

    public static bool TryApply(IntPtr handle, bool darkMode)
    {
        if (handle == IntPtr.Zero)
        {
            return false;
        }

        try
        {
            var dark = darkMode ? 1 : 0;
            _ = DwmSetWindowAttribute(handle, DwmwaUseImmersiveDarkMode, ref dark, sizeof(int));

            var backdrop = DwmSbtTransientWindow;
            return DwmSetWindowAttribute(handle, DwmwaSystemBackdropType, ref backdrop, sizeof(int)) == 0;
        }
        catch
        {
            return false;
        }
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int dwAttribute, ref int pvAttribute, int cbAttribute);
}
