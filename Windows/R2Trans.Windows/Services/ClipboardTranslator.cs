using System.Windows;
using R2Trans.Windows.Localization;
using R2Trans.Windows.Models;
using Clipboard = System.Windows.Clipboard;
using ClipboardDataObject = System.Windows.IDataObject;
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
        var snapshot = TryGetClipboardDataObject();

        try
        {
            statusChanged?.Invoke(AppText.Text(TextKey.Translating));
            var selectedText = await CopySelectionAsync(targetWindow);
            var translatedText = await translator.TranslateAsync(selectedText);
            statusChanged?.Invoke(string.Empty);

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
                        SetClipboardText(translatedText);
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

    private static async Task<string> WaitForCopiedTextAsync(uint originalSequence, string ignoredText)
    {
        for (var attempt = 0; attempt < 40; attempt++)
        {
            await Task.Delay(50);

            if (NativeMethods.GetClipboardSequenceNumber() != originalSequence
                && TryGetClipboardText(out var text))
            {
                if (!string.IsNullOrWhiteSpace(text))
                {
                    if (!string.Equals(text, ignoredText, StringComparison.Ordinal))
                    {
                        return text;
                    }
                }
            }
        }

        throw new R2TransException(AppText.Text(TextKey.ClipboardTextMissing));
    }

    private static async Task<string> CopySelectionAsync(IntPtr targetWindow)
    {
        if (targetWindow != IntPtr.Zero)
        {
            NativeMethods.SetForegroundWindow(targetWindow);
            await Task.Delay(180);
        }

        var marker = $"__R2TRANS_COPY_MARKER_{Guid.NewGuid():N}__";
        if (!TrySetClipboardText(marker))
        {
            TryClearClipboard();
        }

        var sequenceBeforeCopy = NativeMethods.GetClipboardSequenceNumber();

        NativeMethods.SendShortcut(NativeMethods.VkControl, (ushort)Keys.C);
        try
        {
            return await WaitForCopiedTextAsync(sequenceBeforeCopy, marker);
        }
        catch (R2TransException)
        {
            var sequenceBeforeFallback = NativeMethods.GetClipboardSequenceNumber();
            NativeMethods.SendShortcut(NativeMethods.VkControl, (ushort)Keys.Insert);
            return await WaitForCopiedTextAsync(sequenceBeforeFallback, marker);
        }
    }

    private static async Task PasteAsync(string translatedText, IntPtr targetWindow)
    {
        SetClipboardText(translatedText);

        if (targetWindow != IntPtr.Zero)
        {
            NativeMethods.SetForegroundWindow(targetWindow);
            await Task.Delay(180);
        }

        NativeMethods.SendShortcut(NativeMethods.VkControl, (ushort)Keys.V);
        await Task.Delay(450);
    }

    private static void RestoreClipboard(ClipboardDataObject? snapshot)
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

    private static ClipboardDataObject? TryGetClipboardDataObject()
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            try
            {
                return Clipboard.GetDataObject();
            }
            catch
            {
                Thread.Sleep(30);
            }
        }

        return null;
    }

    private static bool TryGetClipboardText(out string text)
    {
        text = string.Empty;
        try
        {
            if (!Clipboard.ContainsText())
            {
                return false;
            }

            text = Clipboard.GetText();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static void TryClearClipboard()
    {
        try
        {
            Clipboard.Clear();
        }
        catch
        {
            // The clipboard can be temporarily locked by other apps; copy may still replace it.
        }
    }

    private static void SetClipboardText(string text)
    {
        if (TrySetClipboardText(text))
        {
            return;
        }

        Clipboard.SetText(text);
    }

    private static bool TrySetClipboardText(string text)
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            try
            {
                Clipboard.SetText(text);
                return true;
            }
            catch
            {
                Thread.Sleep(30);
            }
        }

        return false;
    }
}
