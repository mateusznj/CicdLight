namespace CicdLight;

public sealed record UpdateCheckResult(
    string CurrentVersion,
    string LatestVersion,
    string ReleaseUrl,
    bool UpdateAvailable);
