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
    private string? _databaseConnectionString;

    public HttpClient ApiClient { get; private set; } = null!;

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

        ApiClient = _app.CreateHttpClient(ResourceNames.Api);
        WebAppUrl = _app.GetEndpoint(ResourceNames.WebApp);
        _databaseConnectionString = await _app.GetConnectionStringAsync(ResourceNames.Database, cts.Token)
            ?? throw new InvalidOperationException("Database connection string is not configured.");
    }

    public async ValueTask DisposeAsync()
    {
        ApiClient.Dispose();

        if (_app is not null)
        {
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

    internal async Task ResetDatabase(CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(_databaseConnectionString);

        const string sql = """
                           DO $$
                           DECLARE
                               tables_to_truncate text;
                           BEGIN
                               SELECT string_agg('"' || tablename || '"', ', ')
                               INTO tables_to_truncate
                               FROM pg_tables
                               WHERE schemaname = 'public'
                                 AND tablename <> '__EFMigrationsHistory';

                               IF tables_to_truncate IS NOT NULL THEN
                                   EXECUTE 'TRUNCATE TABLE ' || tables_to_truncate || ' RESTART IDENTITY CASCADE';
                               END IF;
                           END $$;
                           """;

        await using var connection = new NpgsqlConnection(_databaseConnectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
