using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json.Serialization;
using R2Trans.Windows.Models;

namespace R2Trans.Windows.Services;

public sealed class UpdateChecker : IDisposable
{
    private const string LatestReleaseUrl = "https://api.github.com/repos/JeffKim-416/R2Trans_windows/releases/latest";
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(8);
    private readonly HttpClient httpClient = new()
    {
        Timeout = RequestTimeout
    };

    public async Task<UpdateInfo?> CheckAsync(CancellationToken cancellationToken = default)
    {
        var currentVersionText = CurrentVersionText();
        if (!TryParseVersion(currentVersionText, out var currentVersion))
        {
            return null;
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, LatestReleaseUrl);
        request.Headers.UserAgent.ParseAdd($"R2Trans/{currentVersionText}");
        request.Headers.Accept.ParseAdd("application/vnd.github+json");

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var release = await response.Content.ReadFromJsonAsync<GitHubRelease>(cancellationToken);

        if (release is null || string.IsNullOrWhiteSpace(release.TagName))
        {
            return null;
        }

        if (!TryParseVersion(release.TagName, out var latestVersion) || latestVersion <= currentVersion)
        {
            return null;
        }

        var downloadUrl = release.Assets
            .FirstOrDefault(asset => asset.Name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            ?.BrowserDownloadUrl;

        return new UpdateInfo(
            currentVersionText,
            release.TagName,
            string.IsNullOrWhiteSpace(downloadUrl) ? release.HtmlUrl : downloadUrl,
            release.HtmlUrl);
    }

    public static void OpenUpdateUrl(UpdateInfo updateInfo)
    {
        Process.Start(new ProcessStartInfo(updateInfo.DownloadUrl)
        {
            UseShellExecute = true
        });
    }

    private static string CurrentVersionText()
    {
        var assembly = typeof(UpdateChecker).Assembly;
        return assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? assembly.GetName().Version?.ToString(3)
            ?? "0.0.0";
    }

    private static bool TryParseVersion(string value, out Version version)
    {
        version = new Version(0, 0, 0);
        var normalized = value.Trim();

        if (normalized.StartsWith('v') || normalized.StartsWith('V'))
        {
            normalized = normalized[1..];
        }

        var versionText = new string(normalized
            .TakeWhile(character => char.IsDigit(character) || character == '.')
            .ToArray())
            .Trim('.');

        if (string.IsNullOrWhiteSpace(versionText))
        {
            return false;
        }

        var parts = versionText.Split('.', StringSplitOptions.RemoveEmptyEntries).ToList();
        while (parts.Count < 3)
        {
            parts.Add("0");
        }

        return Version.TryParse(string.Join('.', parts.Take(4)), out version!);
    }

    public void Dispose()
    {
        httpClient.Dispose();
    }

    private sealed record GitHubRelease(
        [property: JsonPropertyName("tag_name")] string TagName,
        [property: JsonPropertyName("html_url")] string HtmlUrl,
        [property: JsonPropertyName("assets")] IReadOnlyList<GitHubReleaseAsset> Assets);

    private sealed record GitHubReleaseAsset(
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("browser_download_url")] string BrowserDownloadUrl);
}

public sealed record UpdateInfo(
    string CurrentVersion,
    string LatestVersion,
    string DownloadUrl,
    string ReleaseUrl);
