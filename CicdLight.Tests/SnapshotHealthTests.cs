namespace CicdLight.Tests;

public sealed class SnapshotHealthTests
{
    private static PullRequestInfo Pr(bool isMine, int comments) =>
        new(1, "P", "R", "T", "a", isMine, false, "", UnresolvedCommentCount: comments);

    private static Snapshot Create(PullRequestInfo pullRequest, bool alertMine, bool alertOther) =>
        new([], [pullRequest], null, AlertMyPrComments: alertMine, AlertOtherPrComments: alertOther);

    [Theory]
    [InlineData(true, true, false, Health.Warning)]
    [InlineData(true, false, true, Health.Ok)]
    [InlineData(false, false, true, Health.Warning)]
    [InlineData(false, true, false, Health.Ok)]
    public void Health_FollowsAlertScope(bool isMine, bool alertMine, bool alertOther, Health expected)
    {
        Assert.Equal(expected, Create(Pr(isMine, 2), alertMine, alertOther).Health);
    }

    [Fact]
    public void Counters_SplitMineAndOther()
    {
        var snapshot = new Snapshot([], [Pr(true, 1), Pr(false, 3), Pr(false, 0)], null);

        Assert.Equal(1, snapshot.MyPullRequestsToRevise);
        Assert.Equal(1, snapshot.OtherPullRequestsToRevise);
        Assert.Equal(1, snapshot.PullRequestsToRevise);
    }
}
