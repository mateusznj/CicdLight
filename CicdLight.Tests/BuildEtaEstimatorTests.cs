namespace CicdLight.Tests;

public sealed class BuildEtaEstimatorTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 18, 12, 0, 0, TimeSpan.Zero);

    private static BuildInfo Completed(int id, int minutes, string branch = "main", string definition = "App") =>
        new(id, "P", definition, branch, id.ToString(), "completed", "succeeded", "u", "", Now.AddHours(-id),
            StartTime: Now.AddHours(-id).AddMinutes(-minutes), FinishTime: Now.AddHours(-id));

    private static BuildInfo Running(string branch = "main", string definition = "App", int startedMinutesAgo = 4) =>
        new(100, "P", definition, branch, "100", "inProgress", null, "u", "", Now.AddMinutes(-startedMinutesAgo),
            StartTime: Now.AddMinutes(-startedMinutesAgo));

    [Fact]
    public void Annotate_UsesMedianOfSuccessfulRuns()
    {
        var builds = new List<BuildInfo> { Running(), Completed(1, 10), Completed(2, 20), Completed(3, 30), Completed(4, 100) };

        var running = BuildEtaEstimator.Annotate(builds)[0];

        Assert.Equal(TimeSpan.FromMinutes(25), running.TypicalDuration);
        Assert.Equal(4, running.DurationSamples);
    }

    [Fact]
    public void Annotate_WithTooFewSamples_LeavesEstimateEmpty()
    {
        var builds = new List<BuildInfo> { Running(), Completed(1, 10), Completed(2, 20) };

        Assert.Null(BuildEtaEstimator.Annotate(builds)[0].TypicalDuration);
    }

    [Fact]
    public void Annotate_PrefersSameBranchAndFallsBackToPipeline()
    {
        var other = new[] { Completed(1, 10, "dev"), Completed(2, 10, "dev"), Completed(3, 10, "dev") };
        var main = new[] { Completed(4, 40, "main"), Completed(5, 40, "main"), Completed(6, 40, "main") };

        Assert.Equal(TimeSpan.FromMinutes(40),
            BuildEtaEstimator.Annotate([Running("main"), .. other, .. main])[0].TypicalDuration);
        Assert.Equal(TimeSpan.FromMinutes(10),
            BuildEtaEstimator.Annotate([Running("feature"), .. other, Completed(7, 40, "main")])[0].TypicalDuration);
    }

    [Fact]
    public void Annotate_IgnoresFailedAndOtherPipelines()
    {
        var failed = Completed(1, 5) with { Result = "failed" };
        var builds = new List<BuildInfo> { Running(), failed, Completed(2, 5, definition: "Other"), Completed(3, 5), Completed(4, 5) };

        Assert.Null(BuildEtaEstimator.Annotate(builds)[0].TypicalDuration);
    }

    [Fact]
    public void Remaining_IsNegativeWhenBuildRunsLongerThanTypical()
    {
        var build = Running(startedMinutesAgo: 30) with { TypicalDuration = TimeSpan.FromMinutes(20) };

        Assert.Equal(TimeSpan.FromMinutes(-10), build.Remaining(Now));
    }

    [Fact]
    public void Remaining_IsNullForQueuedBuild()
    {
        var queued = Running() with { Status = "notStarted", TypicalDuration = TimeSpan.FromMinutes(20) };

        Assert.Null(queued.Remaining(Now));
    }
}
