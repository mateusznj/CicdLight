namespace CicdLight;

public record ReleaseInfo(
    string Project, string Definition, string ReleaseName, bool IsFailed, bool IsRunning, string Url, string ConnectionId = "")
{
    public bool IsSucceeded => !IsFailed && !IsRunning;
    public string Key => string.IsNullOrEmpty(ConnectionId)
        ? $"R:{Project}/{Definition}"
        : $"R:{ConnectionId}:{Project}/{Definition}";
}
