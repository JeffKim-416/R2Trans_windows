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
    private Action? action;

    public void Register(Window window, string hotKeyString, Action action)
    {
        Unregister();
        HotKeyValidator.Validate(hotKeyString);

        var parsed = HotKeyParser.Parse(hotKeyString);
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
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == NativeMethods.WmHotKey && wParam.ToInt32() == HotKeyId)
        {
            handled = true;
            action?.Invoke();
        }

        return IntPtr.Zero;
    }

    public void Dispose()
    {
        Unregister();
    }
}
