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
    private const int ProbeHotKeyId = HotKeyId + 1;
    private HwndSource? source;
    private IntPtr handle;
    private Action? action;
    private HotKey? registeredHotKey;

    public void Register(Window window, string hotKeyString, Action action)
    {
        HotKeyValidator.Validate(hotKeyString);

        var parsed = HotKeyParser.Parse(hotKeyString);
        var helper = new WindowInteropHelper(window);
        var newHandle = helper.Handle;
        if (newHandle == IntPtr.Zero)
        {
            throw new R2TransException(AppText.Text(TextKey.InvalidHotkey));
        }

        var newSource = HwndSource.FromHwnd(newHandle);
        if (newSource is null)
        {
            throw new R2TransException(AppText.Text(TextKey.InvalidHotkey));
        }

        if (handle == newHandle && registeredHotKey == parsed)
        {
            this.action = action;
            return;
        }

        ProbeRegistration(newHandle, parsed);

        var previousHandle = handle;
        var previousSource = source;
        var previousHotKey = registeredHotKey;

        if (previousHandle != IntPtr.Zero)
        {
            NativeMethods.UnregisterHotKey(previousHandle, HotKeyId);
        }

        var registered = NativeMethods.RegisterHotKey(
            newHandle,
            HotKeyId,
            parsed.Modifiers | NativeMethods.ModNoRepeat,
            parsed.KeyCode);

        if (!registered)
        {
            var errorCode = Marshal.GetLastWin32Error();
            RestorePreviousRegistration(previousHandle, previousHotKey);
            throw RegistrationException(errorCode);
        }

        if (!ReferenceEquals(previousSource, newSource))
        {
            previousSource?.RemoveHook(WndProc);
            newSource.AddHook(WndProc);
        }

        handle = newHandle;
        source = newSource;
        this.action = action;
        registeredHotKey = parsed;
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
        registeredHotKey = null;
    }

    private static void ProbeRegistration(IntPtr targetHandle, HotKey hotKey)
    {
        var registered = NativeMethods.RegisterHotKey(
            targetHandle,
            ProbeHotKeyId,
            hotKey.Modifiers | NativeMethods.ModNoRepeat,
            hotKey.KeyCode);

        if (!registered)
        {
            throw RegistrationException(Marshal.GetLastWin32Error());
        }

        NativeMethods.UnregisterHotKey(targetHandle, ProbeHotKeyId);
    }

    private void RestorePreviousRegistration(IntPtr previousHandle, HotKey? previousHotKey)
    {
        if (previousHandle == IntPtr.Zero || previousHotKey is not { } hotKey)
        {
            handle = IntPtr.Zero;
            source = null;
            action = null;
            registeredHotKey = null;
            return;
        }

        var restored = NativeMethods.RegisterHotKey(
            previousHandle,
            HotKeyId,
            hotKey.Modifiers | NativeMethods.ModNoRepeat,
            hotKey.KeyCode);

        if (!restored)
        {
            source?.RemoveHook(WndProc);
            handle = IntPtr.Zero;
            source = null;
            action = null;
            registeredHotKey = null;
        }
    }

    private static R2TransException RegistrationException(int errorCode)
    {
        return new R2TransException($"{AppText.Text(TextKey.InvalidHotkey)}: {new Win32Exception(errorCode).Message}");
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
