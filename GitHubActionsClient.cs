using System.Net.Http.Headers;
using System.Text.Json;

namespace CicdLight;

public sealed class GitHubActionsClient : IIntegrationClient
{
    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(30) };
    private readonly IntegrationConnection _connection;

    public GitHubActionsClient(IntegrationConnection connection)
    {
        _connection = connection;
        _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("CicdLight/1.0");
        _http.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
        if (!string.IsNullOrWhiteSpace(connection.Credential))
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", connection.Credential);
    }

    public async Task<Snapshot> FetchAsync(CancellationToken ct)
    {
        try
        {
            var (owner, repository) = ParseRepository(_connection.Repository);
            var currentUser = string.IsNullOrWhiteSpace(_connection.Credential)
                ? null
                : await GetAuthenticatedUserAsync(ct);
            var buildsTask = GetRunsAsync(owner, repository, ct);
            var pullRequestsTask = GetPullRequestsAsync(owner, repository, currentUser, ct);
            await Task.WhenAll(buildsTask, pullRequestsTask);
            return new Snapshot(buildsTask.Result, pullRequestsTask.Result, null);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex) { return Snapshot.Empty($"{_connection.DisplayName}: {ex.Message}"); }
    }

    private async Task<string?> GetAuthenticatedUserAsync(CancellationToken ct)
    {
        var json = await GetJsonAsync("https://api.github.com/user", ct);
        return json.GetProperty("login").GetString();
    }

    private async Task<List<PullRequestInfo>> GetPullRequestsAsync(
        string owner, string repository, string? currentUser, CancellationToken ct)
    {
        var url = $"https://api.github.com/repos/{Uri.EscapeDataString(owner)}/{Uri.EscapeDataString(repository)}/pulls?state=open&per_page=100";
        var json = await GetJsonAsync(url, ct);
        var fullRepository = $"{owner}/{repository}";
        return json.EnumerateArray().Select(pullRequest =>
        {
            var author = pullRequest.GetProperty("user").GetProperty("login").GetString() ?? "";
            var isMine = currentUser != null && author.Equals(currentUser, StringComparison.OrdinalIgnoreCase);
            var needsMyReview = currentUser != null &&
                pullRequest.GetProperty("requested_reviewers").EnumerateArray()
                    .Any(reviewer => string.Equals(
                        reviewer.GetProperty("login").GetString(), currentUser, StringComparison.OrdinalIgnoreCase));
            return new PullRequestInfo(
                pullRequest.GetProperty("number").GetInt32(),
                fullRepository,
                fullRepository,
                pullRequest.GetProperty("title").GetString() ?? "",
                author,
                isMine,
                needsMyReview,
                pullRequest.GetProperty("html_url").GetString() ?? "",
                ConnectionId: _connection.Id);
        }).ToList();
    }

    private async Task<List<BuildInfo>> GetRunsAsync(string owner, string repository, CancellationToken ct)
    {
        var fullRepository = $"{owner}/{repository}";
        var url = $"https://api.github.com/repos/{Uri.EscapeDataString(owner)}/{Uri.EscapeDataString(repository)}/actions/runs?per_page=100";
        var json = await GetJsonAsync(url, ct);
        return json.GetProperty("workflow_runs").EnumerateArray()
            .Select(run => ToBuild(fullRepository, run))
            .GroupBy(build => build.Key, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.OrderByDescending(build => build.Time ?? DateTimeOffset.MinValue).First())
            .ToList();
    }

    private async Task<JsonElement> GetJsonAsync(string url, CancellationToken ct)
    {
        using var response = await _http.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        return document.RootElement.Clone();
    }

    private BuildInfo ToBuild(string repository, JsonElement run)
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
            time,
            _connection.Id);
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