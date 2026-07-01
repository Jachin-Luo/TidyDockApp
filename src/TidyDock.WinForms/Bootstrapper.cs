using TidyDock.WinForms.Services;
using TidyDock.WinForms.Shell;
using TidyDock.WinForms.UI;

namespace TidyDock.WinForms;

internal sealed class AppHost : IDisposable
{
    public AppHost(DockForm dockForm, IconService iconService)
    {
        DockForm = dockForm;
        IconService = iconService;
    }

    public DockForm DockForm { get; }
    public LogService Log { get; init; } = null!;

    private IconService IconService { get; }

    public void Dispose()
    {
        DockForm.Dispose();
        IconService.Dispose();
    }
}

internal static class Bootstrapper
{
    public static AppHost Create()
    {
        var paths = new AppPaths();
        paths.Ensure();

        var log = new LogService(paths);
        var configService = new ConfigService(paths, log);
        var config = configService.Load();
        var iconService = new IconService(paths, log);
        var shortcutService = new ShortcutService(paths);
        var startupService = new StartupService();
        var layoutService = new DockLayoutService();
        var dockForm = new DockForm(config, configService, iconService, shortcutService, startupService, layoutService, log);
        return new AppHost(dockForm, iconService) { Log = log };
    }
}
