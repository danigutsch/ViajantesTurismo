using Npgsql;
using Projects;
using SharedKernel.Testing;
using ViajantesTurismo.Resources;

namespace ViajantesTurismo.Admin.IntegrationTests.Infrastructure.Fixtures;

public sealed class AspireSerialIntegrationTestFixture : IAsyncLifetime, IDisposable
{
    private AspireTestApplication? _app;
    private HttpClient? _client;
    private string? _databaseConnectionString;

    public HttpClient Client => _client ?? throw new InvalidOperationException("Fixture is not initialized.");

    public Uri BaseUri => Client.BaseAddress ?? throw new InvalidOperationException("Client base address is not configured.");

    public async ValueTask InitializeAsync()
    {
        _app = await AspireTestApplication.Start<ViajantesTurismo_AppHost>([ResourceNames.Api], null, TestContext.Current.CancellationToken);
        _client = _app.CreateHttpClient(ResourceNames.Api);
        _databaseConnectionString = await _app.GetConnectionString(ResourceNames.Database, TestContext.Current.CancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        var client = _client;
        var app = _app;
        _client = null;
        _app = null;
        _databaseConnectionString = null;

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

    internal async Task ResetToKnownBaseline(CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(_databaseConnectionString);

        await using var connection = new NpgsqlConnection(_databaseConnectionString);
        await PostgreSqlPublicSchemaReset.Reset(connection, ct);
    }
}
