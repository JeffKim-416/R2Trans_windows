using R2Trans.Windows.Localization;
using R2Trans.Windows.Models;

namespace R2Trans.Windows.Services;

public static class HotKeyValidator
{
    private static readonly HashSet<string> CommonWindowsShortcuts = new(StringComparer.OrdinalIgnoreCase)
    {
        "control+c",
        "control+v",
        "control+x",
        "control+z",
        "control+a",
        "control+s",
        "control+p",
        "alt+f4",
        "win+d",
        "win+l",
        "win+r",
        "win+tab"
    };

    public static void Validate(string value)
    {
        var normalized = HotKeyParser.NormalizeString(value);
        var modifierCount = normalized.Split('+').Count(part =>
            part is "control" or "alt" or "shift" or "win");

        if (modifierCount < 2)
        {
            throw new R2TransException(AppText.Text(TextKey.ShortcutNeedsMoreModifiers));
        }

        if (CommonWindowsShortcuts.Contains(normalized))
        {
            throw new R2TransException(AppText.Text(TextKey.ShortcutConflictsWithWindows));
        }
    }
}
