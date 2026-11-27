namespace CicdLight;

public sealed class ProjectNotificationSettings
{
    public bool BuildFailed { get; set; } = true;
    public bool BuildFixed { get; set; } = true;
    public bool ReviewRequested { get; set; } = true;
    public bool UnresolvedComments { get; set; } = true;
    public bool ShowPullRequests { get; set; } = true;

    // Opt-out list, so repositories created later are visible by default.
    public List<string> HiddenPullRequestRepositories { get; set; } = [];

    public bool IsRepositoryHidden(string repository) =>
        HiddenPullRequestRepositories.Contains(repository, StringComparer.OrdinalIgnoreCase);
}
