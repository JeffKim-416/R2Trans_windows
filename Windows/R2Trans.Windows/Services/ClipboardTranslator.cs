using System.Windows;
using R2Trans.Windows.Localization;
using R2Trans.Windows.Models;
using Keys = System.Windows.Forms.Keys;

namespace R2Trans.Windows.Services;

public enum TranslationOutcome
{
    Replaced,
    Copied,
    Cancelled
}

public sealed class ClipboardTranslator
{
    private readonly SettingsStore settingsStore;
    private readonly OpenAITranslator translator;
    private bool isTranslating;

    public ClipboardTranslator(SettingsStore settingsStore, OpenAITranslator translator)
    {
        this.settingsStore = settingsStore;
        this.translator = translator;
    }

    public async Task<TranslationOutcome> TranslateSelectionAsync(Window owner, Action<string>? statusChanged = null)
    {
        if (isTranslating)
        {
            throw new R2TransException(AppText.Text(TextKey.AlreadyTranslating));
        }

        isTranslating = true;
        var targetWindow = NativeMethods.GetForegroundWindow();
        var snapshot = Clipboard.GetDataObject();
        var originalSequence = NativeMethods.GetClipboardSequenceNumber();

        try
        {
            statusChanged?.Invoke(AppText.Text(TextKey.Translating));
            NativeMethods.SendShortcut(NativeMethods.VkControl, (ushort)Keys.C);
            var selectedText = await WaitForCopiedTextAsync(originalSequence);
            var translatedText = await translator.TranslateAsync(selectedText);

            if (settingsStore.Current.ConfirmBeforeReplace)
            {
                var confirmation = new TranslationConfirmationWindow(translatedText)
                {
                    Owner = owner,
                    Topmost = true
                };
                confirmation.ShowDialog();

                switch (confirmation.Action)
                {
                    case TranslationConfirmationAction.Replace:
                        await PasteAsync(translatedText, targetWindow);
                        RestoreClipboard(snapshot);
                        return TranslationOutcome.Replaced;
                    case TranslationConfirmationAction.Copy:
                        Clipboard.SetText(translatedText);
                        return TranslationOutcome.Copied;
                    default:
                        RestoreClipboard(snapshot);
                        return TranslationOutcome.Cancelled;
                }
            }

            await PasteAsync(translatedText, targetWindow);
            RestoreClipboard(snapshot);
            return TranslationOutcome.Replaced;
        }
        catch
        {
            RestoreClipboard(snapshot);
            throw;
        }
        finally
        {
            isTranslating = false;
            statusChanged?.Invoke(string.Empty);
        }
    }

    private static async Task<string> WaitForCopiedTextAsync(uint originalSequence)
    {
        for (var attempt = 0; attempt < 20; attempt++)
        {
            await Task.Delay(50);

            if (NativeMethods.GetClipboardSequenceNumber() == originalSequence)
            {
                continue;
            }

            if (Clipboard.ContainsText())
            {
                var text = Clipboard.GetText();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    return text;
                }
            }
        }

        throw new R2TransException(AppText.Text(TextKey.ClipboardTextMissing));
    }

    private static async Task PasteAsync(string translatedText, IntPtr targetWindow)
    {
        Clipboard.SetText(translatedText);

        if (targetWindow != IntPtr.Zero)
        {
            NativeMethods.SetForegroundWindow(targetWindow);
            await Task.Delay(180);
        }

        NativeMethods.SendShortcut(NativeMethods.VkControl, (ushort)Keys.V);
        await Task.Delay(450);
    }

    private static void RestoreClipboard(IDataObject? snapshot)
    {
        if (snapshot is null)
        {
            return;
        }

        try
        {
            Clipboard.SetDataObject(snapshot, copy: true);
        }
        catch
        {
            // Clipboard ownership is best-effort on Windows; translation should not fail after paste.
        }
    }
}
