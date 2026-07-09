using System.Windows;
using System.Windows.Forms;
using R2Trans.Windows.Localization;
using R2Trans.Windows.Models;

namespace R2Trans.Windows.Services;

public sealed class AppController : IDisposable
{
    private readonly NotifyIcon notifyIcon = new();
    private readonly GlobalHotKeyService hotKeyService = new();
    private readonly UpdateChecker updateChecker = new();
    private MainWindow? mainWindow;
    private LiveInterpreterWindow? liveInterpreterWindow;
    private TranslationProgressWindow? progressWindow;
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

        _ = CheckForUpdatesOnStartupAsync();
    }

    public void ApplySettings()
    {
        try
        {
            ApplySettingsCore();
        }
        catch (Exception exception)
        {
            ShowError(exception.Message);
        }
    }

    public void ApplySettingsForSave()
    {
        RegisterHotKey();
    }

    private void ApplySettingsCore()
    {
        if (mainWindow is null)
        {
            return;
        }

        AppText.Language = SettingsStore.Current.AppLanguage;
        mainWindow.ReloadLocalizedText();
        mainWindow.ReloadValues();
        ConfigureTray();
        RegisterHotKey();
    }

    public void SuspendHotKey()
    {
        hotKeyService.Unregister();
    }

    public void ResumeHotKey()
    {
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
                await ClipboardTranslator.TranslateSelectionAsync(mainWindow, UpdateTranslationStatus);
            }
            catch (Exception exception)
            {
                HideProgressWindow();
                ShowError(exception.Message);
            }
        });
    }

    private void UpdateTranslationStatus(string status)
    {
        mainWindow?.SetStatus(status);

        if (string.IsNullOrWhiteSpace(status))
        {
            HideProgressWindow();
            return;
        }

        ShowProgressWindow(status);
    }

    private void ShowProgressWindow(string message)
    {
        if (progressWindow is null)
        {
            progressWindow = new TranslationProgressWindow();
            progressWindow.Closed += (_, _) => progressWindow = null;
        }

        progressWindow.SetMessage(message);
        progressWindow.Show();
    }

    private void HideProgressWindow()
    {
        if (progressWindow is null)
        {
            return;
        }

        progressWindow.Close();
        progressWindow = null;
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

    private async Task CheckForUpdatesOnStartupAsync()
    {
        try
        {
            await Task.Delay(1800);
            var updateInfo = await updateChecker.CheckAsync();
            if (updateInfo is null || disposed || IsShuttingDown)
            {
                return;
            }

            var result = ShowUpdatePrompt(updateInfo);
            if (result == MessageBoxResult.Yes)
            {
                UpdateChecker.OpenUpdateUrl(updateInfo);
            }
        }
        catch
        {
            // Update checks should never interrupt app startup.
        }
    }

    private MessageBoxResult ShowUpdatePrompt(UpdateInfo updateInfo)
    {
        var isKorean = SettingsStore.Current.AppLanguage == AppLanguage.Korean;
        var title = isKorean ? "R2Trans 업데이트" : "R2Trans Update";
        var message = isKorean
            ? $"새 버전 {updateInfo.LatestVersion}이 있습니다.\n현재 버전: {updateInfo.CurrentVersion}\n\n다운로드 페이지를 열까요?"
            : $"A new version {updateInfo.LatestVersion} is available.\nCurrent version: {updateInfo.CurrentVersion}\n\nOpen the download page?";

        return mainWindow?.IsVisible == true
            ? System.Windows.MessageBox.Show(mainWindow, message, title, MessageBoxButton.YesNo, MessageBoxImage.Information)
            : System.Windows.MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Information);
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
        updateChecker.Dispose();
        HideProgressWindow();
        notifyIcon.Visible = false;
        notifyIcon.Dispose();
    }
}
