namespace CicdLight;

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
