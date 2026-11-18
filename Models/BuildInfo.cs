namespace CicdLight;

public record BuildInfo(
    int Id, string Project, string Definition, string Branch, string BuildNumber,
    string Status, string? Result, string RequestedFor, string Url, DateTimeOffset? Time = null, string ConnectionId = "",
    DateTimeOffset? StartTime = null, DateTimeOffset? FinishTime = null, TimeSpan? TypicalDuration = null, int DurationSamples = 0)
{
    public bool IsRunning => Status is "inProgress" or "notStarted";
    public bool IsFailed => Result is "failed" or "partiallySucceeded";
    public bool IsSucceeded => Result == "succeeded";
    public TimeSpan? Duration => StartTime.HasValue && FinishTime.HasValue && FinishTime > StartTime ? FinishTime - StartTime : null;

    // Null until the build has really started (queued builds have no meaningful elapsed time).
    public TimeSpan? Elapsed(DateTimeOffset now) => Status == "inProgress" && StartTime.HasValue ? now - StartTime.Value : null;

    public TimeSpan? Remaining(DateTimeOffset now) => TypicalDuration.HasValue && Elapsed(now) is { } elapsed ? TypicalDuration.Value - elapsed : null;
    public string Key => string.IsNullOrEmpty(ConnectionId)
        ? $"B:{Project}/{Definition}"
        : $"B:{ConnectionId}:{Project}/{Definition}";
}
