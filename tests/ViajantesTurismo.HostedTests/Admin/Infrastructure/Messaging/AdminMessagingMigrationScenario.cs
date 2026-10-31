using PostgreSqlTestDatabase = PandaBear.Npgsql.Testing.PostgreSqlTestDatabaseLease;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using PandaBear.Domain.EntityFrameworkCore;
using PandaBear.EntityFrameworkCore;
using PandaBear.Idempotency.EntityFrameworkCore;
using PandaBear.Messaging.IntegrationEvents.EntityFrameworkCore;
using ViajantesTurismo.Resources;
using ViajantesTurismo.Admin.Infrastructure;
using ViajantesTurismo.Admin.Infrastructure.DispositionHolds;

namespace ViajantesTurismo.HostedTests.Admin.Infrastructure.Messaging;

/// <summary>Runs messaging schema migrations against a real PostgreSQL database.</summary>
internal sealed class AdminMessagingMigrationScenario : IAsyncDisposable
{
    /// <summary>Name of the first Admin migration.</summary>
    public const string InitialMigration = "20260724210254_InitialAdmin";

    private readonly PostgreSqlTestDatabase database;
    private readonly string connectionString;

    private AdminMessagingMigrationScenario(PostgreSqlTestDatabase database)
    {
        this.database = database;
        connectionString = database.ConnectionString;
    }

    /// <summary>Creates a scenario over a new fixture-issued database.</summary>
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

    /// <summary>Migrates the database up to <see cref="InitialMigration" />.</summary>
    public async Task ApplyInitialMigration(CancellationToken ct)
    {
        await using var dbContext = CreateDbContext();
        var migrator = dbContext.Database.GetService<IMigrator>();
        await migrator.MigrateAsync(InitialMigration, ct);
    }

    /// <summary>Reverts the database to before any migration.</summary>
    public async Task RemoveAllMigrations(CancellationToken ct)
    {
        await using var dbContext = CreateDbContext();
        var migrator = dbContext.Database.GetService<IMigrator>();
        await migrator.MigrateAsync(Migration.InitialDatabase, ct);
    }

    /// <summary>Inserts one completed idempotency row with raw SQL.</summary>
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

    /// <summary>Returns whether the messaging idempotency keys table exists.</summary>
    public Task<bool> IdempotencyTableExists(CancellationToken ct) =>
        RelationExists("messaging.idempotency_keys", ct);

    /// <summary>Returns whether the messaging outbox messages table exists.</summary>
    public Task<bool> OutboxTableExists(CancellationToken ct) =>
        RelationExists("messaging.outbox_messages", ct);

    /// <summary>Returns whether the <c>citext</c> extension is installed in the database.</summary>
    public async Task<bool> CustomerEmailExtensionExists(CancellationToken ct)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT EXISTS (SELECT 1 FROM pg_extension WHERE extname = 'citext');";
        return (bool)(await command.ExecuteScalarAsync(ct) ?? false);
    }

    /// <summary>Counts the rows in the idempotency keys table.</summary>
    public async Task<int> IdempotencyRowCount(CancellationToken ct)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*)::int FROM messaging.idempotency_keys;";
        return Convert.ToInt32(await command.ExecuteScalarAsync(ct), global::System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>Reads the applied migration ids in order.</summary>
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

    /// <summary>Disposes the fixture-issued database.</summary>
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
        services.AddAdminDispositionHolds();
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
