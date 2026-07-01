namespace TidyDock.WinForms;

static class Program
{
    [STAThread]
    static void Main()
    {
        var startup = System.Diagnostics.Stopwatch.StartNew();
        using var mutex = new Mutex(true, "TidyDock.WinForms.SingleInstance", out var created);
        if (!created)
        {
            return;
        }

        ApplicationConfiguration.Initialize();
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        using var app = Bootstrapper.Create();
        app.Log.Info($"Startup completed in {startup.ElapsedMilliseconds} ms.");
        Application.Run(app.DockForm);
    }
}
