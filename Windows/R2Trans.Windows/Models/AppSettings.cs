namespace R2Trans.Windows.Models;

public sealed class AppSettings
{
    public string SourceLanguageCode { get; set; } = SupportedLanguage.DefaultSourceCode;
    public string TargetLanguageCode { get; set; } = SupportedLanguage.DefaultTargetCode;
    public AppLanguage AppLanguage { get; set; } = AppLanguage.English;
    public string HotKeyString { get; set; } = "control+alt+t";
    public string Model { get; set; } = SupportedModel.DefaultId;
    public bool AutoDetectEnabled { get; set; }
    public AutoDetectPair AutoDetectPair { get; set; } = AutoDetectPair.KoreanEnglish;
    public bool ConfirmBeforeReplace { get; set; }
    public TranslationStyle TranslationStyle { get; set; } = TranslationStyle.Natural;
    public bool ShowTrayIcon { get; set; } = true;
    public WorkMode WorkMode { get; set; } = WorkMode.Translation;

    public string LanguagePairDisplayName => AutoDetectEnabled
        ? $"Auto {AutoDetectPair.DisplayName()}"
        : $"{SupportedLanguage.DisplayName(SourceLanguageCode)}->{SupportedLanguage.DisplayName(TargetLanguageCode)}";
}

public sealed record SupportedLanguage(string Code, string Name)
{
    public const string DefaultSourceCode = "ko-KR";
    public const string DefaultTargetCode = "en-US";

    public string DisplayName => Code;

    public static IReadOnlyList<SupportedLanguage> All { get; } =
    [
        new("en-US", "English"),
        new("ko-KR", "Korean"),
        new("es-ES", "Spanish"),
        new("ja-JP", "Japanese"),
        new("zh-CN", "Chinese")
    ];

    public static string DisplayName(string code) => LanguageFor(code).DisplayName;

    public static string EnglishName(string code) => LanguageFor(code).Name;

    public static SupportedLanguage LanguageFor(string code)
    {
        var normalized = NormalizeCode(code, DefaultTargetCode);
        return All.FirstOrDefault(language => language.Code == normalized) ?? All[0];
    }

    public static string NormalizeCode(string code, string fallback)
    {
        var aliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = "en-US",
            ["en-us"] = "en-US",
            ["ko"] = "ko-KR",
            ["kr"] = "ko-KR",
            ["ko-kr"] = "ko-KR",
            ["es"] = "es-ES",
            ["sp"] = "es-ES",
            ["es-es"] = "es-ES",
            ["ja"] = "ja-JP",
            ["jp"] = "ja-JP",
            ["ja-jp"] = "ja-JP",
            ["zh"] = "zh-CN",
            ["cn"] = "zh-CN",
            ["zh-cn"] = "zh-CN"
        };

        var resolved = aliases.TryGetValue(code.Trim(), out var alias) ? alias : code.Trim();
        return All.Any(language => language.Code == resolved) ? resolved : fallback;
    }
}

public sealed record SupportedModel(string Id, string DisplayName)
{
    public const string DefaultId = "gpt-5.4-nano";

    public static IReadOnlyList<SupportedModel> All { get; } =
    [
        new("gpt-5.5", "GPT-5.5 - highest quality"),
        new("gpt-5.4", "GPT-5.4 - balanced"),
        new("gpt-5.3-codex", "GPT-5.3 Codex - specialized"),
        new("gpt-5.2", "GPT-5.2 - compatibility"),
        new("gpt-5.4-nano", "GPT-5.4 nano - lowest cost")
    ];

    public static string DisplayNameFor(string id) => All.FirstOrDefault(model => model.Id == id)?.DisplayName ?? id;
}

public enum AppLanguage
{
    English,
    Korean,
    Japanese,
    Chinese
}

public enum AutoDetectPair
{
    KoreanEnglish,
    KoreanJapanese
}

public static class AutoDetectPairExtensions
{
    public static string DisplayName(this AutoDetectPair pair) => pair switch
    {
        AutoDetectPair.KoreanJapanese => "ko-KR <-> ja-JP",
        _ => "ko-KR <-> en-US"
    };

    public static string FirstLanguageCode(this AutoDetectPair pair) => "ko-KR";

    public static string SecondLanguageCode(this AutoDetectPair pair) => pair switch
    {
        AutoDetectPair.KoreanJapanese => "ja-JP",
        _ => "en-US"
    };
}

public enum TranslationStyle
{
    Natural,
    Formal,
    Polite,
    Groveling,
    Nyang
}

public enum WorkMode
{
    Translation,
    Rewrite
}
