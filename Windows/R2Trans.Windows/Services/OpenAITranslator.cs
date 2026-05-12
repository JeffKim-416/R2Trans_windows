using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using R2Trans.Windows.Localization;
using R2Trans.Windows.Models;

namespace R2Trans.Windows.Services;

public sealed class OpenAITranslator
{
    private readonly SettingsStore settingsStore;
    private readonly SecureCredentialStore credentialStore;
    private readonly HttpClient httpClient = new();

    public OpenAITranslator(SettingsStore settingsStore, SecureCredentialStore credentialStore)
    {
        this.settingsStore = settingsStore;
        this.credentialStore = credentialStore;
    }

    public async Task<string> TranslateAsync(string text, CancellationToken cancellationToken = default)
    {
        var apiKey = credentialStore.LoadApiKey().Trim();
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new R2TransException(AppText.Text(TextKey.APIKeyMissing));
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/responses");
        request.Headers.Authorization = new("Bearer", apiKey);
        request.Content = JsonContent.Create(new ResponsesRequest(
            settingsStore.Current.Model,
            MakeInstructions(),
            text,
            2048
        ));

        HttpResponseMessage response;
        try
        {
            response = await httpClient.SendAsync(request, cancellationToken);
        }
        catch (HttpRequestException exception)
        {
            throw new R2TransException($"{AppText.Text(TextKey.NetworkError)}\n\n{exception.Message}", exception);
        }

        var responseText = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new R2TransException(FriendlyErrorMessage(response.StatusCode, ExtractErrorMessage(responseText)));
        }

        var translated = ExtractTextOutput(responseText).Trim();
        if (string.IsNullOrWhiteSpace(translated))
        {
            throw new R2TransException(AppText.Text(TextKey.ResponseMissing));
        }

        return translated;
    }

    private string MakeInstructions() => settingsStore.Current.WorkMode switch
    {
        WorkMode.Rewrite => MakeRewriteInstructions(),
        _ => MakeTranslationInstructions()
    };

    private string MakeTranslationInstructions()
    {
        var settings = settingsStore.Current;
        string modeInstruction;

        if (settings.AutoDetectEnabled)
        {
            var firstLanguage = SupportedLanguage.EnglishName(settings.AutoDetectPair.FirstLanguageCode());
            var secondLanguage = SupportedLanguage.EnglishName(settings.AutoDetectPair.SecondLanguageCode());
            modeInstruction = $"""
                Detect whether the user's text is primarily {firstLanguage} or {secondLanguage}.
                If it is primarily {firstLanguage}, translate it to {secondLanguage}.
                If it is primarily {secondLanguage}, translate it to {firstLanguage}.
                If both languages appear, choose the predominant language and translate to the other language in this pair.
                """;
        }
        else
        {
            var sourceLanguage = SupportedLanguage.EnglishName(settings.SourceLanguageCode);
            var targetLanguage = SupportedLanguage.EnglishName(settings.TargetLanguageCode);
            modeInstruction = $"Translate the user's text from {sourceLanguage} to {targetLanguage}.";
        }

        return $"""
            You are a precise translation engine.
            {modeInstruction}
            {StyleInstruction(settings.TranslationStyle)}
            Return only the translated text.
            Preserve line breaks, list structure, numbers, names, URLs, code, and markdown when possible.
            Do not add explanations or quotation marks.
            """;
    }

    private string MakeRewriteInstructions()
    {
        return $"""
            You are a precise rewriting engine.
            Rewrite the user's text in the same language as the input. Do not translate it into another language.
            Improve rough, awkward, or unclear wording while preserving the original meaning, intent, facts, names, numbers, URLs, code, markdown, and line breaks when possible.
            {StyleInstruction(settingsStore.Current.TranslationStyle)}
            Return only the rewritten text.
            Do not add explanations or quotation marks.
            """;
    }

    private static string StyleInstruction(TranslationStyle style) => style switch
    {
        TranslationStyle.Formal => "Use formal, polished, professional wording.",
        TranslationStyle.Polite => "Use courteous, respectful, and considerate wording.",
        TranslationStyle.Groveling => "Use very humble, apologetic, and deferential wording without adding new substantive meaning.",
        TranslationStyle.Nyang => """
            If the target output language is Korean, use a cute Korean nyangnyang style with endings like '냥' or '다냥' where natural.
            Do not overuse it, and preserve the original meaning.
            If the target output language is not Korean, use the Natural style instead.
            """,
        _ => "Make the translation sound natural and fluent."
    };

    private static string ExtractTextOutput(string json)
    {
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        if (root.TryGetProperty("output_text", out var outputText) && outputText.ValueKind == JsonValueKind.String)
        {
            return outputText.GetString() ?? string.Empty;
        }

        if (!root.TryGetProperty("output", out var output) || output.ValueKind != JsonValueKind.Array)
        {
            return string.Empty;
        }

        var parts = new List<string>();
        foreach (var outputItem in output.EnumerateArray())
        {
            if (!outputItem.TryGetProperty("content", out var content) || content.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var contentItem in content.EnumerateArray())
            {
                if (contentItem.TryGetProperty("text", out var textElement) && textElement.ValueKind == JsonValueKind.String)
                {
                    parts.Add(textElement.GetString() ?? string.Empty);
                }
            }
        }

        return string.Concat(parts);
    }

    private static string ExtractErrorMessage(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.TryGetProperty("error", out var error)
                && error.TryGetProperty("message", out var message)
                && message.ValueKind == JsonValueKind.String)
            {
                return message.GetString() ?? json;
            }
        }
        catch (JsonException)
        {
            return json;
        }

        return json;
    }

    private static string FriendlyErrorMessage(HttpStatusCode statusCode, string apiMessage)
    {
        var numericStatusCode = (int)statusCode;
        var friendly = numericStatusCode switch
        {
            401 or 403 => AppText.Text(TextKey.OpenAIUnauthorized),
            429 => AppText.Text(TextKey.OpenAIRateLimited),
            >= 500 => AppText.Text(TextKey.OpenAITemporaryFailure),
            _ => $"OpenAI API error ({numericStatusCode})."
        };

        return $"{friendly}\n\n{apiMessage}";
    }

    private sealed record ResponsesRequest(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("instructions")] string Instructions,
        [property: JsonPropertyName("input")] string Input,
        [property: JsonPropertyName("max_output_tokens")] int MaxOutputTokens);
}
