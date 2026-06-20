using Aspire.Hosting.Testing;
using Npgsql;
using Projects;
using ViajantesTurismo.Resources;

namespace ViajantesTurismo.Admin.IntegrationTests.Infrastructure.Fixtures;

public sealed class AspireSerialIntegrationTestFixture : IAsyncLifetime, IDisposable
{
    private static readonly TimeSpan ResourceStartupTimeout = TimeSpan.FromSeconds(90);

    private IDistributedApplicationTestingBuilder? _appBuilder;
    private DistributedApplication? _app;
    private HttpClient? _client;
    private string? _databaseConnectionString;

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
        _databaseConnectionString = await _app.GetConnectionStringAsync(ResourceNames.Database, cts.Token)
            ?? throw new InvalidOperationException("Database connection string is not configured.");
    }

    public async ValueTask DisposeAsync()
    {
        var client = _client;
        var app = _app;
        var appBuilder = _appBuilder;
        _client = null;
        _app = null;
        _appBuilder = null;
        _databaseConnectionString = null;

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

    internal async Task ResetToKnownBaseline(CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(_databaseConnectionString);

        await using var connection = new NpgsqlConnection(_databaseConnectionString);
        await DatabaseResetHelper.ResetPublicTables(connection, ct);
    }
}
