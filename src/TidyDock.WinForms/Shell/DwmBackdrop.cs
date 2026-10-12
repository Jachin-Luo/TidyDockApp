using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace TidyDock.WinForms.Shell;

internal static class DwmBackdrop
{
    private const int DwmwaUseImmersiveDarkMode = 20;
    private const int DwmwaSystemBackdropType = 38;
    private const int DwmSbtTransientWindow = 3;

    public static bool TransparencyEnabled
    {
        get
        {
            if (SystemInformation.HighContrast || !OperatingSystem.IsWindowsVersionAtLeast(10, 0, 17134))
            {
                return false;
            }
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                return key?.GetValue("EnableTransparency") is not int value || value != 0;
            }
            catch
            {
                return false;
            }
        }
    }

    public static bool TryApplyFrost(IntPtr handle, Color tint)
    {
        if (handle == IntPtr.Zero)
        {
            return false;
        }

        // AccentPolicy is an optional Windows compositor extension, not an iOS material API.
        // Keep the opaque fallback when the extension is unavailable.
        var enabled = TransparencyEnabled;
        var policy = new AccentPolicy
        {
            State = enabled ? 3 : 0,
            // Keep native tint almost clear: Acrylic tints outside custom HRGN corners on some builds.
            // The layered foreground paints the actual tint with an antialiased rounded mask.
            GradientColor = PackAccentColor(Color.FromArgb(1, tint))
        };
        var memory = Marshal.AllocHGlobal(Marshal.SizeOf<AccentPolicy>());
        try
        {
            Marshal.StructureToPtr(policy, memory, false);
            var data = new CompositionAttributeData
            {
                Attribute = 19,
                Data = memory,
                Size = (nuint)Marshal.SizeOf<AccentPolicy>()
            };
            var applied = SetWindowCompositionAttribute(handle, ref data);
            return enabled && applied;
        }
        catch (Exception ex) when (ex is EntryPointNotFoundException or DllNotFoundException)
        {
            return false;
        }
        finally
        {
            Marshal.FreeHGlobal(memory);
        }
    }

    internal static uint PackAccentColor(Color color) =>
        ((uint)color.A << 24) | ((uint)color.B << 16) | ((uint)color.G << 8) | color.R;

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

    public static bool TryApplyControlTheme(IntPtr handle, bool darkMode)
    {
        if (handle == IntPtr.Zero)
        {
            return false;
        }

        try
        {
            return SetWindowTheme(handle, darkMode ? "DarkMode_Explorer" : "Explorer", null) == 0;
        }
        catch
        {
            return false;
        }
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int dwAttribute, ref int pvAttribute, int cbAttribute);

    [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
    private static extern int SetWindowTheme(IntPtr hwnd, string? pszSubAppName, string? pszSubIdList);

    [StructLayout(LayoutKind.Sequential)]
    private struct AccentPolicy
    {
        public int State;
        public int Flags;
        public uint GradientColor;
        public int AnimationId;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct CompositionAttributeData
    {
        public int Attribute;
        public IntPtr Data;
        public nuint Size;
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowCompositionAttribute(IntPtr hwnd, ref CompositionAttributeData data);

}
