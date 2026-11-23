namespace CicdLight;

// Fake data source used only when CICDLIGHT_DEMO=1; lets us take screenshots without real credentials.
public sealed class DemoIntegrationClient : IIntegrationClient
{
    public const string EnvironmentVariable = "CICDLIGHT_DEMO";

    public static bool IsEnabled => Environment.GetEnvironmentVariable(EnvironmentVariable) == "1";

    public Task<Snapshot> FetchAsync(CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.Now;
        var eta = TimeSpan.FromMinutes(9);
        List<BuildInfo> builds =
        [
            new(1, "SampleApp", "DEV - SampleApp", "DEV", "20261123.4", "inProgress", null, "Demo User", "https://example.com/1",
                now, StartTime: now.AddMinutes(-4), TypicalDuration: eta, DurationSamples: 8),
            new(2, "WebPortal", "RELEASE - WebPortal", "RELEASE", "20261123.3", "completed", "failed", "Demo User", "https://example.com/2",
                now.AddMinutes(-12), StartTime: now.AddMinutes(-20), FinishTime: now.AddMinutes(-12)),
            new(3, "SampleApp", "RELEASE - SampleApp", "RELEASE", "20261123.2", "completed", "succeeded", "Demo User", "https://example.com/3",
                now.AddMinutes(-35), StartTime: now.AddMinutes(-44), FinishTime: now.AddMinutes(-35)),
            new(4, "WebPortal", "DEV - WebPortal", "DEV", "20261123.1", "completed", "succeeded", "Demo User", "https://example.com/4",
                now.AddHours(-2), StartTime: now.AddHours(-2).AddMinutes(-8), FinishTime: now.AddHours(-2)),
            new(5, "SampleApp", "PROD - SampleApp", "PROD", "20261122.9", "completed", "succeeded", "Demo User", "https://example.com/5",
                now.AddDays(-1), StartTime: now.AddDays(-1).AddMinutes(-10), FinishTime: now.AddDays(-1))
        ];
        List<PullRequestInfo> pullRequests =
        [
            new(101, "SampleApp", "SampleApp", "Add retry policy to uploads", "Demo Reviewer", false, true, "https://example.com/pr/101", UnresolvedCommentCount: 2),
            new(102, "WebPortal", "WebPortal", "Fix pagination on orders list", "Demo User", true, false, "https://example.com/pr/102", UnresolvedCommentCount: 3),
            new(103, "SampleApp", "SampleApp", "Update build pipeline templates", "Demo Colleague", false, false, "https://example.com/pr/103")
        ];
        List<ReleaseInfo> releases =
        [
            new("SampleApp", "RELEASE SampleApp", "2026.11.1 · RELEASE_1", false, false, "https://example.com/r/1"),
            new("SampleApp", "DEVELOPMENT SampleApp", "2026.11.1 · v1.2.0", false, false, "https://example.com/r/2")
        ];
        return Task.FromResult(new Snapshot(builds, pullRequests, null, releases));
    }

    public void Dispose()
    {
    }
}
