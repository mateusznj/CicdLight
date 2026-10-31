using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace CicdLight;

public sealed class AzureDevOpsClient : IIntegrationClient
{
    private const string Api = "api-version=7.1";
    private readonly HttpClient _http;
    private readonly AppSettings _s;
    private readonly IntegrationConnection _connection;
    private string? _userId;

    public AzureDevOpsClient(AppSettings settings, IntegrationConnection connection)
    {
        _s = settings;
        _connection = connection;
        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes(":" + connection.Credential)));
    }

    public async Task<Snapshot> FetchAsync(CancellationToken ct)
    {
        try
        {
            _userId ??= (await GetAsync($"{_connection.OrganizationUrl}/_apis/connectionData", ct))
                .GetProperty("authenticatedUser").GetProperty("id").GetString();

            var projects = string.IsNullOrWhiteSpace(_connection.Project)
                ? await GetProjectsAsync(ct)
                : [_connection.Project];

            var buildTasks = projects.Select(p => GetBuildsAsync(p, ct)).ToList();
            var releaseTasks = projects.Select(p => GetReleasesAsync(p, ct)).ToList();
            var prTask = GetPullRequestsAsync(ct);
            await Task.WhenAll(buildTasks.Cast<Task>().Concat(releaseTasks).Append(prTask));

            return new Snapshot(buildTasks.SelectMany(t => t.Result).ToList(), prTask.Result, null,
                releaseTasks.SelectMany(t => t.Result).ToList());
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex)
        {
            return Snapshot.Empty(ex.Message);
        }
    }

    private async Task<string[]> GetProjectsAsync(CancellationToken ct)
    {
        var json = await GetAsync($"{_connection.OrganizationUrl}/_apis/projects?$top=100&{Api}", ct);
        return json.GetProperty("value").EnumerateArray().Select(p => p.GetProperty("name").GetString()!).ToArray();
    }

    private static DateTimeOffset? ParseTime(System.Text.Json.JsonElement b)
    {
        foreach (var name in new[] { "finishTime", "startTime", "queueTime" })
            if (b.TryGetProperty(name, out var p) && p.ValueKind == System.Text.Json.JsonValueKind.String
                && DateTimeOffset.TryParse(p.GetString(), out var t))
                return t;
        return null;
    }

    // Latest build per definition+branch, newest first.
    private async Task<List<BuildInfo>> GetBuildsAsync(string project, CancellationToken ct)
    {
        var url = $"{_connection.OrganizationUrl}/{Uri.EscapeDataString(project)}/_apis/build/builds" +
                  $"?$top=100&queryOrder=queueTimeDescending&{Api}";
        if (_s.OnlyMyBuilds) url += $"&requestedFor={_userId}";

        var json = await GetAsync(url, ct);
        return json.GetProperty("value").EnumerateArray()
            .Select(b => new BuildInfo(
                b.GetProperty("id").GetInt32(),
                project,
                b.GetProperty("definition").GetProperty("name").GetString() ?? "?",
                (b.GetProperty("sourceBranch").GetString() ?? "").Replace("refs/heads/", ""),
                b.GetProperty("buildNumber").GetString() ?? "",
                b.GetProperty("status").GetString() ?? "",
                b.TryGetProperty("result", out var r) ? r.GetString() : null,
                b.TryGetProperty("requestedFor", out var rf) ? rf.GetProperty("displayName").GetString() ?? "" : "",
                b.GetProperty("_links").GetProperty("web").GetProperty("href").GetString() ?? "",
                ParseTime(b), _connection.Id))
            .GroupBy(b => b.Key)
            .Select(g => g.First())
            .ToList();
    }

    // Latest deployment per release definition + environment, aggregated per definition.
    private async Task<List<ReleaseInfo>> GetReleasesAsync(string project, CancellationToken ct)
    {
        try
        {
            var vsrm = _connection.OrganizationUrl.Replace("https://dev.azure.com", "https://vsrm.dev.azure.com", StringComparison.OrdinalIgnoreCase);
            var json = await GetAsync($"{vsrm}/{Uri.EscapeDataString(project)}/_apis/release/deployments?$top=200&queryOrder=descending&{Api}", ct);

            var latest = json.GetProperty("value").EnumerateArray()
                .Select(d => new
                {
                    Definition = d.GetProperty("releaseDefinition").GetProperty("name").GetString() ?? "?",
                    Env = d.GetProperty("releaseEnvironment").GetProperty("name").GetString() ?? "",
                    Release = d.GetProperty("release").GetProperty("name").GetString() ?? "",
                    ReleaseId = d.GetProperty("release").GetProperty("id").GetInt32(),
                    Status = d.GetProperty("deploymentStatus").GetString() ?? ""
                })
                .GroupBy(d => (d.Definition, d.Env))
                .Select(g => g.First());

            return latest.GroupBy(d => d.Definition).Select(g =>
            {
                var top = g.OrderByDescending(d => d.ReleaseId).First();
                var failed = g.Any(d => d.Status is "failed" or "partiallySucceeded");
                var running = g.Any(d => d.Status == "inProgress");
                var url = $"{_connection.OrganizationUrl}/{Uri.EscapeDataString(project)}/_releaseProgress?releaseId={top.ReleaseId}&_a=release-pipeline-progress";
                return new ReleaseInfo(project, g.Key, top.Release, failed, running, url, _connection.Id);
            }).ToList();
        }
        catch (HttpRequestException) { return []; }   // project without Releases
    }

    private async Task<List<PullRequestInfo>> GetPullRequestsAsync(CancellationToken ct)
    {
        var baseUrl = $"{_connection.OrganizationUrl}/_apis/git/pullrequests?searchCriteria.status=active&$top=50&{Api}";
        var mineTask = GetAsync($"{baseUrl}&searchCriteria.creatorId={_userId}", ct);
        var reviewTask = GetAsync($"{baseUrl}&searchCriteria.reviewerId={_userId}", ct);
        await Task.WhenAll(mineTask, reviewTask);

        string[] filter = string.IsNullOrWhiteSpace(_connection.Project) ? [] : [_connection.Project];
        var result = new Dictionary<int, PullRequestInfo>();

        foreach (var pr in reviewTask.Result.GetProperty("value").EnumerateArray())
        {
            var info = ToPr(pr, isMine: false);
            if (!InScope(info, filter)) continue;
            var myVote = pr.GetProperty("reviewers").EnumerateArray()
                .Where(r => r.GetProperty("id").GetString() == _userId)
                .Select(r => r.GetProperty("vote").GetInt32())
                .FirstOrDefault();
            result[info.Id] = info with { NeedsMyReview = myVote == 0 };
        }

        foreach (var pr in mineTask.Result.GetProperty("value").EnumerateArray())
        {
            var info = ToPr(pr, isMine: true);
            if (InScope(info, filter)) result[info.Id] = info with { IsMine = true, NeedsMyReview = false };
        }

        var pullRequests = result.Values.OrderByDescending(p => p.Id).ToList();
        var visiblePullRequests = pullRequests.Where(p => p.NeedsMyReview).Take(5)
            .Concat(pullRequests.Where(p => p.IsMine).Take(3))
            .DistinctBy(p => p.Id)
            .ToList();
        var commentChecks = visiblePullRequests.Select(async p =>
            (p.Id, HasComments: await HasUnresolvedCommentsAsync(p, ct))).ToList();
        await Task.WhenAll(commentChecks);

        var prsWithComments = commentChecks.Where(t => t.Result.HasComments).Select(t => t.Result.Id).ToHashSet();
        return pullRequests.Select(p => prsWithComments.Contains(p.Id)
            ? p with { HasUnresolvedComments = true }
            : p).ToList();
    }

    private async Task<bool> HasUnresolvedCommentsAsync(PullRequestInfo pullRequest, CancellationToken ct)
    {
        var url = $"{_connection.OrganizationUrl}/{Uri.EscapeDataString(pullRequest.Project)}/_apis/git/repositories/" +
                  $"{Uri.EscapeDataString(pullRequest.RepositoryId)}/pullRequests/{pullRequest.Id}/threads?{Api}";
        try
        {
            var json = await GetAsync(url, ct);
            return json.GetProperty("value").EnumerateArray().Any(thread =>
                thread.TryGetProperty("status", out var status) && status.GetString() == "active" &&
                thread.TryGetProperty("comments", out var comments) && comments.EnumerateArray().Any(comment =>
                    (!comment.TryGetProperty("isDeleted", out var deleted) || !deleted.GetBoolean()) &&
                    (!comment.TryGetProperty("commentType", out var type) || type.GetString() != "system")));
        }
        catch (HttpRequestException) { return false; }
        catch (InvalidOperationException) { return false; }
    }

    private static bool InScope(PullRequestInfo p, string[] projects) =>
        projects.Length == 0 || projects.Contains(p.Project, StringComparer.OrdinalIgnoreCase);

    private PullRequestInfo ToPr(JsonElement pr, bool isMine)
    {
        var repo = pr.GetProperty("repository");
        var repoId = repo.GetProperty("id").GetString() ?? "";
        var project = repo.GetProperty("project").GetProperty("name").GetString() ?? "";
        var repoName = repo.GetProperty("name").GetString() ?? "";
        var id = pr.GetProperty("pullRequestId").GetInt32();
        var url = $"{_connection.OrganizationUrl}/{Uri.EscapeDataString(project)}/_git/{Uri.EscapeDataString(repoName)}/pullrequest/{id}";
        return new PullRequestInfo(id, project, repoName, pr.GetProperty("title").GetString() ?? "",
            pr.GetProperty("createdBy").GetProperty("displayName").GetString() ?? "", isMine, false, url, repoId, false, _connection.Id);
    }

    private async Task<JsonElement> GetAsync(string url, CancellationToken ct)
    {
        using var resp = await _http.GetAsync(url, ct);
        if (resp.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            throw new InvalidOperationException(Strings.Get("Error_Auth"));
        resp.EnsureSuccessStatusCode();
        var text = await resp.Content.ReadAsStringAsync(ct);
        if (text.TrimStart().StartsWith('<'))
            throw new InvalidOperationException(Strings.Get("Error_Html"));
        return JsonDocument.Parse(text).RootElement.Clone();
    }

    public void Dispose() => _http.Dispose();
}
