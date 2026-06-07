namespace ViajantesTurismo.Admin.Testing.Integration;

public interface IAdminTestHost : IAsyncDisposable, IDisposable
{
    HttpClient Client { get; }
    Uri BaseUri { get; }
}
