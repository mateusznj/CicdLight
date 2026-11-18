namespace CicdLight;

public record Snapshot(
    IReadOnlyList<BuildInfo> Builds,
    IReadOnlyList<PullRequestInfo> PullRequests,
    string? Error,
    IReadOnlyList<ReleaseInfo>? ReleaseList = null,
    IReadOnlyList<string>? ProjectList = null,
    bool AlertMyPrComments = true,
    bool AlertOtherPrComments = false)
{
    public IReadOnlyList<ReleaseInfo> Releases => ReleaseList ?? [];
    public int MyPullRequestsToRevise => PullRequests.Count(p => p.IsMine && p.HasUnresolvedComments);
    public int OtherPullRequestsToRevise => PullRequests.Count(p => !p.IsMine && p.HasUnresolvedComments);

    // Only the PR groups the user chose to be alerted about.
    public int PullRequestsToRevise =>
        (AlertMyPrComments ? MyPullRequestsToRevise : 0) + (AlertOtherPrComments ? OtherPullRequestsToRevise : 0);

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
        PullRequests.Any(p => p.NeedsMyReview) || PullRequestsToRevise > 0 ? Health.Warning :
        Health.Ok;
}
