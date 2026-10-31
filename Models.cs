namespace CicdLight;

public enum Health { Unknown, Ok, Running, Warning, Failed }
public enum IntegrationProvider { AzureDevOps, GitHub }

public interface IIntegrationClient : IDisposable
{
    Task<Snapshot> FetchAsync(CancellationToken cancellationToken);
}

public sealed class IntegrationConnection
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public IntegrationProvider Provider { get; set; }
    public string Repository { get; set; } = "";
    public string Organization { get; set; } = "";
    public string Project { get; set; } = "";
    public string EncryptedCredential { get; set; } = "";
    public List<string> SelectedItems { get; set; } = [];
    public bool HasSelection { get; set; }
    [System.Text.Json.Serialization.JsonIgnore]
    public string? LastError { get; set; }

    [System.Text.Json.Serialization.JsonIgnore]
    public string Credential
    {
        get
        {
            if (string.IsNullOrEmpty(EncryptedCredential)) return "";
            try
            {
                var protectedBytes = Convert.FromBase64String(EncryptedCredential);
                return System.Text.Encoding.UTF8.GetString(System.Security.Cryptography.ProtectedData.Unprotect(
                    protectedBytes, null, System.Security.Cryptography.DataProtectionScope.CurrentUser));
            }
            catch (System.Security.Cryptography.CryptographicException) { return ""; }
        }
        set => EncryptedCredential = string.IsNullOrEmpty(value) ? "" : Convert.ToBase64String(
            System.Security.Cryptography.ProtectedData.Protect(System.Text.Encoding.UTF8.GetBytes(value), null,
                System.Security.Cryptography.DataProtectionScope.CurrentUser));
    }

    [System.Text.Json.Serialization.JsonIgnore]
    public string DisplayName => Provider == IntegrationProvider.GitHub
        ? Repository
        : string.IsNullOrWhiteSpace(Project) ? Organization : $"{Organization}/{Project}";

    [System.Text.Json.Serialization.JsonIgnore]
    public string OrganizationUrl
    {
        get
        {
            var value = Organization.Trim().TrimEnd('/');
            return value.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? value : $"https://dev.azure.com/{value}";
        }
    }
}

public sealed class ProjectNotificationSettings
{
    public bool BuildFailed { get; set; } = true;
    public bool BuildFixed { get; set; } = true;
    public bool ReviewRequested { get; set; } = true;
}

public record BuildInfo(
    int Id, string Project, string Definition, string Branch, string BuildNumber,
    string Status, string? Result, string RequestedFor, string Url, DateTimeOffset? Time = null, string ConnectionId = "")
{
    public bool IsRunning => Status is "inProgress" or "notStarted";
    public bool IsFailed => Result is "failed" or "partiallySucceeded";
    public bool IsSucceeded => Result == "succeeded";
    public string Key => string.IsNullOrEmpty(ConnectionId)
        ? $"B:{Project}/{Definition}"
        : $"B:{ConnectionId}:{Project}/{Definition}";
}

public record ReleaseInfo(
    string Project, string Definition, string ReleaseName, bool IsFailed, bool IsRunning, string Url, string ConnectionId = "")
{
    public bool IsSucceeded => !IsFailed && !IsRunning;
    public string Key => string.IsNullOrEmpty(ConnectionId)
        ? $"R:{Project}/{Definition}"
        : $"R:{ConnectionId}:{Project}/{Definition}";
}

public record PullRequestInfo(
    int Id, string Project, string Repository, string Title, string Author,
    bool IsMine, bool NeedsMyReview, string Url,
    string RepositoryId = "", bool HasUnresolvedComments = false, string ConnectionId = "");

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
