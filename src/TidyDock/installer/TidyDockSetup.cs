using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

[assembly: AssemblyTitle("TidyDock Setup")]
[assembly: AssemblyDescription("Current-user installer for TidyDock")]
[assembly: AssemblyCompany("TidyDock")]
[assembly: AssemblyProduct("TidyDock")]
[assembly: AssemblyCopyright("Copyright 2026")]
[assembly: AssemblyVersion("0.1.1.0")]
[assembly: AssemblyFileVersion("0.1.1.0")]

namespace TidyDockSetup
{
    internal static class Program
    {
        private const string ProductName = "TidyDock";
        private const string Version = "0.1.1";

        [STAThread]
        private static int Main(string[] args)
        {
            var silent = HasArg(args, "/silent") || HasArg(args, "-silent");
            var launch = HasArg(args, "/launch") || HasArg(args, "-launch");
            var startWithWindows = HasArg(args, "/startwithwindows") || HasArg(args, "-startwithwindows");

            try
            {
                Application.EnableVisualStyles();

                if (!silent)
                {
                    var confirm = MessageBox.Show(
                        "Install TidyDock for the current Windows user?\n\nNo administrator permission is required.",
                        "TidyDock Setup",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);
                    if (confirm != DialogResult.Yes)
                    {
                        return 0;
                    }
                }

                var installRoot = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Programs",
                    "TidyDock");
                var startMenuDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "Microsoft",
                    "Windows",
                    "Start Menu",
                    "Programs",
                    "TidyDock");

                var installExe = Path.Combine(installRoot, "TidyDock.exe");
                var installIcon = Path.Combine(installRoot, "TidyDock.ico");
                var readmePath = Path.Combine(installRoot, "README.txt");
                var uninstallScript = Path.Combine(installRoot, "Uninstall-CurrentUser.ps1");

                StopRunningTidyDock();

                Directory.CreateDirectory(installRoot);
                Directory.CreateDirectory(startMenuDir);

                ExtractResource("payload.TidyDock.exe", installExe, false);
                ExtractResource("payload.TidyDock.ico", installIcon, true);
                ExtractResource("payload.README.txt", readmePath, false);
                ExtractResource("payload.Uninstall-CurrentUser.ps1", uninstallScript, false);

                CreateShortcut(
                    Path.Combine(startMenuDir, "TidyDock.lnk"),
                    installExe,
                    string.Empty,
                    installRoot,
                    "TidyDock");
                CreateShortcut(
                    Path.Combine(startMenuDir, "Uninstall TidyDock.lnk"),
                    "powershell.exe",
                    "-ExecutionPolicy Bypass -File \"" + uninstallScript + "\"",
                    installRoot,
                    "Uninstall TidyDock");

                SetStartWithWindows(startWithWindows, installExe);
                RegisterUninstallEntry(installRoot, installExe, uninstallScript);

                if (!silent)
                {
                    var result = MessageBox.Show(
                        "TidyDock has been installed.\n\nLaunch it now?",
                        "TidyDock Setup",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Information);
                    launch = result == DialogResult.Yes;
                }

                if (launch)
                {
                    Process.Start(new ProcessStartInfo(installExe) { WorkingDirectory = installRoot });
                }

                return 0;
            }
            catch (Exception ex)
            {
                if (!silent)
                {
                    MessageBox.Show(
                        "TidyDock setup failed.\n\n" + ex.Message,
                        "TidyDock Setup",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }

                if (silent)
                {
                    try
                    {
                        File.WriteAllText(
                            Path.Combine(Path.GetTempPath(), "TidyDockSetup-error.log"),
                            ex.ToString());
                    }
                    catch
                    {
                    }
                }

                Console.Error.WriteLine(ex.ToString());
                return 1;
            }
        }

        private static bool HasArg(string[] args, string value)
        {
            foreach (var arg in args)
            {
                if (string.Equals(arg, value, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static void StopRunningTidyDock()
        {
            foreach (var process in Process.GetProcessesByName("TidyDock"))
            {
                try
                {
                    process.Kill();
                    process.WaitForExit(3000);
                }
                catch
                {
                }
            }
        }

        private static void ExtractResource(string resourceName, string targetPath, bool keepExistingIfLocked)
        {
            var assembly = Assembly.GetExecutingAssembly();
            using (var input = assembly.GetManifestResourceStream(resourceName))
            {
                if (input == null)
                {
                    throw new InvalidOperationException("Missing setup payload: " + resourceName);
                }

                var tempPath = targetPath + ".tmp-" + Guid.NewGuid().ToString("N");
                try
                {
                    using (var output = File.Create(tempPath))
                    {
                        input.CopyTo(output);
                    }

                    File.Copy(tempPath, targetPath, true);
                }
                catch (IOException)
                {
                    if (!keepExistingIfLocked || !File.Exists(targetPath))
                    {
                        throw;
                    }
                }
                finally
                {
                    try
                    {
                        if (File.Exists(tempPath))
                        {
                            File.Delete(tempPath);
                        }
                    }
                    catch
                    {
                    }
                }
            }
        }

        private static void CreateShortcut(string shortcutPath, string targetPath, string arguments, string workingDirectory, string description)
        {
            var shellType = Type.GetTypeFromProgID("WScript.Shell");
            if (shellType == null)
            {
                return;
            }

            var shell = Activator.CreateInstance(shellType);
            var shortcut = shellType.InvokeMember(
                "CreateShortcut",
                BindingFlags.InvokeMethod,
                null,
                shell,
                new object[] { shortcutPath });

            SetComProperty(shortcut, "TargetPath", targetPath);
            SetComProperty(shortcut, "Arguments", arguments);
            SetComProperty(shortcut, "WorkingDirectory", workingDirectory);
            SetComProperty(shortcut, "Description", description);
            shortcut.GetType().InvokeMember("Save", BindingFlags.InvokeMethod, null, shortcut, null);
        }

        private static void SetComProperty(object target, string name, object value)
        {
            target.GetType().InvokeMember(name, BindingFlags.SetProperty, null, target, new[] { value });
        }

        private static void SetStartWithWindows(bool enabled, string installExe)
        {
            using (var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run"))
            {
                if (key == null)
                {
                    return;
                }

                if (enabled)
                {
                    key.SetValue(ProductName, "\"" + installExe + "\"");
                }
                else
                {
                    key.DeleteValue(ProductName, false);
                }
            }
        }

        private static void RegisterUninstallEntry(string installRoot, string installExe, string uninstallScript)
        {
            using (var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\TidyDock"))
            {
                if (key == null)
                {
                    return;
                }

                key.SetValue("DisplayName", ProductName);
                key.SetValue("DisplayVersion", Version);
                key.SetValue("Publisher", "TidyDock");
                key.SetValue("InstallLocation", installRoot);
                key.SetValue("DisplayIcon", installExe);
                key.SetValue("UninstallString", "powershell.exe -ExecutionPolicy Bypass -File \"" + uninstallScript + "\"");
                key.SetValue("NoModify", 1, RegistryValueKind.DWord);
                key.SetValue("NoRepair", 1, RegistryValueKind.DWord);
            }
        }
    }
}
