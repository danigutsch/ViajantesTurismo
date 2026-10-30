using Npgsql;
using PandaBear.Npgsql.Testing;

namespace ViajantesTurismo.HostedTests.Infrastructure;

/// <summary>Issues databases on the assembly-owned PostgreSQL server.</summary>
public sealed class PostgreSqlTestServerFixture(HostedTestFixture shared) : IAsyncLifetime
{
    private PostgreSqlTestDatabasePool? pool;
    private string? connectionString;

    /// <summary>Names of the databases this fixture has issued and still owns.</summary>
    internal IReadOnlyCollection<string> OwnedDatabaseNames => Pool.OwnedDatabaseNames;
    private PostgreSqlTestDatabasePool Pool => pool ?? throw new InvalidOperationException("Fixture is not initialized.");

    public async ValueTask InitializeAsync()
    {
        connectionString = await shared.GetPostgreSqlConnectionString(TestContext.Current.CancellationToken);
        pool = new PostgreSqlTestDatabasePool(connectionString);
    }

    /// <summary>Issues a new test database from the pool.</summary>
    internal Task<PostgreSqlTestDatabaseLease> CreateDatabase(CancellationToken ct) => Pool.Create(ct);

    /// <summary>Checks whether a database named <paramref name="databaseName" /> exists on the server.</summary>
    internal async Task<bool> DatabaseExists(string databaseName, CancellationToken ct)
    {
        await using var dataSource = NpgsqlDataSource.Create(connectionString ?? throw new InvalidOperationException("Fixture is not initialized."));
        await using var command = dataSource.CreateCommand("SELECT EXISTS (SELECT 1 FROM pg_database WHERE datname = @databaseName);");
        command.Parameters.AddWithValue("databaseName", databaseName);
        return await command.ExecuteScalarAsync(ct) is true;
    }

    public async ValueTask DisposeAsync()
    {
        if (pool is not null)
        {
            await pool.DisposeAsync();
        }
    }
}
