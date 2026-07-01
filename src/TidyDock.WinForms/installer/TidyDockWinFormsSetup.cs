using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

[assembly: AssemblyTitle("TidyDock WinForms Setup")]
[assembly: AssemblyDescription("Current-user installer for TidyDock WinForms")]
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

                var result = MessageBox.Show(
                    ProductName + " WinForms has been installed for the current user.\n\nLaunch now?",
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
                    throw new InvalidOperationException("Missing setup payload: " + name);
                }
                using (var file = File.Create(path))
                {
                    stream.CopyTo(file);
                }
            }
        }
    }
}
