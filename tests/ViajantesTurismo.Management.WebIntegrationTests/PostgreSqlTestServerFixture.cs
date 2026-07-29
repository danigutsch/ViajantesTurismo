using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using SharedKernel.IntegrationTesting;

namespace ViajantesTurismo.Management.WebIntegrationTests;

/// <summary>
/// Provides one PostgreSQL server and an isolated database for each test case.
/// </summary>
public sealed class PostgreSqlTestServerFixture : IAsyncLifetime
{
    private const string DatabaseResourceName = "managementfixture";
    private const string PostgreSqlResourceName = "postgres";

    private readonly ConcurrentDictionary<string, byte> _ownedDatabases = new(StringComparer.Ordinal);
    private AspireTestApplication? _app;
    private string? _administrativeConnectionString;

    /// <inheritdoc />
    public async ValueTask InitializeAsync()
    {
        var appBuilder = AspireTestApplication.CreateBuilder();
        var databaseServer = appBuilder.AddPostgres(PostgreSqlResourceName);
        _ = databaseServer.AddDatabase(DatabaseResourceName);

        var app = await AspireTestApplication.Start(
            appBuilder,
            [PostgreSqlResourceName],
            resourceStartupTimeout: null,
            TestContext.Current.CancellationToken);
        try
        {
            var connectionString = await app.GetConnectionString(
                DatabaseResourceName,
                TestContext.Current.CancellationToken);
            _administrativeConnectionString = new NpgsqlConnectionStringBuilder(connectionString)
            {
                Database = "postgres",
            }.ConnectionString;
            _app = app;
        }
        catch (Exception startupFailure)
        {
            await PostgreSqlTestCleanup.DisposeResources(startupFailure, app);
            throw;
        }
    }

    internal async Task<PostgreSqlTestDatabase> CreateDatabase(CancellationToken ct)
    {
        var databaseName = $"{PostgreSqlTestDatabase.DatabaseNamePrefix}{Guid.NewGuid():N}";
        return await CreateDatabase(databaseName, ct);
    }

    internal async Task<PostgreSqlTestDatabase> CreateDatabase(
        string databaseName,
        CancellationToken ct)
    {
        var connectionString = GetAdministrativeConnectionString();
        if (!_ownedDatabases.TryAdd(databaseName, 0))
        {
            throw new InvalidOperationException("A duplicate PostgreSQL test database was registered.");
        }

        return await PostgreSqlTestDatabase.Create(connectionString, databaseName, OnDatabaseDropped, ct);
    }

    internal bool OwnsDatabase(string databaseName) => _ownedDatabases.ContainsKey(databaseName);

    internal async Task<bool> DatabaseExists(string databaseName, CancellationToken ct)
    {
        await using var dataSource = NpgsqlDataSource.Create(GetAdministrativeConnectionString());
        await using var command = dataSource.CreateCommand(
            "SELECT EXISTS (SELECT 1 FROM pg_database WHERE datname = @databaseName);");
        command.Parameters.AddWithValue("databaseName", databaseName);
        return await command.ExecuteScalarAsync(ct) is true;
    }

    /// <inheritdoc />
    [SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification = "Assembly teardown must retry every owned database and Aspire cleanup before reporting aggregate failures.")]
    public async ValueTask DisposeAsync()
    {
        List<Exception> failures = [];
        var connectionString = _administrativeConnectionString;
        _administrativeConnectionString = null;
        if (connectionString is not null)
        {
            foreach (var databaseName in _ownedDatabases.Keys)
            {
                try
                {
                    await PostgreSqlTestDatabase.DropDatabase(connectionString, databaseName);
                    _ = _ownedDatabases.TryRemove(databaseName, out _);
                }
                catch (Exception exception)
                {
                    failures.Add(exception);
                }
            }
        }

        var app = Interlocked.Exchange(ref _app, null);
        if (app is not null)
        {
            try
            {
                await app.DisposeAsync();
            }
            catch (Exception exception)
            {
                failures.Add(exception);
            }
        }

        if (failures.Count > 0)
        {
            throw new AggregateException("PostgreSQL test server cleanup failed.", failures);
        }
    }

    private string GetAdministrativeConnectionString()
    {
        return _administrativeConnectionString
            ?? throw new InvalidOperationException("PostgreSQL test server fixture is not initialized.");
    }

    private void OnDatabaseDropped(string databaseName)
    {
        _ = _ownedDatabases.TryRemove(databaseName, out _);
    }
}
