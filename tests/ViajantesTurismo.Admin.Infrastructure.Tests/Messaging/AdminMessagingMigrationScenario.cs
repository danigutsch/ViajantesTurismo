using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using SharedKernel.Domain.EntityFrameworkCore;
using SharedKernel.EntityFrameworkCore;
using SharedKernel.Idempotency.EntityFrameworkCore;
using SharedKernel.Messaging.IntegrationEvents.EntityFrameworkCore;
using ViajantesTurismo.Resources;

namespace ViajantesTurismo.Admin.Infrastructure.Tests.Messaging;

internal sealed class AdminMessagingMigrationScenario : IAsyncDisposable
{
    public const string InitialMigration = "20260724210254_InitialAdmin";

    private readonly PostgreSqlTestDatabase database;
    private readonly string connectionString;

    private AdminMessagingMigrationScenario(PostgreSqlTestDatabase database)
    {
        this.database = database;
        connectionString = database.ConnectionString;
    }

    [SuppressMessage(
        "Reliability",
        "CA2000:Dispose objects before losing scope",
        Justification = "The returned scenario owns and disposes the fixture-issued database.")]
    public static async ValueTask<AdminMessagingMigrationScenario> Create(
        PostgreSqlTestServerFixture fixture,
        CancellationToken ct)
    {
        var database = await fixture.CreateDatabase(ct);
        return new AdminMessagingMigrationScenario(database);
    }

    public async Task ApplyInitialMigration(CancellationToken ct)
    {
        await using var dbContext = CreateDbContext();
        var migrator = dbContext.Database.GetService<IMigrator>();
        await migrator.MigrateAsync(InitialMigration, ct);
    }

    public async Task RemoveAllMigrations(CancellationToken ct)
    {
        await using var dbContext = CreateDbContext();
        var migrator = dbContext.Database.GetService<IMigrator>();
        await migrator.MigrateAsync(Migration.InitialDatabase, ct);
    }

    public async Task InsertIdempotencyRow(CancellationToken ct)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO messaging.idempotency_keys
                ("Scope", "Key", "State", "StartedAt", "CompletedAt", "ResultFingerprint")
            VALUES
                ('admin-test', 'event-1', 'Completed', TIMESTAMPTZ '2026-07-18T12:00:00Z',
                 TIMESTAMPTZ '2026-07-18T12:01:00Z', 'fingerprint');
            """;
        _ = await command.ExecuteNonQueryAsync(ct);
    }

    public Task<bool> IdempotencyTableExists(CancellationToken ct) =>
        RelationExists("messaging.idempotency_keys", ct);

    public Task<bool> OutboxTableExists(CancellationToken ct) =>
        RelationExists("messaging.outbox_messages", ct);

    public async Task<bool> CustomerEmailExtensionExists(CancellationToken ct)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT EXISTS (SELECT 1 FROM pg_extension WHERE extname = 'citext');";
        return (bool)(await command.ExecuteScalarAsync(ct) ?? false);
    }

    public async Task<int> IdempotencyRowCount(CancellationToken ct)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*)::int FROM messaging.idempotency_keys;";
        return Convert.ToInt32(await command.ExecuteScalarAsync(ct), System.Globalization.CultureInfo.InvariantCulture);
    }

    public async Task<string[]> GetMigrationHistory(CancellationToken ct)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT \"MigrationId\" FROM \"__EFMigrationsHistory\" ORDER BY \"MigrationId\";";
        await using var reader = await command.ExecuteReaderAsync(ct);
        var migrations = new List<string>();
        while (await reader.ReadAsync(ct))
        {
            migrations.Add(reader.GetString(0));
        }

        return [.. migrations];
    }

    public ValueTask DisposeAsync() => database.DisposeAsync();

    private async Task<bool> RelationExists(string relationName, CancellationToken ct)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT to_regclass(@relationName) IS NOT NULL;";
        _ = command.Parameters.AddWithValue("relationName", relationName);
        return (bool)(await command.ExecuteScalarAsync(ct) ?? false);
    }

    private AdminWriteDbContext CreateDbContext()
    {
        var services = new ServiceCollection();
        services.AddDomainEventDispatch<AdminWriteDbContext>();
        services.AddIdempotencyStore<AdminWriteDbContext>();
        services.AddIntegrationEventOutbox<AdminWriteDbContext>();
        services.AddPostgreSqlIntegrationEventTransportProducer<AdminWriteDbContext>(
            IntegrationEventConsumerNames.Catalog);
        using var provider = services.BuildServiceProvider();
        var configurations = provider.GetServices<IDbContextConfiguration<AdminWriteDbContext>>().ToArray();
        var options = new DbContextOptionsBuilder<AdminWriteDbContext>()
            .UseNpgsql(connectionString)
            .Options;
        return new AdminWriteDbContext(options, configurations);
    }
}
