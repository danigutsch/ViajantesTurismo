using Npgsql;
using System.Diagnostics.CodeAnalysis;

namespace ViajantesTurismo.Admin.IntegrationTests.Observability;

public sealed class PostgreSqlIndexHealthCollectorScenario(ApiFixture fixture) : IAsyncLifetime
{
    private static readonly TimeSpan ResourceTeardownTimeout = TimeSpan.FromSeconds(30);

    private readonly string _monitoringRoleName = $"index_health_reader_{Guid.NewGuid():N}";
    private readonly string _monitoringRolePassword = Guid.NewGuid().ToString("N");
    private PostgreSqlTestDatabase? _database;
    private NpgsqlDataSource? _monitoringDataSource;
    private NpgsqlDataSource? _testDataSource;

    public NpgsqlDataSource MonitoringDataSource => _monitoringDataSource
        ?? throw new InvalidOperationException("The PostgreSQL index-health scenario has not started.");

    public async ValueTask InitializeAsync()
    {
        _database = await fixture.CreateIsolatedPostgreSqlDatabase(TestContext.Current.CancellationToken);
        _testDataSource = _database.CreateDataSource("index-health-test");
        await ConfigureDatabase(
            _testDataSource,
            _monitoringRoleName,
            _monitoringRolePassword,
            TestContext.Current.CancellationToken);
        _monitoringDataSource = _database.CreateDataSource(
            "index-health-monitoring-test",
            _monitoringRoleName,
            _monitoringRolePassword);
    }

