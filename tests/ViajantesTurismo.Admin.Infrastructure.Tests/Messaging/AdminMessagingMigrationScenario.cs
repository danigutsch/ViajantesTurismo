using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using SharedKernel.EntityFrameworkCore;
using SharedKernel.Idempotency.EntityFrameworkCore;
using SharedKernel.IntegrationTesting;
using SharedKernel.Messaging.IntegrationEvents.EntityFrameworkCore;
using ViajantesTurismo.Resources;

namespace ViajantesTurismo.Admin.Infrastructure.Tests.Messaging;

internal sealed class AdminMessagingMigrationScenario : IAsyncDisposable
{
    private const string PostgreSqlResourceName = "postgres";
    private const string DatabaseResourceName = "admin-messaging-migration";
    private const string MigrationApplicationName = "admin-messaging-migration-lock";
    private const string InsertApplicationName = "admin-messaging-concurrent-insert";
    private const int AdvisoryLockClassId = 1126;
    private const int AdvisoryLockObjectId = 20260719;

    public const string InitialMigration = "20260720203807_InitialAdmin";
    public const string RemovalMigration = "20260721193433_RemoveUnusedAdminIdempotencyKeys";
    public const string IdempotencyRestoreMigration = "20260723174241_EnforceUniqueDocumentRevisionsAndRestoreIdempotency";

    private readonly AspireTestApplication app;
    private readonly string connectionString;

    private AdminMessagingMigrationScenario(AspireTestApplication app, string connectionString)
    {
        this.app = app;
        this.connectionString = connectionString;
    }

    public static async ValueTask<AdminMessagingMigrationScenario> Create(CancellationToken ct)
    {
        var appBuilder = AspireTestApplication.CreateBuilder();
        var databaseServer = appBuilder.AddPostgres(PostgreSqlResourceName);
        _ = databaseServer.AddDatabase(DatabaseResourceName);

        var app = await AspireTestApplication.Start(appBuilder, [PostgreSqlResourceName], null, ct);
        var connectionString = await app.GetConnectionString(DatabaseResourceName, ct);

        return new AdminMessagingMigrationScenario(app, connectionString);
    }

    public async Task ApplyInitialMigration(CancellationToken ct)
    {
        await using var dbContext = CreateDbContext();
        var migrator = dbContext.Database.GetService<IMigrator>();
        await migrator.MigrateAsync(InitialMigration, ct);
    }

    public async Task ApplyRemovalMigration(CancellationToken ct)
    {
        await using var dbContext = CreateDbContext();
        var migrator = dbContext.Database.GetService<IMigrator>();
        await migrator.MigrateAsync(RemovalMigration, ct);
    }

    public async Task ApplyLatestMigration(CancellationToken ct)
    {
        await using var dbContext = CreateDbContext();
        await dbContext.Database.MigrateAsync(ct);
    }

    public async Task<PostgresException> ApplyRemovalMigrationWithConcurrentInboxInsert(CancellationToken ct)
    {
        await using var barrierConnection = CreateConnection("admin-messaging-migration-barrier");
        await barrierConnection.OpenAsync(ct);
        await CreateDropBarrier(barrierConnection, ct);
        await AcquireAdvisoryBarrier(barrierConnection, ct);
        var barrierHeld = true;

        try
        {
            var migrationTask = ApplyRemovalMigration(MigrationApplicationName, ct);
            await WaitForAdvisoryBarrier(MigrationApplicationName, migrationTask, ct);

            var insertTask = InsertUnexpectedInboxRow(InsertApplicationName, ct);
            await WaitForTableLockBarrier(InsertApplicationName, insertTask, ct);

            await ReleaseAdvisoryBarrier(barrierConnection, ct);
            barrierHeld = false;
            await migrationTask;

            try
            {
                await insertTask;
            }
            catch (PostgresException exception)
            {
                return exception;
            }

            throw new InvalidOperationException("Concurrent legacy insert unexpectedly succeeded while the migration dropped its table.");
        }
        finally
        {
            if (barrierHeld)
            {
                await ReleaseAdvisoryBarrier(barrierConnection, CancellationToken.None);
            }

            await RemoveDropBarrier(barrierConnection, CancellationToken.None);
        }
    }

