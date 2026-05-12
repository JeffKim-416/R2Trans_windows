using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using R2Trans.Windows.Localization;
using R2Trans.Windows.Models;

namespace R2Trans.Windows.Services;

public sealed class GlobalHotKeyService : IDisposable
{
    private const int HotKeyId = 0x5254;
    private HwndSource? source;
    private IntPtr handle;
    private Func<Task>? action;
    private HotKey? parsedHotKey;
    private bool isDispatching;

    public void Register(Window window, string hotKeyString, Func<Task> action)
    {
        Unregister();
        HotKeyValidator.Validate(hotKeyString);

        var parsed = HotKeyParser.Parse(hotKeyString);
        parsedHotKey = parsed;
        var helper = new WindowInteropHelper(window);
        handle = helper.Handle;
        source = HwndSource.FromHwnd(handle);
        source?.AddHook(WndProc);
        this.action = action;

        var registered = NativeMethods.RegisterHotKey(
            handle,
            HotKeyId,
            parsed.Modifiers | NativeMethods.ModNoRepeat,
            parsed.KeyCode);

        if (!registered)
        {
            throw new R2TransException($"{AppText.Text(TextKey.InvalidHotkey)}: {new Win32Exception(Marshal.GetLastWin32Error()).Message}");
        }
    }

    public void Unregister()
    {
        if (handle != IntPtr.Zero)
        {
            NativeMethods.UnregisterHotKey(handle, HotKeyId);
            handle = IntPtr.Zero;
        }

        if (source is not null)
        {
            source.RemoveHook(WndProc);
            source = null;
        }

        action = null;
        parsedHotKey = null;
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == NativeMethods.WmHotKey && wParam.ToInt32() == HotKeyId)
        {
            handled = true;
            _ = DispatchAfterHotKeyReleaseAsync();
        }

        return IntPtr.Zero;
    }

    private async Task DispatchAfterHotKeyReleaseAsync()
    {
        if (isDispatching || action is null)
        {
            return;
        }

        isDispatching = true;
        try
        {
            await WaitForHotKeyReleaseAsync();
            await action();
        }
        finally
        {
            isDispatching = false;
        }
    }

    private async Task WaitForHotKeyReleaseAsync()
    {
        if (parsedHotKey is null)
        {
            return;
        }

        var keysToWatch = new HashSet<ushort> { (ushort)parsedHotKey.Value.KeyCode };

        if ((parsedHotKey.Value.Modifiers & NativeMethods.ModControl) != 0)
        {
            keysToWatch.Add(NativeMethods.VkControl);
        }

        if ((parsedHotKey.Value.Modifiers & NativeMethods.ModAlt) != 0)
        {
            keysToWatch.Add(NativeMethods.VkMenu);
        }

        if ((parsedHotKey.Value.Modifiers & NativeMethods.ModShift) != 0)
        {
            keysToWatch.Add(NativeMethods.VkShift);
        }

        if ((parsedHotKey.Value.Modifiers & NativeMethods.ModWin) != 0)
        {
            keysToWatch.Add(NativeMethods.VkLWin);
            keysToWatch.Add(NativeMethods.VkRWin);
        }

        for (var attempt = 0; attempt < 40; attempt++)
        {
            if (!keysToWatch.Any(NativeMethods.IsKeyDown))
            {
                await Task.Delay(80);
                return;
            }

            await Task.Delay(25);
        }
    }

    public void Dispose()
    {
        Unregister();
    }
}
