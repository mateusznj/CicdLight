using System.Reflection;
using System.Text.Json;

namespace CicdLight;

public static class UpdateChecker
{
    public static async Task<UpdateCheckResult> CheckAsync(AppSettings settings, CancellationToken cancellationToken)
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd($"CicdLight/{GetCurrentVersion()}");
        client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");

        using var response = await client.GetAsync(settings.UpdateLatestReleaseApiUrl, cancellationToken);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        var release = document.RootElement;
        var tag = release.GetProperty("tag_name").GetString() ?? "";
        var releaseUrl = release.GetProperty("html_url").GetString() ?? settings.UpdateReleasesUrl;
        var currentText = GetCurrentVersion();
        var current = ParseVersion(currentText);
        var latest = ParseVersion(tag);
        if (current == null || latest == null)
        {
            throw new InvalidOperationException(Strings.Get("Update_InvalidVersion"));
        }

        return new UpdateCheckResult(
            currentText,
            tag,
            releaseUrl,
            latest > current);
    }

    public static string GetCurrentVersion()
    {
        var value = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? Assembly.GetExecutingAssembly().GetName().Version?.ToString()
            ?? "0.0.0";
        return ParseVersion(value)?.ToString() ?? value;
    }

    public static Version? ParseVersion(string value)
    {
        var normalized = value.Trim().TrimStart('v', 'V');
        var suffix = normalized.IndexOfAny(['-', '+']);
        if (suffix >= 0)
        {
            normalized = normalized[..suffix];
        }
        return Version.TryParse(normalized, out var version) ? version : null;
    }
}
