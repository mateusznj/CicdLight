namespace CicdLight;

public record PullRequestInfo(
    int Id, string Project, string Repository, string Title, string Author,
    bool IsMine, bool NeedsMyReview, string Url,
    string RepositoryId = "", int UnresolvedCommentCount = 0, string ConnectionId = "")
{
    public bool HasUnresolvedComments => UnresolvedCommentCount > 0;

    public string Key => string.IsNullOrEmpty(ConnectionId)
        ? $"P:{Project}:{Id}"
        : $"P:{ConnectionId}:{Project}:{Id}";
}