    public async Task<string[]> GetIndexDefinitions(CancellationToken ct)
    {
        var dataSource = _testDataSource ?? throw new InvalidOperationException("The PostgreSQL index-health scenario has not started.");
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT indexdef
            FROM pg_catalog.pg_indexes
            ORDER BY schemaname, indexname;
            """;
        await using var reader = await command.ExecuteReaderAsync(ct);
        var definitions = new List<string>();
        while (await reader.ReadAsync(ct))
        {
            definitions.Add(reader.GetString(0));
        }

        return [.. definitions];
    }

    public async Task CreateTableAsMonitoringRole(CancellationToken ct)
    {
        await using var connection = await MonitoringDataSource.OpenConnectionAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = "CREATE TABLE monitoring_role_must_not_write (id integer);";
        _ = await command.ExecuteNonQueryAsync(ct);
    }

    public async Task CreateTemporaryTableAsMonitoringRole(CancellationToken ct)
    {
        await using var connection = await MonitoringDataSource.OpenConnectionAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = "CREATE TEMP TABLE monitoring_role_must_not_create_temporary_objects (id integer);";
        _ = await command.ExecuteNonQueryAsync(ct);
    }

    [SuppressMessage(
        "Security",
        "CA2100:Review SQL queries for security vulnerabilities",
        Justification = "The operation is an internal enum mapped only to fixed test SQL literals.")]
    internal async Task AttemptDataMutationAsMonitoringRole(MonitoringRoleDataOperation operation, CancellationToken ct)
    {
        await using var connection = await MonitoringDataSource.OpenConnectionAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = operation switch
        {
            MonitoringRoleDataOperation.Insert => "INSERT INTO index_health_sample (id, payload) VALUES (2, 'unauthorized');",
            MonitoringRoleDataOperation.Update => "UPDATE index_health_sample SET payload = 'unauthorized' WHERE id = 1;",
            MonitoringRoleDataOperation.Delete => "DELETE FROM index_health_sample WHERE id = 1;",
            MonitoringRoleDataOperation.Drop => "DROP TABLE index_health_sample;",
            _ => throw new ArgumentOutOfRangeException(nameof(operation)),
        };
        _ = await command.ExecuteNonQueryAsync(ct);
    }

    public async Task<string> GetSamplePayload(CancellationToken ct)
    {
        var dataSource = _testDataSource ?? throw new InvalidOperationException("The PostgreSQL index-health scenario has not started.");
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT payload FROM index_health_sample WHERE id = 1;";
        return (string)(await command.ExecuteScalarAsync(ct) ?? throw new InvalidOperationException("The sample row is missing."));
    }

    public async ValueTask DisposeAsync()
    {
        var monitoringDataSource = _monitoringDataSource;
        var testDataSource = _testDataSource;
        var database = _database;
        _monitoringDataSource = null;
        _testDataSource = null;
        _database = null;

        var teardownFailures = new List<Exception>();
        if (monitoringDataSource is not null)
        {
            await CaptureTeardownFailure(_ => monitoringDataSource.DisposeAsync().AsTask(), teardownFailures);
        }

        if (testDataSource is not null)
        {
            await CaptureTeardownFailure(
                teardownCt => DropMonitoringRole(testDataSource, _monitoringRoleName, teardownCt),
                teardownFailures);
            await CaptureTeardownFailure(_ => testDataSource.DisposeAsync().AsTask(), teardownFailures);
        }

        if (database is not null)
        {
            await CaptureTeardownFailure(_ => database.DisposeAsync().AsTask(), teardownFailures);
        }

        if (teardownFailures.Count > 0)
        {
            throw new AggregateException("PostgreSQL index-health collector scenario teardown failed.", teardownFailures);
        }
    }

    [SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification = "Teardown must attempt every cleanup phase before reporting failures.")]
    private static async Task CaptureTeardownFailure(
        Func<CancellationToken, Task> operation,
        List<Exception> teardownFailures)
    {
        try
        {
            using var timeoutCts = new CancellationTokenSource(ResourceTeardownTimeout);
            await operation(timeoutCts.Token).WaitAsync(timeoutCts.Token);
        }
        catch (Exception exception)
        {
            teardownFailures.Add(exception);
        }
    }

    [SuppressMessage(
        "Security",
        "CA2100:Review SQL queries for security vulnerabilities",
        Justification = "The role identifier has a fixed prefix and Guid.NewGuid().ToString(\"N\"); the password is Guid.NewGuid().ToString(\"N\").")]
    private static async Task ConfigureDatabase(
        NpgsqlDataSource dataSource,
        string monitoringRoleName,
        string monitoringRolePassword,
        CancellationToken ct)
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE index_health_sample (id integer PRIMARY KEY, payload text NOT NULL);
            CREATE INDEX ix_index_health_sample_payload ON index_health_sample (payload);
            INSERT INTO index_health_sample (id, payload) VALUES (1, 'sample');
            ANALYZE index_health_sample;
            CREATE SCHEMA branding;
            CREATE TABLE branding.index_health_branding_sample (id integer PRIMARY KEY, payload text NOT NULL);
            CREATE INDEX ix_index_health_branding_sample_payload ON branding.index_health_branding_sample (payload);
            INSERT INTO branding.index_health_branding_sample (id, payload) VALUES (1, 'sample');
            ANALYZE branding.index_health_branding_sample;
            """;
        _ = await command.ExecuteNonQueryAsync(ct);

        using var commandBuilder = new NpgsqlCommandBuilder();
        var roleIdentifier = commandBuilder.QuoteIdentifier(monitoringRoleName);
        var databaseIdentifier = commandBuilder.QuoteIdentifier(connection.Database);
        command.CommandText = $"""
            CREATE ROLE {roleIdentifier} LOGIN PASSWORD '{monitoringRolePassword}';
            GRANT pg_read_all_stats TO {roleIdentifier};
            REVOKE TEMPORARY ON DATABASE {databaseIdentifier} FROM PUBLIC;
            REVOKE TEMPORARY ON DATABASE {databaseIdentifier} FROM {roleIdentifier};
            """;
        _ = await command.ExecuteNonQueryAsync(ct);
    }

    [SuppressMessage(
        "Security",
        "CA2100:Review SQL queries for security vulnerabilities",
        Justification = "The role identifier has a fixed prefix and Guid.NewGuid().ToString(\"N\").")]
    private static async Task DropMonitoringRole(NpgsqlDataSource dataSource, string monitoringRoleName, CancellationToken ct)
    {
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        await using var command = connection.CreateCommand();
        using var commandBuilder = new NpgsqlCommandBuilder();
        var roleIdentifier = commandBuilder.QuoteIdentifier(monitoringRoleName);
        command.CommandText = $"DROP ROLE IF EXISTS {roleIdentifier};";
        _ = await command.ExecuteNonQueryAsync(ct);
    }
}
