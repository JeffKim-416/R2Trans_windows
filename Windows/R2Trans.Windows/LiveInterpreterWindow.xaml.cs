using System.Windows;
using R2Trans.Windows.Localization;
using R2Trans.Windows.Models;
using R2Trans.Windows.Services;

namespace R2Trans.Windows;

public partial class LiveInterpreterWindow : Window
{
    private readonly AppController controller;
    private readonly LiveInterpreterService service;
    private bool isRunning;

    public LiveInterpreterWindow(AppController controller)
    {
        this.controller = controller;
        service = new LiveInterpreterService(controller.CredentialStore);
        service.Update += Service_Update;
        InitializeComponent();
        PopulateControls();
        ReloadLocalizedText();
    }

    private void PopulateControls()
    {
        InputSourceCombo.ItemsSource = Enum.GetValues<LiveInterpreterInputSource>()
            .Select(source => new ComboOption<LiveInterpreterInputSource>(source, InputSourceDisplayName(source)))
            .ToList();
        InputSourceCombo.DisplayMemberPath = nameof(ComboOption<LiveInterpreterInputSource>.Label);
        InputSourceCombo.SelectedValuePath = nameof(ComboOption<LiveInterpreterInputSource>.Value);
        InputSourceCombo.SelectedValue = LiveInterpreterInputSource.Microphone;

        OutputLanguageCombo.ItemsSource = SupportedLanguage.All;
        OutputLanguageCombo.DisplayMemberPath = nameof(SupportedLanguage.DisplayName);
        OutputLanguageCombo.SelectedItem = SupportedLanguage.All.First(language => language.Code == controller.SettingsStore.Current.TargetLanguageCode);
    }

    private void ReloadLocalizedText()
    {
        Title = AppText.Text(TextKey.LiveInterpreterTitle);
        StatusLabel.Text = AppText.Text(TextKey.LiveInterpreterStopped);
        InputSourceLabel.Text = AppText.Text(TextKey.InputSource);
        OutputLanguageLabel.Text = AppText.Text(TextKey.TargetLanguage);
        KeepOnTopCheckBox.Content = AppText.Text(TextKey.KeepInterpreterOnTop);
        SourceTranscriptLabel.Text = AppText.Text(TextKey.SourceTranscript);
        TranslatedSubtitleLabel.Text = AppText.Text(TextKey.TranslatedSubtitle);
        SourceTranscriptBox.Text = AppText.Text(TextKey.LiveInterpreterNoSource);
        SubtitleBox.Text = AppText.Text(TextKey.LiveInterpreterWaitingSubtitle);
        MicrophoneLevelLabel.Text = AppText.Text(TextKey.MicrophoneLevel);
        SystemAudioLevelLabel.Text = AppText.Text(TextKey.SystemAudioLevel);
        DebugLabel.Text = AppText.Text(TextKey.LiveInterpreterBillingNote);
        ClearButton.Content = AppText.Text(TextKey.Clear);
        StartStopButton.Content = AppText.Text(TextKey.Start);
        CloseButton.Content = AppText.Text(TextKey.Close);
    }

    private async void StartStopButton_Click(object sender, RoutedEventArgs e)
    {
        if (isRunning)
        {
            service.Stop();
            return;
        }

        try
        {
            var inputSource = (LiveInterpreterInputSource)InputSourceCombo.SelectedValue;
            var outputLanguage = (SupportedLanguage)OutputLanguageCombo.SelectedItem;
            SetControlsEnabled(false);
            await service.StartAsync(inputSource, outputLanguage.Code);
        }
        catch (Exception exception)
        {
            SetControlsEnabled(true);
            MessageBox.Show(this, exception.Message, AppText.Text(TextKey.LiveInterpreterError), MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void ClearButton_Click(object sender, RoutedEventArgs e)
    {
        service.Clear();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void KeepOnTopCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        Topmost = KeepOnTopCheckBox.IsChecked == true;
    }

    private void Service_Update(object? sender, LiveInterpreterUpdate update)
    {
        Dispatcher.Invoke(() =>
        {
            switch (update)
            {
                case LiveInterpreterUpdate.RunningStateChanged runningState:
                    isRunning = runningState.IsRunning;
                    SetControlsEnabled(!isRunning);
                    StartStopButton.Content = isRunning ? AppText.Text(TextKey.Stop) : AppText.Text(TextKey.Start);
                    break;
                case LiveInterpreterUpdate.Status status:
                    StatusLabel.Text = status.Message;
                    break;
                case LiveInterpreterUpdate.SourceTranscript source:
                    SourceTranscriptBox.Text = string.IsNullOrWhiteSpace(source.Text)
                        ? AppText.Text(TextKey.LiveInterpreterNoSource)
                        : source.Text;
                    SourceTranscriptBox.ScrollToEnd();
                    break;
                case LiveInterpreterUpdate.Subtitle subtitle:
                    TargetLanguageLabel.Text = subtitle.LanguageLabel;
                    SubtitleBox.Text = string.IsNullOrWhiteSpace(subtitle.Text)
                        ? AppText.Text(TextKey.LiveInterpreterWaitingSubtitle)
                        : subtitle.Text;
                    SubtitleBox.ScrollToEnd();
                    break;
                case LiveInterpreterUpdate.AudioLevel level:
                    if (level.Source == LiveInterpreterAudioSource.Microphone)
                    {
                        MicrophoneLevelBar.Value = level.Level;
                    }
                    else
                    {
                        SystemAudioLevelBar.Value = level.Level;
                    }
                    break;
                case LiveInterpreterUpdate.Debug debug:
                    DebugLabel.Text = debug.Message;
                    break;
                case LiveInterpreterUpdate.Error error:
                    DebugLabel.Text = error.Message;
                    break;
            }
        });
    }

    private void SetControlsEnabled(bool enabled)
    {
        InputSourceCombo.IsEnabled = enabled;
        OutputLanguageCombo.IsEnabled = enabled;
    }

    protected override void OnClosed(EventArgs e)
    {
        service.Dispose();
        base.OnClosed(e);
    }

    private static string InputSourceDisplayName(LiveInterpreterInputSource inputSource) => inputSource switch
    {
        LiveInterpreterInputSource.SystemAudio => AppText.Text(TextKey.SystemAudioInput),
        LiveInterpreterInputSource.MicrophoneAndSystemAudio => AppText.Text(TextKey.MicrophoneAndSystemAudioInput),
        _ => AppText.Text(TextKey.MicrophoneInput)
    };

    private sealed record ComboOption<T>(T Value, string Label);
}