    public async Task InsertUnexpectedInboxRow(CancellationToken ct)
    {
        await using var dbContext = CreateDbContext();
        _ = await dbContext.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO messaging.idempotency_keys
                ("Scope", "Key", "State", "StartedAt", "CompletedAt", "ResultFingerprint")
            VALUES
                ('admin-unexpected', 'event-1', 'Completed', TIMESTAMPTZ '2026-07-18T12:00:00Z', TIMESTAMPTZ '2026-07-18T12:01:00Z', 'unexpected-row');
            """,
            ct);
    }

    public async Task<Guid> InsertOutboxRow(CancellationToken ct)
    {
        var id = Guid.Parse("019bfab5-71f0-7d00-bc31-e8bf2f9c7812");
        await using var dbContext = CreateDbContext();
        _ = await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $$"""
            INSERT INTO messaging.outbox_messages
                ("Id", "EnqueuedAt", "PublishedAt", "PublishAttempts", "LastPublishAttemptAt",
                 "NextPublishAttemptAt", "LastPublishError", "ClaimedBy", "ClaimedUntil",
                 "EnvelopeSpec", "EnvelopeSpecVersion", "EventId", "Source", "EventType",
                 "EventVersion", "Time", "Subject", "DataContentType", "DataSchema", "Payload",
                 "PayloadEncoding", "ExtensionAttributesJson")
            VALUES
                ({{id}}, TIMESTAMPTZ '2026-07-18T12:00:00Z', TIMESTAMPTZ '2026-07-18T12:01:00Z', 2,
                 TIMESTAMPTZ '2026-07-18T12:00:30Z', TIMESTAMPTZ '2026-07-18T12:02:00Z',
                 'transient transport failure', 'relay-1', TIMESTAMPTZ '2026-07-18T12:03:00Z',
                 'cloudevents', '1.0', 'event-1', 'urn:test:admin', 'admin.tour.created', 1,
                 TIMESTAMPTZ '2026-07-18T11:59:00Z', 'tour-1', 'application/json',
                 'urn:schema:admin-tour-created', '{"tourId":"tour-1"}', 'Json',
                 '{"traceparent":"00-test"}');
            """,
            ct);

        return id;
    }

    public async Task<string> GetInboxRow(CancellationToken ct)
    {
        await using var dbContext = CreateDbContext();
        return await dbContext.Database.SqlQueryRaw<string>(
                "SELECT to_jsonb(row_data)::text AS \"Value\" FROM messaging.idempotency_keys AS row_data")
            .SingleAsync(ct);
    }

    public async Task<string> GetOutboxRow(Guid id, CancellationToken ct)
    {
        await using var dbContext = CreateDbContext();
        return await dbContext.Database.SqlQueryRaw<string>(
                "SELECT to_jsonb(row_data)::text AS \"Value\" FROM messaging.outbox_messages AS row_data WHERE \"Id\" = {0}",
                id)
            .SingleAsync(ct);
    }

    public async Task<string[]> GetMigrationHistory(CancellationToken ct)
    {
        await using var dbContext = CreateDbContext();
        return await dbContext.Database.SqlQueryRaw<string>(
                "SELECT \"MigrationId\" AS \"Value\" FROM \"__EFMigrationsHistory\" ORDER BY \"MigrationId\"")
            .ToArrayAsync(ct);
    }

    public async Task<bool> InboxTableExists(CancellationToken ct)
    {
        await using var dbContext = CreateDbContext();
        return await dbContext.Database.SqlQueryRaw<bool>(
                "SELECT to_regclass('messaging.idempotency_keys') IS NOT NULL AS \"Value\"")
            .SingleAsync(ct);
    }

    public async Task<bool> OutboxTableExists(CancellationToken ct)
    {
        await using var dbContext = CreateDbContext();
        return await dbContext.Database.SqlQueryRaw<bool>(
                "SELECT to_regclass('messaging.outbox_messages') IS NOT NULL AS \"Value\"")
            .SingleAsync(ct);
    }

    public async Task<int> InboxRowCount(CancellationToken ct)
    {
        await using var dbContext = CreateDbContext();
        return await dbContext.Database.SqlQueryRaw<int>(
                "SELECT COUNT(*)::int AS \"Value\" FROM messaging.idempotency_keys")
            .SingleAsync(ct);
    }

    public ValueTask DisposeAsync()
    {
        return app.DisposeAsync();
    }

    private async Task ApplyRemovalMigration(string applicationName, CancellationToken ct)
    {
        await using var dbContext = CreateDbContext(applicationName);
        var migrator = dbContext.Database.GetService<IMigrator>();
        await migrator.MigrateAsync(RemovalMigration, ct);
    }

    private async Task InsertUnexpectedInboxRow(string applicationName, CancellationToken ct)
    {
        await using var connection = CreateConnection(applicationName);
        await connection.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO messaging.idempotency_keys
                ("Scope", "Key", "State", "StartedAt", "CompletedAt", "ResultFingerprint")
            VALUES
                ('admin-concurrent', 'event-2', 'Completed', TIMESTAMPTZ '2026-07-18T12:00:00Z', TIMESTAMPTZ '2026-07-18T12:01:00Z', 'concurrent-row');
            """;
        _ = await command.ExecuteNonQueryAsync(ct);
    }

