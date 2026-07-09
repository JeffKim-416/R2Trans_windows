using System.Windows.Forms;
using R2Trans.Windows.Localization;
using R2Trans.Windows.Models;

namespace R2Trans.Windows.Services;

public readonly record struct HotKey(uint KeyCode, uint Modifiers);

public static class HotKeyParser
{
    public static HotKey Parse(string value)
    {
        var parts = value
            .ToLowerInvariant()
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (parts.Length < 2)
        {
            throw new R2TransException($"{AppText.Text(TextKey.InvalidHotkey)}: {value}.");
        }

        uint modifiers = 0;
        uint? keyCode = null;

        foreach (var part in parts)
        {
            switch (part)
            {
                case "control":
                case "ctrl":
                    modifiers |= NativeMethods.ModControl;
                    break;
                case "option":
                case "opt":
                case "alt":
                    modifiers |= NativeMethods.ModAlt;
                    break;
                case "shift":
                    modifiers |= NativeMethods.ModShift;
                    break;
                case "command":
                case "cmd":
                case "win":
                case "windows":
                    modifiers |= NativeMethods.ModWin;
                    break;
                default:
                    if (keyCode is not null || !TryKeyCode(part, out var parsedKeyCode))
                    {
                        throw new R2TransException($"{AppText.Text(TextKey.InvalidHotkey)}: {value}.");
                    }

                    keyCode = parsedKeyCode;
                    break;
            }
        }

        if (keyCode is null || modifiers == 0)
        {
            throw new R2TransException($"{AppText.Text(TextKey.InvalidHotkey)}: {value}.");
        }

        return new HotKey(keyCode.Value, modifiers);
    }

    public static string NormalizeString(string value)
    {
        var hotKey = Parse(value);
        return Normalize(hotKey);
    }

    public static string Normalize(HotKey hotKey)
    {
        var parts = ModifierTokens(hotKey.Modifiers);
        parts.Add(KeyToken(hotKey.KeyCode));
        return string.Join('+', parts);
    }

    public static string DisplayString(string value)
    {
        try
        {
            var normalized = NormalizeString(value);
            return string.Join("+", normalized.Split('+').Select(part => part switch
            {
                "control" => "Ctrl",
                "alt" => "Alt",
                "shift" => "Shift",
                "win" => "Win",
                "space" => "Space",
                _ => part.ToUpperInvariant()
            }));
        }
        catch
        {
            return value;
        }
    }

    private static List<string> ModifierTokens(uint modifiers)
    {
        var parts = new List<string>();
        if ((modifiers & NativeMethods.ModControl) != 0)
        {
            parts.Add("control");
        }

        if ((modifiers & NativeMethods.ModAlt) != 0)
        {
            parts.Add("alt");
        }

        if ((modifiers & NativeMethods.ModShift) != 0)
        {
            parts.Add("shift");
        }

        if ((modifiers & NativeMethods.ModWin) != 0)
        {
            parts.Add("win");
        }

        return parts;
    }

    private static bool TryKeyCode(string token, out uint keyCode)
    {
        var key = token switch
        {
            "a" => Keys.A,
            "b" => Keys.B,
            "c" => Keys.C,
            "d" => Keys.D,
            "e" => Keys.E,
            "f" => Keys.F,
            "g" => Keys.G,
            "h" => Keys.H,
            "i" => Keys.I,
            "j" => Keys.J,
            "k" => Keys.K,
            "l" => Keys.L,
            "m" => Keys.M,
            "n" => Keys.N,
            "o" => Keys.O,
            "p" => Keys.P,
            "q" => Keys.Q,
            "r" => Keys.R,
            "s" => Keys.S,
            "t" => Keys.T,
            "u" => Keys.U,
            "v" => Keys.V,
            "w" => Keys.W,
            "x" => Keys.X,
            "y" => Keys.Y,
            "z" => Keys.Z,
            "0" => Keys.D0,
            "1" => Keys.D1,
            "2" => Keys.D2,
            "3" => Keys.D3,
            "4" => Keys.D4,
            "5" => Keys.D5,
            "6" => Keys.D6,
            "7" => Keys.D7,
            "8" => Keys.D8,
            "9" => Keys.D9,
            "space" => Keys.Space,
            "-" => Keys.OemMinus,
            "=" => Keys.Oemplus,
            "[" => Keys.OemOpenBrackets,
            "]" => Keys.OemCloseBrackets,
            "\\" => Keys.OemPipe,
            ";" => Keys.OemSemicolon,
            "'" => Keys.OemQuotes,
            "," => Keys.Oemcomma,
            "." => Keys.OemPeriod,
            "/" => Keys.OemQuestion,
            _ => Keys.None
        };

        keyCode = (uint)key;
        return key != Keys.None;
    }

    private static string KeyToken(uint keyCode)
    {
        var key = (Keys)keyCode;
        return key switch
        {
            Keys.A => "a",
            Keys.B => "b",
            Keys.C => "c",
            Keys.D => "d",
            Keys.E => "e",
            Keys.F => "f",
            Keys.G => "g",
            Keys.H => "h",
            Keys.I => "i",
            Keys.J => "j",
            Keys.K => "k",
            Keys.L => "l",
            Keys.M => "m",
            Keys.N => "n",
            Keys.O => "o",
            Keys.P => "p",
            Keys.Q => "q",
            Keys.R => "r",
            Keys.S => "s",
            Keys.T => "t",
            Keys.U => "u",
            Keys.V => "v",
            Keys.W => "w",
            Keys.X => "x",
            Keys.Y => "y",
            Keys.Z => "z",
            Keys.D0 => "0",
            Keys.D1 => "1",
            Keys.D2 => "2",
            Keys.D3 => "3",
            Keys.D4 => "4",
            Keys.D5 => "5",
            Keys.D6 => "6",
            Keys.D7 => "7",
            Keys.D8 => "8",
            Keys.D9 => "9",
            Keys.Space => "space",
            Keys.OemMinus => "-",
            Keys.Oemplus => "=",
            Keys.OemOpenBrackets => "[",
            Keys.OemCloseBrackets => "]",
            Keys.OemPipe => "\\",
            Keys.OemSemicolon => ";",
            Keys.OemQuotes => "'",
            Keys.Oemcomma => ",",
            Keys.OemPeriod => ".",
            Keys.OemQuestion => "/",
            _ => throw new R2TransException(AppText.Text(TextKey.UnsupportedKey))
        };
    }
}
