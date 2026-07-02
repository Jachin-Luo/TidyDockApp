using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

[assembly: AssemblyTitle("TidyDock WinForms \u5b89\u88c5\u5668")]
[assembly: AssemblyDescription("TidyDock WinForms \u5f53\u524d\u7528\u6237\u5b89\u88c5\u5668")]
[assembly: AssemblyCompany("TidyDock")]
[assembly: AssemblyProduct("TidyDock")]
[assembly: AssemblyVersion("0.2.0.0")]
[assembly: AssemblyFileVersion("0.2.0.0")]

namespace TidyDockWinFormsSetup
{
    internal static class Program
    {
        private const string ProductName = "TidyDock";

        [STAThread]
        private static int Main(string[] args)
        {
            try
            {
                var target = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Programs",
                    "TidyDock.WinForms");
                Directory.CreateDirectory(target);

                WriteResource("payload.TidyDock.exe", Path.Combine(target, "TidyDock.exe"));
                WriteResource("payload.TidyDock.ico", Path.Combine(target, "TidyDock.ico"));
                WriteResource("payload.README.txt", Path.Combine(target, "README.txt"));
                WriteResource("payload.Uninstall-CurrentUser.ps1", Path.Combine(target, "Uninstall-CurrentUser.ps1"));
                CreateStartMenuShortcuts(target);

                var result = MessageBox.Show(
                    ProductName + " WinForms \u5df2\u4e3a\u5f53\u524d\u7528\u6237\u5b89\u88c5\u5b8c\u6210\u3002\n\n\u73b0\u5728\u542f\u52a8\u5417\uff1f",
                    ProductName,
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Information);
                if (result == DialogResult.Yes)
                {
                    Process.Start(new ProcessStartInfo(Path.Combine(target, "TidyDock.exe")) { UseShellExecute = true });
                }

                return 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return 1;
            }
        }

        private static void WriteResource(string name, string path)
        {
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name))
            {
                if (stream == null)
                {
                    throw new InvalidOperationException("\u5b89\u88c5\u5305\u7f3a\u5c11 payload: " + name);
                }
                using (var file = File.Create(path))
                {
                    stream.CopyTo(file);
                }
            }
        }

        private static void CreateStartMenuShortcuts(string target)
        {
            var startMenu = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Microsoft",
                "Windows",
                "Start Menu",
                "Programs",
                ProductName);
            Directory.CreateDirectory(startMenu);

            var exe = Path.Combine(target, "TidyDock.exe");
            var icon = Path.Combine(target, "TidyDock.ico");
            CreateShortcut(Path.Combine(startMenu, "TidyDock.lnk"), exe, "", target, icon);
            CreateShortcut(
                Path.Combine(startMenu, "\u5378\u8f7d TidyDock.lnk"),
                "powershell.exe",
                "-ExecutionPolicy Bypass -File \"" + Path.Combine(target, "Uninstall-CurrentUser.ps1") + "\"",
                target,
                icon);
        }

        private static void CreateShortcut(string shortcutPath, string targetPath, string arguments, string workingDirectory, string iconPath)
        {
            var shellType = Type.GetTypeFromProgID("WScript.Shell");
            if (shellType == null)
            {
                throw new InvalidOperationException("WScript.Shell is not available.");
            }

            var shell = Activator.CreateInstance(shellType);
            var shortcut = shellType.InvokeMember("CreateShortcut", BindingFlags.InvokeMethod, null, shell, new object[] { shortcutPath });
            if (shortcut == null)
            {
                throw new InvalidOperationException("Cannot create shortcut: " + shortcutPath);
            }

            var shortcutType = shortcut.GetType();
            shortcutType.InvokeMember("TargetPath", BindingFlags.SetProperty, null, shortcut, new object[] { targetPath });
            shortcutType.InvokeMember("Arguments", BindingFlags.SetProperty, null, shortcut, new object[] { arguments });
            shortcutType.InvokeMember("WorkingDirectory", BindingFlags.SetProperty, null, shortcut, new object[] { workingDirectory });
            shortcutType.InvokeMember("IconLocation", BindingFlags.SetProperty, null, shortcut, new object[] { iconPath });
            shortcutType.InvokeMember("Save", BindingFlags.InvokeMethod, null, shortcut, null);
        }
    }
}
