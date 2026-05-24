namespace ViajantesTurismo.Admin.Testing.Integration;

public interface IAdminTestHost : IAsyncDisposable, IDisposable
{
    HttpClient Client { get; }
    Uri BaseUri { get; }
    Task Seed(CancellationToken cancellationToken = default);
    Task Reset(CancellationToken cancellationToken = default);
}
