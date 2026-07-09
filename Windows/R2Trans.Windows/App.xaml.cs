using System.Windows;
using R2Trans.Windows.Services;
using WpfApplication = System.Windows.Application;

namespace R2Trans.Windows;

public partial class App : WpfApplication
{
    private AppController? controller;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        controller = new AppController();
        controller.Start();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        controller?.Dispose();
        base.OnExit(e);
    }
}