    private async Task WaitForAdvisoryBarrier(
        string applicationName,
        Task competingTask,
        CancellationToken ct)
    {
        await using var connection = CreateConnection("admin-messaging-lock-monitor");
        await connection.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT EXISTS (
                SELECT 1
                FROM pg_locks AS locks
                INNER JOIN pg_stat_activity AS activity ON activity.pid = locks.pid
                WHERE activity.application_name = @applicationName
                  AND locks.locktype = 'advisory'
                  AND NOT locks.granted)
            """;
        _ = command.Parameters.AddWithValue("applicationName", applicationName);
        await WaitForDatabaseCondition(command, applicationName, competingTask, ct);
    }

    private async Task WaitForTableLockBarrier(
        string applicationName,
        Task competingTask,
        CancellationToken ct)
    {
        await using var connection = CreateConnection("admin-messaging-lock-monitor");
        await connection.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT EXISTS (
                SELECT 1
                FROM pg_locks AS locks
                INNER JOIN pg_stat_activity AS activity ON activity.pid = locks.pid
                WHERE activity.application_name = @applicationName
                  AND locks.locktype = 'relation'
                  AND locks.relation = 'messaging.idempotency_keys'::regclass
                  AND NOT locks.granted)
            """;
        _ = command.Parameters.AddWithValue("applicationName", applicationName);
        await WaitForDatabaseCondition(command, applicationName, competingTask, ct);
    }

    private static async Task WaitForDatabaseCondition(
        NpgsqlCommand command,
        string applicationName,
        Task competingTask,
        CancellationToken ct)
    {
        while (true)
        {
            var observed = (bool)(await command.ExecuteScalarAsync(ct) ?? false);
            if (observed)
            {
                return;
            }

            if (competingTask.IsCompleted)
            {
                await competingTask;
                throw new InvalidOperationException($"Database session '{applicationName}' completed before reaching the expected lock barrier.");
            }

            await Task.Yield();
        }
    }

    private static async Task CreateDropBarrier(NpgsqlConnection connection, CancellationToken ct)
    {
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            CREATE OR REPLACE FUNCTION public.pause_admin_idempotency_drop()
            RETURNS event_trigger
            LANGUAGE plpgsql
            AS $barrier$
            BEGIN
                PERFORM pg_advisory_xact_lock(1126, 20260719);
            END;
            $barrier$;

            CREATE EVENT TRIGGER pause_admin_idempotency_drop
                ON ddl_command_start
                WHEN TAG IN ('DROP TABLE')
                EXECUTE FUNCTION public.pause_admin_idempotency_drop();
            """;
        _ = await command.ExecuteNonQueryAsync(ct);
    }

    private static async Task RemoveDropBarrier(NpgsqlConnection connection, CancellationToken ct)
    {
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            DROP EVENT TRIGGER IF EXISTS pause_admin_idempotency_drop;
            DROP FUNCTION IF EXISTS public.pause_admin_idempotency_drop();
            """;
        _ = await command.ExecuteNonQueryAsync(ct);
    }

    private static async Task AcquireAdvisoryBarrier(NpgsqlConnection connection, CancellationToken ct)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT pg_advisory_lock(@classId, @objectId)";
        _ = command.Parameters.AddWithValue("classId", AdvisoryLockClassId);
        _ = command.Parameters.AddWithValue("objectId", AdvisoryLockObjectId);
        _ = await command.ExecuteScalarAsync(ct);
    }

    private static async Task ReleaseAdvisoryBarrier(NpgsqlConnection connection, CancellationToken ct)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT pg_advisory_unlock(@classId, @objectId)";
        _ = command.Parameters.AddWithValue("classId", AdvisoryLockClassId);
        _ = command.Parameters.AddWithValue("objectId", AdvisoryLockObjectId);
        _ = await command.ExecuteScalarAsync(ct);
    }

    private NpgsqlConnection CreateConnection(string applicationName)
    {
        var connectionStringBuilder = new NpgsqlConnectionStringBuilder(connectionString)
        {
            ApplicationName = applicationName
        };
        return new NpgsqlConnection(connectionStringBuilder.ConnectionString);
    }

    private AdminWriteDbContext CreateDbContext(string? applicationName = null)
    {
        var services = new ServiceCollection();
        services.AddIdempotencyStore<AdminWriteDbContext>();
        services.AddIntegrationEventOutbox<AdminWriteDbContext>();
        services.AddPostgreSqlIntegrationEventTransportProducer<AdminWriteDbContext>(IntegrationEventConsumerNames.Catalog);
        using var provider = services.BuildServiceProvider();
        var configurations = provider.GetServices<IDbContextConfiguration<AdminWriteDbContext>>().ToArray();
        var configuredConnectionString = applicationName is null
            ? connectionString
            : new NpgsqlConnectionStringBuilder(connectionString) { ApplicationName = applicationName }.ConnectionString;
        var options = new DbContextOptionsBuilder<AdminWriteDbContext>()
            .UseNpgsql(configuredConnectionString)
            .Options;

        return new AdminWriteDbContext(options, configurations);
    }

}
