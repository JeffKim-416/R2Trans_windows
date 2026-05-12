using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using R2Trans.Windows.Localization;
using R2Trans.Windows.Models;
using R2Trans.Windows.Services;
using Clipboard = System.Windows.Clipboard;
using MessageBox = System.Windows.MessageBox;

namespace R2Trans.Windows;

public partial class MainWindow : Window
{
    private readonly AppController controller;
    private bool isLoading;

    public MainWindow(AppController controller)
    {
        this.controller = controller;
        InitializeComponent();
        PopulateControls();
        ReloadLocalizedText();
        ReloadValues();
    }

    public event EventHandler? SettingsSaved;
    public event EventHandler? LiveInterpreterRequested;

    public void ShowAndActivate()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
    }

    public void SetStatus(string status)
    {
        StatusText.Text = status;
    }

    private void PopulateControls()
    {
        AppLanguageCombo.ItemsSource = Enum.GetValues<AppLanguage>();
        WorkModeCombo.ItemsSource = Enum.GetValues<WorkMode>();
        SourceLanguageCombo.ItemsSource = SupportedLanguage.All;
        TargetLanguageCombo.ItemsSource = SupportedLanguage.All;
        AutoPairCombo.ItemsSource = Enum.GetValues<AutoDetectPair>();
        StyleCombo.ItemsSource = Enum.GetValues<TranslationStyle>();
        ModelCombo.ItemsSource = SupportedModel.All;

        AppLanguageCombo.DisplayMemberPath = nameof(AppLanguage);
        SourceLanguageCombo.DisplayMemberPath = nameof(SupportedLanguage.DisplayName);
        TargetLanguageCombo.DisplayMemberPath = nameof(SupportedLanguage.DisplayName);
        ModelCombo.DisplayMemberPath = nameof(SupportedModel.DisplayName);
    }

    public void ReloadLocalizedText()
    {
        Title = AppText.Text(TextKey.SettingsTitle);
        TitleText.Text = AppText.Text(TextKey.SettingsTitle);
        ApiKeyLabel.Text = AppText.Text(TextKey.OpenAIAPIKey);
        PasteApiKeyButton.Content = AppText.Text(TextKey.Paste);
        CreateApiKeyButton.Content = AppText.Text(TextKey.CreateAPIKey);
        CreateApiKeyButton.ToolTip = AppText.Text(TextKey.OpenAIAPIKeyHelp);
        AppLanguageLabel.Text = AppText.Text(TextKey.AppLanguage);
        WorkModeLabel.Text = AppText.Text(TextKey.WorkMode);
        TranslationModeLabel.Text = AppText.Text(TextKey.TranslationMode);
        AutoDetectLabel.Text = AppText.Text(TextKey.AutoDetect);
        AutoDetectCheckBox.Content = AppText.Text(TextKey.AutoDetect);
        ConfirmBeforeReplaceLabel.Text = AppText.Text(TextKey.ConfirmBeforeReplace);
        ConfirmBeforeReplaceCheckBox.Content = AppText.Text(TextKey.ConfirmBeforeReplace);
        StyleLabel.Text = AppText.Text(TextKey.TranslationStyle);
        HotKeyLabel.Text = AppText.Text(TextKey.Hotkey);
        ModelLabel.Text = AppText.Text(TextKey.Model);
        LiveInterpreterButton.Content = AppText.Text(TextKey.LiveInterpreter);
        LaunchAtLoginLabel.Text = AppText.Text(TextKey.LaunchAtLogin);
        LaunchAtLoginCheckBox.Content = AppText.Text(TextKey.LaunchAtLogin);
        ShowTrayIconCheckBox.Content = AppText.Text(TextKey.ShowTrayIcon);
        TrayWarningText.Text = AppText.Text(TextKey.TrayHiddenWarning);
        CloseButton.Content = AppText.Text(TextKey.Close);
        SaveButton.Content = AppText.Text(TextKey.Save);

        RefreshLocalizedComboItems();
    }

    public void ReloadValues()
    {
        isLoading = true;
        var settings = controller.SettingsStore.Current;
        AppText.Language = settings.AppLanguage;
        ApiKeyBox.Password = controller.CredentialStore.LoadApiKey();
        AppLanguageCombo.SelectedValue = settings.AppLanguage;
        WorkModeCombo.SelectedValue = settings.WorkMode;
        SourceLanguageCombo.SelectedItem = SupportedLanguage.All.First(language => language.Code == settings.SourceLanguageCode);
        TargetLanguageCombo.SelectedItem = SupportedLanguage.All.First(language => language.Code == settings.TargetLanguageCode);
        AutoDetectCheckBox.IsChecked = settings.AutoDetectEnabled;
        AutoPairCombo.SelectedValue = settings.AutoDetectPair;
        ConfirmBeforeReplaceCheckBox.IsChecked = settings.ConfirmBeforeReplace;
        StyleCombo.SelectedValue = settings.TranslationStyle;
        SetHotKeyText(settings.HotKeyString);
        ModelCombo.SelectedItem = SupportedModel.All.First(model => model.Id == settings.Model);
        LaunchAtLoginCheckBox.IsChecked = StartupManager.IsEnabled;
        ShowTrayIconCheckBox.IsChecked = settings.ShowTrayIcon;
        isLoading = false;
        RefreshModeAvailability();
    }

    private void RefreshLocalizedComboItems()
    {
        WorkModeCombo.ItemsSource = null;
        WorkModeCombo.ItemsSource = Enum.GetValues<WorkMode>().Select(mode => new ComboOption<WorkMode>(mode, mode.DisplayName())).ToList();
        WorkModeCombo.DisplayMemberPath = nameof(ComboOption<WorkMode>.Label);
        WorkModeCombo.SelectedValuePath = nameof(ComboOption<WorkMode>.Value);

        AutoPairCombo.ItemsSource = null;
        AutoPairCombo.ItemsSource = Enum.GetValues<AutoDetectPair>().Select(pair => new ComboOption<AutoDetectPair>(pair, pair.DisplayName())).ToList();
        AutoPairCombo.DisplayMemberPath = nameof(ComboOption<AutoDetectPair>.Label);
        AutoPairCombo.SelectedValuePath = nameof(ComboOption<AutoDetectPair>.Value);

        StyleCombo.ItemsSource = null;
        StyleCombo.ItemsSource = Enum.GetValues<TranslationStyle>().Select(style => new ComboOption<TranslationStyle>(style, style.DisplayName())).ToList();
        StyleCombo.DisplayMemberPath = nameof(ComboOption<TranslationStyle>.Label);
        StyleCombo.SelectedValuePath = nameof(ComboOption<TranslationStyle>.Value);

        AppLanguageCombo.ItemsSource = null;
        AppLanguageCombo.ItemsSource = Enum.GetValues<AppLanguage>().Select(language => new ComboOption<AppLanguage>(language, language.DisplayName())).ToList();
        AppLanguageCombo.DisplayMemberPath = nameof(ComboOption<AppLanguage>.Label);
        AppLanguageCombo.SelectedValuePath = nameof(ComboOption<AppLanguage>.Value);
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var settings = controller.SettingsStore.Current;
            var selectedSource = (SupportedLanguage)SourceLanguageCombo.SelectedItem;
            var selectedTarget = (SupportedLanguage)TargetLanguageCombo.SelectedItem;
            var selectedModel = (SupportedModel)ModelCombo.SelectedItem;

            var hotKey = HotKeyParser.NormalizeString((HotKeyTextBox.Tag as string) ?? HotKeyTextBox.Text);
            HotKeyValidator.Validate(hotKey);

            settings.AppLanguage = (AppLanguage)AppLanguageCombo.SelectedValue;
            settings.WorkMode = (WorkMode)WorkModeCombo.SelectedValue;
            settings.SourceLanguageCode = selectedSource.Code;
            settings.TargetLanguageCode = selectedTarget.Code;
            settings.AutoDetectEnabled = AutoDetectCheckBox.IsChecked == true;
            settings.AutoDetectPair = (AutoDetectPair)AutoPairCombo.SelectedValue;
            settings.ConfirmBeforeReplace = ConfirmBeforeReplaceCheckBox.IsChecked == true;
            settings.TranslationStyle = (TranslationStyle)StyleCombo.SelectedValue;
            settings.HotKeyString = hotKey;
            settings.Model = selectedModel.Id;
            settings.ShowTrayIcon = ShowTrayIconCheckBox.IsChecked == true;

            controller.CredentialStore.SaveApiKey(ApiKeyBox.Password);
            StartupManager.SetEnabled(LaunchAtLoginCheckBox.IsChecked == true);
            controller.SettingsStore.Save();
            SettingsSaved?.Invoke(this, EventArgs.Empty);
            Close();
        }
        catch (Exception exception)
        {
            MessageBox.Show(this, exception.Message, AppText.Text(TextKey.SettingsError), MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void PasteApiKeyButton_Click(object sender, RoutedEventArgs e)
    {
        if (Clipboard.ContainsText())
        {
            ApiKeyBox.Password = Clipboard.GetText();
        }
    }

    private void HotKeyTextBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        e.Handled = true;

        var key = e.Key switch
        {
            Key.System => e.SystemKey,
            Key.ImeProcessed => e.ImeProcessedKey,
            _ => e.Key
        };

        if (IsModifierKey(key))
        {
            HotKeyTextBox.Text = HotKeyDisplayFromModifiers(Keyboard.Modifiers);
            return;
        }

        var modifiers = Keyboard.Modifiers;
        if (!TryNormalizeHotKey(key, modifiers, out var normalized))
        {
            return;
        }

        SetHotKeyText(normalized);
    }

    private void HotKeyTextBox_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        controller.SuspendHotKey();
        HotKeyTextBox.SelectAll();
    }

    private void HotKeyTextBox_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        controller.ResumeHotKey();
    }

    private void HotKeyTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        e.Handled = true;
    }

    private void CreateApiKeyButton_Click(object sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo("https://platform.openai.com/api-keys")
        {
            UseShellExecute = true
        });
    }

    private void LiveInterpreterButton_Click(object sender, RoutedEventArgs e)
    {
        LiveInterpreterRequested?.Invoke(this, EventArgs.Empty);
    }

    private void WorkModeCombo_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        RefreshModeAvailability();
    }

    private void AutoDetectCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        RefreshModeAvailability();
    }

    private void AppLanguageCombo_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (isLoading || AppLanguageCombo.SelectedValue is not AppLanguage language)
        {
            return;
        }

        AppText.Language = language;
        controller.SettingsStore.Current.AppLanguage = language;
        controller.SettingsStore.Save();
        ReloadLocalizedText();
        ReloadValues();
    }

    private void RefreshModeAvailability()
    {
        if (WorkModeCombo.SelectedValue is not WorkMode workMode)
        {
            return;
        }

        var isTranslationMode = workMode == WorkMode.Translation;
        var autoDetect = AutoDetectCheckBox.IsChecked == true;

        SourceLanguageCombo.IsEnabled = isTranslationMode && !autoDetect;
        TargetLanguageCombo.IsEnabled = isTranslationMode && !autoDetect;
        AutoDetectCheckBox.IsEnabled = isTranslationMode;
        AutoPairCombo.IsEnabled = isTranslationMode && autoDetect;
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (!controller.IsShuttingDown && controller.SettingsStore.Current.ShowTrayIcon)
        {
            e.Cancel = true;
            Hide();
            return;
        }

        base.OnClosing(e);
    }

    protected override void OnClosed(EventArgs e)
    {
        if (!controller.IsShuttingDown && !controller.SettingsStore.Current.ShowTrayIcon)
        {
            controller.Shutdown();
        }

        base.OnClosed(e);
    }

    private sealed record ComboOption<T>(T Value, string Label);

    private static bool TryNormalizeHotKey(Key key, ModifierKeys modifiers, out string normalized)
    {
        normalized = string.Empty;
        var nativeModifiers = NativeModifiers(modifiers);
        if (nativeModifiers == 0)
        {
            return false;
        }

        var virtualKey = KeyInterop.VirtualKeyFromKey(key);
        if (virtualKey == 0)
        {
            return false;
        }

        try
        {
            normalized = HotKeyParser.Normalize(new HotKey((uint)virtualKey, nativeModifiers));
            return true;
        }
        catch (R2TransException)
        {
            return false;
        }
    }

    private static uint NativeModifiers(ModifierKeys modifiers)
    {
        uint nativeModifiers = 0;
        if ((modifiers & ModifierKeys.Control) != 0)
        {
            nativeModifiers |= NativeMethods.ModControl;
        }

        if ((modifiers & ModifierKeys.Alt) != 0)
        {
            nativeModifiers |= NativeMethods.ModAlt;
        }

        if ((modifiers & ModifierKeys.Shift) != 0)
        {
            nativeModifiers |= NativeMethods.ModShift;
        }

        if ((modifiers & ModifierKeys.Windows) != 0)
        {
            nativeModifiers |= NativeMethods.ModWin;
        }

        return nativeModifiers;
    }

    private static bool IsModifierKey(Key key)
    {
        return key is Key.LeftCtrl or Key.RightCtrl
            or Key.LeftAlt or Key.RightAlt
            or Key.LeftShift or Key.RightShift
            or Key.LWin or Key.RWin;
    }

    private static string HotKeyDisplayFromModifiers(ModifierKeys modifiers)
    {
        var parts = new List<string>();
        if ((modifiers & ModifierKeys.Control) != 0)
        {
            parts.Add("Ctrl");
        }

        if ((modifiers & ModifierKeys.Alt) != 0)
        {
            parts.Add("Alt");
        }

        if ((modifiers & ModifierKeys.Shift) != 0)
        {
            parts.Add("Shift");
        }

        if ((modifiers & ModifierKeys.Windows) != 0)
        {
            parts.Add("Win");
        }

        return parts.Count == 0 ? string.Empty : $"{string.Join("+", parts)}+...";
    }

    private void SetHotKeyText(string normalized)
    {
        HotKeyTextBox.Tag = HotKeyParser.NormalizeString(normalized);
        HotKeyTextBox.Text = HotKeyParser.DisplayString(normalized);
    }
}
