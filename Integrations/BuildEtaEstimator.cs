namespace CicdLight;

public static class BuildEtaEstimator
{
    public const int MinSamples = 3;
    public const int MaxSamples = 10;

    // Attaches the median duration of recent successful runs to every build of the same pipeline.
    public static List<BuildInfo> Annotate(IReadOnlyList<BuildInfo> builds)
    {
        var successfulByKey = builds
            .Where(build => build.IsSucceeded && !build.IsRunning && build.Duration.HasValue)
            .ToLookup(build => build.Key);

        return builds.Select(build =>
        {
            if (!build.IsRunning)
            {
                return build;
            }

            var samples = SelectSamples(successfulByKey[build.Key], build.Branch);
            return samples.Count < MinSamples
                ? build
                : build with { TypicalDuration = Median(samples), DurationSamples = samples.Count };
        }).ToList();
    }

    private static List<TimeSpan> SelectSamples(IEnumerable<BuildInfo> candidates, string branch)
    {
        var recent = candidates
            .OrderByDescending(build => build.FinishTime ?? DateTimeOffset.MinValue)
            .ToList();
        var sameBranch = recent
            .Where(build => string.Equals(build.Branch, branch, StringComparison.OrdinalIgnoreCase))
            .Take(MaxSamples)
            .ToList();
        var chosen = sameBranch.Count >= MinSamples ? sameBranch : recent.Take(MaxSamples).ToList();
        return chosen.Select(build => build.Duration!.Value).ToList();
    }

    private static TimeSpan Median(List<TimeSpan> durations)
    {
        var sorted = durations.OrderBy(duration => duration).ToList();
        var middle = sorted.Count / 2;
        return sorted.Count % 2 == 1 ? sorted[middle] : (sorted[middle - 1] + sorted[middle]) / 2;
    }
}
