using Npgsql;
using PandaBear.Npgsql.Testing;

namespace ViajantesTurismo.Admin.Infrastructure.Tests;

/// <summary>Issues databases on the assembly-owned PostgreSQL server.</summary>
public sealed class PostgreSqlTestServerFixture(HostedTestFixture shared) : IAsyncLifetime
{
    private PostgreSqlTestDatabasePool? pool;
    private string? connectionString;

    internal IReadOnlyCollection<string> OwnedDatabaseNames => Pool.OwnedDatabaseNames;
    private PostgreSqlTestDatabasePool Pool => pool ?? throw new InvalidOperationException("Fixture is not initialized.");

    public async ValueTask InitializeAsync()
    {
        connectionString = await shared.GetPostgreSqlConnectionString(TestContext.Current.CancellationToken);
        pool = new PostgreSqlTestDatabasePool(connectionString);
    }

    internal Task<PostgreSqlTestDatabaseLease> CreateDatabase(CancellationToken ct) => Pool.Create(ct);

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
