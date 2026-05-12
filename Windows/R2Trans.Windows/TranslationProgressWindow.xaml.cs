using System.Windows;
using R2Trans.Windows.Localization;

namespace R2Trans.Windows;

public partial class TranslationProgressWindow : Window
{
    public TranslationProgressWindow()
    {
        InitializeComponent();
        Title = "R2Trans";
        MessageText.Text = AppText.Text(TextKey.Translating);
    }

    public void SetMessage(string message)
    {
        MessageText.Text = string.IsNullOrWhiteSpace(message)
            ? AppText.Text(TextKey.Translating)
            : message;
    }
}
