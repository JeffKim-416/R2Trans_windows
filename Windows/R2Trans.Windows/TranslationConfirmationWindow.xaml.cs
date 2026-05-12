using System.Windows;
using R2Trans.Windows.Localization;

namespace R2Trans.Windows;

public enum TranslationConfirmationAction
{
    Replace,
    Copy,
    Cancel
}

public partial class TranslationConfirmationWindow : Window
{
    public TranslationConfirmationAction Action { get; private set; } = TranslationConfirmationAction.Cancel;

    public TranslationConfirmationWindow(string translatedText)
    {
        InitializeComponent();
        Title = AppText.Text(TextKey.ConfirmTranslationTitle);
        TitleText.Text = AppText.Text(TextKey.ConfirmTranslationTitle);
        MessageText.Text = AppText.Text(TextKey.ConfirmTranslationMessage);
        PreviewTextBox.Text = translatedText;
        ReplaceButton.Content = AppText.Text(TextKey.Replace);
        CopyButton.Content = AppText.Text(TextKey.CopyOnly);
        CancelButton.Content = AppText.Text(TextKey.Cancel);
    }

    private void ReplaceButton_Click(object sender, RoutedEventArgs e)
    {
        Action = TranslationConfirmationAction.Replace;
        DialogResult = true;
    }

    private void CopyButton_Click(object sender, RoutedEventArgs e)
    {
        Action = TranslationConfirmationAction.Copy;
        DialogResult = true;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        Action = TranslationConfirmationAction.Cancel;
        DialogResult = false;
    }
}
