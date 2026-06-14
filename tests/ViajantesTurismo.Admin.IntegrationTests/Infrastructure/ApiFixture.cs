using Aspire.Hosting.Testing;
using Projects;
using ViajantesTurismo.Resources;

namespace ViajantesTurismo.Admin.IntegrationTests.Infrastructure;

public sealed class ApiFixture : ViajantesTurismo.Admin.Testing.Integration.IAdminTestHost, IAsyncLifetime
{
    private static readonly TimeSpan ResourceStartupTimeout = TimeSpan.FromSeconds(90);

    private IDistributedApplicationTestingBuilder? _appBuilder;
    private DistributedApplication? _app;
    private HttpClient? _client;

    public HttpClient Client => _client ?? throw new InvalidOperationException("Fixture is not initialized.");

    public Uri BaseUri => Client.BaseAddress ?? throw new InvalidOperationException("Client base address is not configured.");

    public async ValueTask InitializeAsync()
    {
        _appBuilder = await DistributedApplicationTestingBuilder.CreateAsync<ViajantesTurismo_AppHost>();
        _app = await _appBuilder.BuildAsync();
        await _app.StartAsync();

        using var cts = new CancellationTokenSource(ResourceStartupTimeout);
        await _app.ResourceNotifications.WaitForResourceHealthyAsync(ResourceNames.Api, cts.Token);

        _client = _app.CreateHttpClient(ResourceNames.Api);
    }

    public async ValueTask DisposeAsync()
    {
        var client = _client;
        var app = _app;
        var appBuilder = _appBuilder;
        _client = null;
        _app = null;
        _appBuilder = null;

        client?.Dispose();

        try
        {
            if (app is not null)
            {
                await app.StopAsync();
                await app.DisposeAsync();
            }
        }
        finally
        {
            if (appBuilder is not null)
            {
                await appBuilder.DisposeAsync();
            }
        }
    }

    public void Dispose()
    {
        DisposeAsync().AsTask().GetAwaiter().GetResult();
    }
}
