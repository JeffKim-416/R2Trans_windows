using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using R2Trans.Windows.Models;

namespace R2Trans.Windows.Services;

public sealed class SettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string settingsPath;

    public SettingsStore()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var settingsDirectory = Path.Combine(appData, "R2Trans");
        Directory.CreateDirectory(settingsDirectory);
        settingsPath = Path.Combine(settingsDirectory, "settings.json");
    }

    public AppSettings Current { get; private set; } = new();

    public void Load()
    {
        if (!File.Exists(settingsPath))
        {
            Current = new AppSettings();
            return;
        }

        try
        {
            var json = File.ReadAllText(settingsPath);
            Current = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
            Normalize();
        }
        catch
        {
            Current = new AppSettings();
        }
    }

    public void Save()
    {
        Normalize();
        var json = JsonSerializer.Serialize(Current, JsonOptions);
        File.WriteAllText(settingsPath, json);
    }

    private void Normalize()
    {
        Current.SourceLanguageCode = SupportedLanguage.NormalizeCode(Current.SourceLanguageCode, SupportedLanguage.DefaultSourceCode);
        Current.TargetLanguageCode = SupportedLanguage.NormalizeCode(Current.TargetLanguageCode, SupportedLanguage.DefaultTargetCode);
        Current.Model = SupportedModel.All.Any(model => model.Id == Current.Model)
            ? Current.Model
            : SupportedModel.DefaultId;

        if (Current.HotKeyString.Contains("option", StringComparison.OrdinalIgnoreCase))
        {
            Current.HotKeyString = Current.HotKeyString.Replace("option", "alt", StringComparison.OrdinalIgnoreCase);
        }
    }
}
