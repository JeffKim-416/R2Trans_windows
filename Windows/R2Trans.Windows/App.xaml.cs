using System.Windows;
using R2Trans.Windows.Services;

namespace R2Trans.Windows;

public partial class App : Application
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
