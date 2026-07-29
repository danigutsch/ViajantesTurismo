using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Npgsql;
using SharedKernel.IntegrationTesting;

namespace ViajantesTurismo.Admin.Infrastructure.Tests;

/// <summary>
/// Provides one lazily started PostgreSQL server and an isolated database for each test case.
/// </summary>
public sealed class PostgreSqlTestServerFixture : IAsyncLifetime
{
    private const string DatabaseResourceName = "adminfixture";
    private const string PostgreSqlResourceName = "postgres";

    private readonly ConcurrentDictionary<string, byte> ownedDatabases = new(StringComparer.Ordinal);
    private readonly SemaphoreSlim startupGate = new(1, 1);
    private AspireTestApplication? app;
    private string? administrativeConnectionString;

    /// <inheritdoc />
    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    internal async Task<PostgreSqlTestDatabase> CreateDatabase(CancellationToken ct)
    {
        var databaseName = $"{PostgreSqlTestDatabase.DatabaseNamePrefix}{Guid.NewGuid():N}";
        return await CreateDatabase(databaseName, ct);
    }

    internal async Task<PostgreSqlTestDatabase> CreateDatabase(
        string databaseName,
        CancellationToken ct)
    {
        var connectionString = await GetAdministrativeConnectionString(ct);
        if (!ownedDatabases.TryAdd(databaseName, 0))
        {
            throw new InvalidOperationException("A duplicate PostgreSQL test database was registered.");
        }

        return await PostgreSqlTestDatabase.Create(connectionString, databaseName, OnDatabaseDropped, ct);
    }

    internal bool OwnsDatabase(string databaseName) => ownedDatabases.ContainsKey(databaseName);

    internal async Task<bool> DatabaseExists(string databaseName, CancellationToken ct)
    {
        await using var dataSource = NpgsqlDataSource.Create(await GetAdministrativeConnectionString(ct));
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
        var connectionString = administrativeConnectionString;
        administrativeConnectionString = null;
        if (connectionString is not null)
        {
            foreach (var databaseName in ownedDatabases.Keys)
            {
                try
                {
                    await PostgreSqlTestDatabase.DropDatabase(connectionString, databaseName);
                    _ = ownedDatabases.TryRemove(databaseName, out _);
                }
                catch (Exception exception)
                {
                    failures.Add(exception);
                }
            }
        }

        var application = Interlocked.Exchange(ref app, null);
        try
        {
            if (application is not null)
            {
                try
                {
                    await application.DisposeAsync();
                }
                catch (Exception exception)
                {
                    failures.Add(exception);
                }
            }
        }
        finally
        {
            startupGate.Dispose();
        }

        if (failures.Count > 0)
        {
            throw new AggregateException("PostgreSQL test server cleanup failed.", failures);
        }
    }

    private async Task<string> GetAdministrativeConnectionString(CancellationToken ct)
    {
        var currentConnectionString = Volatile.Read(ref administrativeConnectionString);
        if (currentConnectionString is not null)
        {
            return currentConnectionString;
        }

        await startupGate.WaitAsync(ct);
        try
        {
            currentConnectionString = administrativeConnectionString;
            if (currentConnectionString is not null)
            {
                return currentConnectionString;
            }

            var appBuilder = AspireTestApplication.CreateBuilder();
            var databaseServer = appBuilder.AddPostgres(PostgreSqlResourceName);
            _ = databaseServer.AddDatabase(DatabaseResourceName);

            var application = await AspireTestApplication.Start(
                appBuilder,
                [PostgreSqlResourceName],
                resourceStartupTimeout: null,
                ct);
            try
            {
                var connectionString = await application.GetConnectionString(DatabaseResourceName, ct);
                currentConnectionString = new NpgsqlConnectionStringBuilder(connectionString)
                {
                    Database = "postgres",
                }.ConnectionString;
                app = application;
                Volatile.Write(ref administrativeConnectionString, currentConnectionString);
                return currentConnectionString;
            }
            catch (Exception startupFailure)
            {
                await PostgreSqlTestCleanup.DisposeResources(startupFailure, application);
                throw;
            }
        }
        finally
        {
            _ = startupGate.Release();
        }
    }

    private void OnDatabaseDropped(string databaseName)
    {
        _ = ownedDatabases.TryRemove(databaseName, out _);
    }
}
