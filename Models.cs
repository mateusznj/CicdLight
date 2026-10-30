namespace CicdLight;

public enum Health { Unknown, Ok, Running, Warning, Failed }

public record BuildInfo(
    int Id, string Project, string Definition, string Branch, string BuildNumber,
    string Status, string? Result, string RequestedFor, string Url, DateTimeOffset? Time = null)
{
    public bool IsRunning => Status is "inProgress" or "notStarted";
    public bool IsFailed => Result is "failed" or "partiallySucceeded";
    public bool IsSucceeded => Result == "succeeded";
    public string Key => $"B:{Project}/{Definition}";
}

public record ReleaseInfo(
    string Project, string Definition, string ReleaseName, bool IsFailed, bool IsRunning, string Url)
{
    public bool IsSucceeded => !IsFailed && !IsRunning;
    public string Key => $"R:{Project}/{Definition}";
}

public record PullRequestInfo(
    int Id, string Project, string Repository, string Title, string Author,
    bool IsMine, bool NeedsMyReview, string Url,
    string RepositoryId = "", bool HasUnresolvedComments = false);

public record Snapshot(
    IReadOnlyList<BuildInfo> Builds,
    IReadOnlyList<PullRequestInfo> PullRequests,
    string? Error,
    IReadOnlyList<ReleaseInfo>? ReleaseList = null)
{
    public IReadOnlyList<ReleaseInfo> Releases => ReleaseList ?? [];
    public int PullRequestsToRevise => PullRequests.Count(p => p.HasUnresolvedComments);

    public static Snapshot Empty(string? error = null) => new([], [], error);

    // Empty watched set = show everything.
    public Snapshot Filter(ICollection<string> watched) => watched.Count == 0 ? this : this with
    {
        Builds = Builds.Where(b => watched.Contains(b.Key)).ToList(),
        ReleaseList = Releases.Where(r => watched.Contains(r.Key)).ToList()
    };

    public Health Health =>
        Error != null ? Health.Unknown :
        Builds.Any(b => b.IsFailed && !b.IsRunning) || Releases.Any(r => r.IsFailed && !r.IsRunning) ? Health.Failed :
        Builds.Any(b => b.IsRunning) || Releases.Any(r => r.IsRunning) ? Health.Running :
        PullRequests.Any(p => p.NeedsMyReview) ? Health.Warning :
        Health.Ok;
}
