using Projects;
using SharedKernel.Testing;
using ViajantesTurismo.Resources;

namespace ViajantesTurismo.Admin.IntegrationTests.Infrastructure;

public sealed class ApiFixture : ViajantesTurismo.Admin.Testing.Integration.IAdminTestHost, IAsyncLifetime
{
    private AspireTestApplication? _app;
    private HttpClient? _client;

    public HttpClient Client => _client ?? throw new InvalidOperationException("Fixture is not initialized.");

    public Uri BaseUri => Client.BaseAddress ?? throw new InvalidOperationException("Client base address is not configured.");

    public async ValueTask InitializeAsync()
    {
        _app = await AspireTestApplication.Start<ViajantesTurismo_AppHost>([ResourceNames.Api], ct: TestContext.Current.CancellationToken);
        _client = _app.CreateHttpClient(ResourceNames.Api);
    }

    public async ValueTask DisposeAsync()
    {
        var client = _client;
        var app = _app;
        _client = null;
        _app = null;

        client?.Dispose();

        if (app is not null)
        {
            await app.DisposeAsync();
        }
    }

    public void Dispose()
    {
        DisposeAsync().AsTask().GetAwaiter().GetResult();
    }
}
