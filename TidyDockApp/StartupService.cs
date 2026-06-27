using Microsoft.Win32;
using System;
using System.Reflection;

namespace TidyDock
{
    public static class StartupService
    {
        private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string ValueName = "TidyDock";

        public static bool IsEnabled()
        {
            using (var key = Registry.CurrentUser.OpenSubKey(RunKey, false))
            {
                if (key == null)
                {
                    return false;
                }

                var value = key.GetValue(ValueName) as string;
                return !string.IsNullOrEmpty(value);
            }
        }

        public static void SetEnabled(bool enabled)
        {
            using (var key = Registry.CurrentUser.OpenSubKey(RunKey, true))
            {
                if (key == null)
                {
                    return;
                }

                if (enabled)
                {
                    var path = Assembly.GetEntryAssembly().Location;
                    key.SetValue(ValueName, "\"" + path + "\"");
                }
                else
                {
                    try
                    {
                        key.DeleteValue(ValueName, false);
                    }
                    catch
                    {
                    }
                }
            }
        }
    }
}
