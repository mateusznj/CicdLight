namespace CicdLight;

public interface IIntegrationClient : IDisposable
{
    Task<Snapshot> FetchAsync(CancellationToken cancellationToken);
}
