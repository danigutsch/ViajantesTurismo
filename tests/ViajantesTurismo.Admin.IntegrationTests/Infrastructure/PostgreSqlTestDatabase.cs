using Npgsql;
using System.Diagnostics.CodeAnalysis;

namespace ViajantesTurismo.Admin.IntegrationTests.Infrastructure;

public sealed class PostgreSqlTestDatabase : IAsyncDisposable
{
    private static readonly TimeSpan DefaultDisposalTimeout = TimeSpan.FromSeconds(30);

    private readonly string _administrativeConnectionString;
    private readonly string _connectionString;
    private readonly string _databaseName;

    private PostgreSqlTestDatabase(string administrativeConnectionString, string connectionString, string databaseName)
    {
        _administrativeConnectionString = administrativeConnectionString;
        _connectionString = connectionString;
        _databaseName = databaseName;
    }

    [SuppressMessage(
        "Security",
        "CA2100:Review SQL queries for security vulnerabilities",
        Justification = "The database name is generated from a fixed ASCII prefix and Guid.NewGuid().ToString(\"N\").")]
    public static async Task<PostgreSqlTestDatabase> Create(string databaseConnectionString, CancellationToken ct)
    {
        var databaseName = $"test_{Guid.NewGuid():N}";
        var administrativeConnectionString = new NpgsqlConnectionStringBuilder(databaseConnectionString)
        {
            Database = "postgres",
        }.ConnectionString;
        var testDatabaseConnectionString = new NpgsqlConnectionStringBuilder(databaseConnectionString)
        {
            Database = databaseName,
        }.ConnectionString;

        await using var dataSource = NpgsqlDataSource.Create(administrativeConnectionString);
        await using var connection = await dataSource.OpenConnectionAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = $"CREATE DATABASE \"{databaseName}\";";
        _ = await command.ExecuteNonQueryAsync(ct);

        return new PostgreSqlTestDatabase(administrativeConnectionString, testDatabaseConnectionString, databaseName);
    }

    public NpgsqlDataSource CreateDataSource(string name)
    {
        return new NpgsqlDataSourceBuilder(_connectionString)
        {
            Name = name,
        }.Build();
    }

    public NpgsqlDataSource CreateDataSource(string name, string username, string password)
    {
        var connectionString = new NpgsqlConnectionStringBuilder(_connectionString)
        {
            Username = username,
            Password = password,
        }.ConnectionString;

        return new NpgsqlDataSourceBuilder(connectionString)
        {
            Name = name,
        }.Build();
    }

    [SuppressMessage(
        "Security",
        "CA2100:Review SQL queries for security vulnerabilities",
        Justification = "The database name is generated from a fixed ASCII prefix and Guid.NewGuid().ToString(\"N\").")]
    public async ValueTask DisposeAsync()
    {
        using var timeoutCts = new CancellationTokenSource(DefaultDisposalTimeout);
        await using var dataSource = NpgsqlDataSource.Create(_administrativeConnectionString);
        await using var connection = await dataSource.OpenConnectionAsync(timeoutCts.Token);
        await using var command = connection.CreateCommand();
        command.CommandText = $"DROP DATABASE IF EXISTS \"{_databaseName}\" WITH (FORCE);";
        _ = await command.ExecuteNonQueryAsync(timeoutCts.Token);
    }
}
