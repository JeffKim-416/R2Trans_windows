using System.Windows;
using System.Windows.Forms;
using R2Trans.Windows.Localization;

namespace R2Trans.Windows.Services;

public sealed class AppController : IDisposable
{
    private readonly NotifyIcon notifyIcon = new();
    private readonly GlobalHotKeyService hotKeyService = new();
    private MainWindow? mainWindow;
    private LiveInterpreterWindow? liveInterpreterWindow;
    private bool disposed;

    public AppController()
    {
        SettingsStore = new SettingsStore();
        CredentialStore = new SecureCredentialStore();
        Translator = new OpenAITranslator(SettingsStore, CredentialStore);
        ClipboardTranslator = new ClipboardTranslator(SettingsStore, Translator);
        notifyIcon.DoubleClick += NotifyIcon_DoubleClick;
    }

    public SettingsStore SettingsStore { get; }
    public SecureCredentialStore CredentialStore { get; }
    public OpenAITranslator Translator { get; }
    public ClipboardTranslator ClipboardTranslator { get; }
    public bool IsShuttingDown { get; private set; }

    public void Start()
    {
        SettingsStore.Load();
        AppText.Language = SettingsStore.Current.AppLanguage;

        mainWindow = new MainWindow(this);
        mainWindow.SettingsSaved += (_, _) => ApplySettings();
        mainWindow.LiveInterpreterRequested += (_, _) => OpenLiveInterpreter();
        mainWindow.SourceInitialized += (_, _) => ApplySettings();

        ConfigureTray();

        mainWindow.Show();
        if (Environment.GetCommandLineArgs().Any(arg => arg.Equals("--minimized", StringComparison.OrdinalIgnoreCase)))
        {
            mainWindow.Hide();
        }
    }

    public void ApplySettings()
    {
        if (mainWindow is null)
        {
            return;
        }

        AppText.Language = SettingsStore.Current.AppLanguage;
        mainWindow.ReloadLocalizedText();
        mainWindow.ReloadValues();
        ConfigureTray();
        try
        {
            RegisterHotKey();
        }
        catch (Exception exception)
        {
            ShowError(exception.Message);
        }
    }

    private void RegisterHotKey()
    {
        if (mainWindow is null)
        {
            return;
        }

        hotKeyService.Register(mainWindow, SettingsStore.Current.HotKeyString, async () =>
        {
            try
            {
                await ClipboardTranslator.TranslateSelectionAsync(mainWindow, mainWindow.SetStatus);
            }
            catch (Exception exception)
            {
                ShowError(exception.Message);
            }
        });
    }

    private void ConfigureTray()
    {
        notifyIcon.Text = "R2Trans";
        notifyIcon.Icon = System.Drawing.SystemIcons.Application;
        notifyIcon.Visible = SettingsStore.Current.ShowTrayIcon;
        notifyIcon.ContextMenuStrip = new ContextMenuStrip();
        notifyIcon.ContextMenuStrip.Items.Add(AppText.Text(TextKey.OpenSettings), null, (_, _) => mainWindow?.ShowAndActivate());
        notifyIcon.ContextMenuStrip.Items.Add(AppText.Text(TextKey.LiveInterpreter), null, (_, _) => OpenLiveInterpreter());
        notifyIcon.ContextMenuStrip.Items.Add("-");
        notifyIcon.ContextMenuStrip.Items.Add(AppText.Text(TextKey.QuitR2Trans), null, (_, _) => Shutdown());
    }

    private void NotifyIcon_DoubleClick(object? sender, EventArgs e)
    {
        mainWindow?.ShowAndActivate();
    }

    private void OpenLiveInterpreter()
    {
        if (liveInterpreterWindow is null || !liveInterpreterWindow.IsLoaded)
        {
            liveInterpreterWindow = new LiveInterpreterWindow(this)
            {
                Owner = mainWindow
            };
        }

        liveInterpreterWindow.Show();
        liveInterpreterWindow.Activate();
    }

    private static void ShowError(string message)
    {
        System.Windows.MessageBox.Show(
            message,
            "R2Trans",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
    }

    public void Shutdown()
    {
        IsShuttingDown = true;
        Dispose();
        System.Windows.Application.Current.Shutdown();
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        hotKeyService.Dispose();
        notifyIcon.Visible = false;
        notifyIcon.Dispose();
    }
}
