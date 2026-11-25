using Microsoft.EntityFrameworkCore;
using Npgsql;
using SharedKernel.Npgsql.Testing;
using ViajantesTurismo.Management.Security;

namespace ViajantesTurismo.HostedTests.Management.Security;

/// <summary>Owns a migrated disposable database and a fixture-specific login with cache DML only.</summary>
internal sealed class PostgreSqlUserTokenCacheConcurrencyScenario(
    PostgreSqlTestDatabaseLease database,
    string login) : IAsyncDisposable
{
    private const string FixturePassword = "synthetic-vt-cache-fixture-only";

    /// <summary>Provisions the schema through the migration and grants only cache DML to the restricted login.</summary>
    public static async Task<PostgreSqlUserTokenCacheConcurrencyScenario> Create(HostedTestFixture fixture, CancellationToken ct)
    {
        var database = await fixture.Databases.Create(ct);
        var login = "vt_cache_" + Guid.NewGuid().ToString("N");
        try
        {
            var options = new DbContextOptionsBuilder<ManagementSecurityDbContext>()
                .UseNpgsql(database.ConnectionString)
                .Options;
            await using (var context = new ManagementSecurityDbContext(options))
            {
                await context.Database.MigrateAsync(ct);
            }

            await Provision(database.ConnectionString, login, ct);
            return new PostgreSqlUserTokenCacheConcurrencyScenario(database, login);
        }
        catch (Exception failure)
        {
            try
            {
                await Cleanup(database, login);
            }
            catch (Exception cleanupFailure)
            {
                throw new AggregateException(failure, cleanupFailure);
            }

            throw;
        }
    }

    /// <summary>Creates an independent instance of the production authentication registration as the restricted login.</summary>
    public ManagementSecurityPersistenceTestHost CreateHost() =>
        ManagementSecurityPersistenceTestHost.Create(CreateRuntimeConnectionString());

    /// <summary>Attempts to alter the cache table as the restricted login.</summary>
    public async Task AlterTableAsRuntime(CancellationToken ct) =>
        await ExecuteAsRuntime(
            new NpgsqlCommand("ALTER TABLE security.management_cookie_tickets ADD COLUMN forbidden integer;"), ct);

    /// <summary>Attempts to create temporary storage as the restricted login.</summary>
    public async Task CreateTemporaryTableAsRuntime(CancellationToken ct) =>
        await ExecuteAsRuntime(new NpgsqlCommand("CREATE TEMP TABLE forbidden (id integer);"), ct);

    /// <summary>Reads the persisted timestamps and confirms the absence of sliding expiry.</summary>
    public async Task<(DateTimeOffset ExpiresAt, DateTimeOffset AbsoluteExpiry, bool HasSlidingExpiry)> ReadExpiry(
        string key, CancellationToken ct)
    {
        await using var read = database.DataSource.CreateCommand("""
            SELECT expiresattime, absoluteexpiration, slidingexpirationinseconds
            FROM security.management_cookie_tickets
            WHERE id = $1;
            """);
        read.Parameters.Add(new NpgsqlParameter<string> { TypedValue = key });
        await using var reader = await read.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct))
        {
            throw new InvalidOperationException("The fixture cache entry is missing.");
        }

        return (
            await reader.GetFieldValueAsync<DateTimeOffset>(0, ct),
            await reader.GetFieldValueAsync<DateTimeOffset>(1, ct),
            !await reader.IsDBNullAsync(2, ct));
    }

    /// <summary>Expires a fixture row without relying on test timing or triggering provider cleanup.</summary>
    public async Task Expire(string key, CancellationToken ct)
    {
        await using var update = database.DataSource.CreateCommand("""
            UPDATE security.management_cookie_tickets
            SET expiresattime = clock_timestamp() - INTERVAL '1 hour'
            WHERE id = $1;
            """);
        update.Parameters.Add(new NpgsqlParameter<string> { TypedValue = key });
        await update.ExecuteNonQueryAsync(ct);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync() => await Cleanup(database, login);

    private static async Task Cleanup(PostgreSqlTestDatabaseLease database, string login)
    {
        var control = new NpgsqlConnectionStringBuilder(database.ConnectionString) { Database = "postgres", Pooling = false };
        await database.DisposeAsync();
        var definition = new NpgsqlBatchCommand("""
            CREATE FUNCTION pg_temp.drop_vt_cache_fixture_login(runtime_login text)
            RETURNS void LANGUAGE plpgsql SET search_path = pg_catalog, pg_temp AS $fixture$
            BEGIN
                EXECUTE pg_catalog.format('DROP ROLE IF EXISTS %I', runtime_login);
            END;
            $fixture$;
            """);
        var invocation = new NpgsqlBatchCommand("SELECT pg_temp.drop_vt_cache_fixture_login($1);");
        invocation.Parameters.Add(new NpgsqlParameter<string> { TypedValue = login });
        using var cleanup = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        await RunRoleBatch(control.ConnectionString, definition, invocation, cleanup.Token);
    }

    private string CreateRuntimeConnectionString() => new NpgsqlConnectionStringBuilder(database.ConnectionString)
    {
        Username = login,
        Password = FixturePassword,
        MaxPoolSize = 1
    }.ConnectionString;

    private static async Task Provision(string connectionString, string login, CancellationToken ct)
    {
        var definition = new NpgsqlBatchCommand("""
            CREATE FUNCTION pg_temp.create_vt_cache_fixture_login(runtime_login text, runtime_password text)
            RETURNS void LANGUAGE plpgsql SET search_path = pg_catalog, pg_temp AS $fixture$
            BEGIN
                EXECUTE pg_catalog.format('CREATE ROLE %I LOGIN PASSWORD %L', runtime_login, runtime_password);
                EXECUTE pg_catalog.format('REVOKE TEMPORARY ON DATABASE %I FROM PUBLIC', current_database());
                EXECUTE pg_catalog.format('GRANT USAGE ON SCHEMA security TO %I', runtime_login);
                EXECUTE pg_catalog.format(
                    'GRANT SELECT, INSERT, UPDATE, DELETE ON security.management_cookie_tickets TO %I',
                    runtime_login);
            END;
            $fixture$;
            """);
        var invocation = new NpgsqlBatchCommand("SELECT pg_temp.create_vt_cache_fixture_login($1, $2);");
        invocation.Parameters.Add(new NpgsqlParameter<string> { TypedValue = login });
        invocation.Parameters.Add(new NpgsqlParameter<string> { TypedValue = FixturePassword });
        await RunRoleBatch(connectionString, definition, invocation, ct);
    }

    private async Task ExecuteAsRuntime(NpgsqlCommand command, CancellationToken ct)
    {
        await using var runtimeCommand = command;
        await using var connection = new NpgsqlConnection(CreateRuntimeConnectionString());
        await connection.OpenAsync(ct);
        runtimeCommand.Connection = connection;
        await runtimeCommand.ExecuteNonQueryAsync(ct);
    }

    private static async Task RunRoleBatch(
        string connectionString, NpgsqlBatchCommand definition, NpgsqlBatchCommand invocation, CancellationToken ct)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(ct);
        await using var batch = new NpgsqlBatch(connection) { BatchCommands = { definition, invocation } };
        await batch.ExecuteNonQueryAsync(ct);
    }
}
