using System.Net.Http.Headers;
using System.Text.Json;

namespace CicdLight;

public sealed class GitHubActionsClient : IDisposable
{
    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(30) };
    private readonly AppSettings _settings;

    public GitHubActionsClient(AppSettings settings)
    {
        _settings = settings;
        _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("CicdLight/1.0");
        _http.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
        if (!string.IsNullOrWhiteSpace(settings.GitHubToken))
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", settings.GitHubToken);
    }

    public async Task<Snapshot> FetchAsync(CancellationToken ct)
    {
        var requests = _settings.GitHubRepositoryList.Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(async repository =>
            {
                try
                {
                    return (Builds: await GetRunsAsync(repository, ct), Error: (string?)null);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
                catch (Exception ex)
                {
                    return (Builds: new List<BuildInfo>(), Error: $"{repository}: {ex.Message}");
                }
            });
        try
        {
            var results = await Task.WhenAll(requests);
            var errors = results.Where(result => result.Error != null).Select(result => result.Error!).ToList();
            return new Snapshot(results.SelectMany(result => result.Builds).ToList(), [],
                errors.Count == 0 ? null : string.Join(" | ", errors));
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex) { return Snapshot.Empty(ex.Message); }
    }

    private async Task<List<BuildInfo>> GetRunsAsync(string repository, CancellationToken ct)
    {
        var (owner, name) = ParseRepository(repository);
        var url = $"https://api.github.com/repos/{Uri.EscapeDataString(owner)}/{Uri.EscapeDataString(name)}/actions/runs?per_page=100";
        using var response = await _http.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        return document.RootElement.GetProperty("workflow_runs").EnumerateArray()
            .Select(run => ToBuild(repository, run))
            .GroupBy(build => build.Key, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.OrderByDescending(build => build.Time ?? DateTimeOffset.MinValue).First())
            .ToList();
    }

    private static BuildInfo ToBuild(string repository, JsonElement run)
    {
        var status = run.GetProperty("status").GetString() switch
        {
            "queued" or "waiting" or "requested" => "notStarted",
            "in_progress" => "inProgress",
            _ => "completed"
        };
        var result = run.TryGetProperty("conclusion", out var conclusion) ? conclusion.GetString() switch
        {
            "success" => "succeeded",
            "failure" or "timed_out" or "action_required" => "failed",
            "cancelled" => "canceled",
            var other => other
        } : null;
        var actor = run.TryGetProperty("actor", out var actorElement) && actorElement.TryGetProperty("login", out var login)
            ? login.GetString() ?? ""
            : "";
        var time = run.TryGetProperty("updated_at", out var updatedAt) && DateTimeOffset.TryParse(updatedAt.GetString(), out var parsedTime)
            ? parsedTime
            : (DateTimeOffset?)null;

        return new BuildInfo(
            run.GetProperty("run_number").GetInt32(),
            repository,
            run.GetProperty("name").GetString() ?? "GitHub Actions",
            run.TryGetProperty("head_branch", out var branch) ? branch.GetString() ?? "" : "",
            run.GetProperty("run_number").GetInt32().ToString(),
            status,
            result,
            actor,
            run.GetProperty("html_url").GetString() ?? $"https://github.com/{repository}/actions",
            time);
    }

    private static (string Owner, string Repository) ParseRepository(string value)
    {
        var input = value.Trim();
        if (Uri.TryCreate(input, UriKind.Absolute, out var uri))
        {
            if (!uri.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase))
                throw new FormatException($"'{value}' is not a github.com repository URL.");
            input = uri.AbsolutePath.Trim('/');
        }
        else if (input.StartsWith("github.com/", StringComparison.OrdinalIgnoreCase))
        {
            input = input["github.com/".Length..].Trim('/');
        }

        input = input.EndsWith(".git", StringComparison.OrdinalIgnoreCase) ? input[..^4] : input;
        var parts = input.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length != 2 || parts.Any(part => part is "." or ".."))
            throw new FormatException($"Use 'owner/repository' or a GitHub repository URL: '{value}'.");
        return (parts[0], parts[1]);
    }

    public static string GetRepositoryUrl(string repository)
    {
        var (owner, name) = ParseRepository(repository);
        return $"https://github.com/{Uri.EscapeDataString(owner)}/{Uri.EscapeDataString(name)}";
    }

    public void Dispose() => _http.Dispose();
}