using Aspire.Hosting.Testing;
using Npgsql;
using Projects;
using ViajantesTurismo.Resources;

namespace ViajantesTurismo.Admin.SystemTests.Infrastructure.Fixtures;

public sealed class AspireSerialSystemTestFixture : IAspireSystemTestFixture, IAsyncLifetime, IDisposable
{
    private static readonly TimeSpan ResourceStartupTimeout = TimeSpan.FromSeconds(90);

    private IDistributedApplicationTestingBuilder? _appBuilder;
    private DistributedApplication? _app;
    private HttpClient? _apiClient;
    private string? _databaseConnectionString;

    public HttpClient ApiClient => _apiClient ?? throw new InvalidOperationException("Fixture is not initialized.");

    public Uri ApiBaseUri => ApiClient.BaseAddress ?? throw new InvalidOperationException("API client base address is not configured.");

    public Uri WebAppUrl { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        _appBuilder = await DistributedApplicationTestingBuilder.CreateAsync<ViajantesTurismo_AppHost>();
        _app = await _appBuilder.BuildAsync();
        await _app.StartAsync();

        using var cts = new CancellationTokenSource(ResourceStartupTimeout);

        await _app.ResourceNotifications.WaitForResourceHealthyAsync(ResourceNames.Api, cts.Token);
        await _app.ResourceNotifications.WaitForResourceHealthyAsync(ResourceNames.WebApp, cts.Token);

        _apiClient = _app.CreateHttpClient(ResourceNames.Api);
        WebAppUrl = _app.GetEndpoint(ResourceNames.WebApp);
        _databaseConnectionString = await _app.GetConnectionStringAsync(ResourceNames.Database, cts.Token)
            ?? throw new InvalidOperationException("Database connection string is not configured.");
    }

    public async ValueTask DisposeAsync()
    {
        _apiClient?.Dispose();
        _apiClient = null;

        if (_app is not null)
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
        }

        if (_appBuilder is not null)
        {
            await _appBuilder.DisposeAsync();
        }
    }

    public void Dispose()
    {
        DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    internal async Task ResetToKnownBaseline(CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(_databaseConnectionString);

        await using var connection = new NpgsqlConnection(_databaseConnectionString);
        await DatabaseResetHelper.ResetPublicTables(connection, ct);
    }
}
