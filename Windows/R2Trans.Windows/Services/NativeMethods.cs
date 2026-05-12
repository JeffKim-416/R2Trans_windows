using System.ComponentModel;
using System.Runtime.InteropServices;

namespace R2Trans.Windows.Services;

internal static class NativeMethods
{
    internal const int WmHotKey = 0x0312;
    internal const uint ModAlt = 0x0001;
    internal const uint ModControl = 0x0002;
    internal const uint ModShift = 0x0004;
    internal const uint ModWin = 0x0008;
    internal const uint ModNoRepeat = 0x4000;
    internal const ushort VkControl = 0x11;
    internal const ushort VkMenu = 0x12;
    internal const ushort VkShift = 0x10;
    internal const ushort VkLWin = 0x5B;
    internal const ushort VkRWin = 0x5C;
    internal const uint InputKeyboard = 1;
    internal const uint KeyEventFExtendedKey = 0x0001;
    internal const uint KeyEventFKeyUp = 0x0002;
    internal const uint KeyEventFScanCode = 0x0008;
    private const uint MapvkVkToVsc = 0;

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    [DllImport("user32.dll")]
    internal static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    internal static extern uint GetClipboardSequenceNumber();

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    internal static bool IsKeyDown(ushort virtualKey)
    {
        return (GetAsyncKeyState(virtualKey) & 0x8000) != 0;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [DllImport("user32.dll")]
    private static extern uint MapVirtualKey(uint uCode, uint uMapType);

    internal static void SendShortcut(params ushort[] virtualKeys)
    {
        var inputs = new List<INPUT>();

        foreach (var key in virtualKeys)
        {
            inputs.Add(CreateKeyboardInput(key, keyUp: false));
        }

        for (var index = virtualKeys.Length - 1; index >= 0; index--)
        {
            inputs.Add(CreateKeyboardInput(virtualKeys[index], keyUp: true));
        }

        if (inputs.Count > 0)
        {
            var sent = SendInput((uint)inputs.Count, inputs.ToArray(), Marshal.SizeOf<INPUT>());
            if (sent != inputs.Count)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }
        }
    }

    private static INPUT CreateKeyboardInput(ushort virtualKey, bool keyUp)
    {
        var scanCode = (ushort)MapVirtualKey(virtualKey, MapvkVkToVsc);
        var flags = KeyEventFScanCode;
        if (keyUp)
        {
            flags |= KeyEventFKeyUp;
        }

        if (IsExtendedKey(virtualKey))
        {
            flags |= KeyEventFExtendedKey;
        }

        return new INPUT
        {
            type = InputKeyboard,
            U = new InputUnion
            {
                ki = new KEYBDINPUT
                {
                    wVk = 0,
                    wScan = scanCode,
                    dwFlags = flags,
                    time = 0,
                    dwExtraInfo = UIntPtr.Zero
                }
            }
        };
    }

    private static bool IsExtendedKey(ushort virtualKey)
    {
        return virtualKey is 0x21 // Page Up
            or 0x22 // Page Down
            or 0x23 // End
            or 0x24 // Home
            or 0x25 // Left
            or 0x26 // Up
            or 0x27 // Right
            or 0x28 // Down
            or 0x2D // Insert
            or 0x2E // Delete
            or 0x6F // Divide
            or VkRWin;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public uint type;
        public InputUnion U;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)]
        public MOUSEINPUT mi;

        [FieldOffset(0)]
        public KEYBDINPUT ki;

        [FieldOffset(0)]
        public HARDWAREINPUT hi;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MOUSEINPUT
    {
        public int dx;
        public int dy;
        public uint mouseData;
        public uint dwFlags;
        public uint time;
        public UIntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public UIntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct HARDWAREINPUT
    {
        public uint uMsg;
        public ushort wParamL;
        public ushort wParamH;
    }
}
