extern alias AdminWorker;

using System.Diagnostics;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;
using OpenTelemetry;
using OpenTelemetry.Trace;
using SharedKernel.ServiceDefaults;
using ViajantesTurismo.Admin.Application.Branding;
using ViajantesTurismo.Admin.Infrastructure;
using ViajantesTurismo.Branding.Contracts.IntegrationEvents;
using ViajantesTurismo.Branding.Contracts.IntegrationEvents.Branding;
using ViajantesTurismo.Resources;
using ViajantesTurismo.ServiceDefaults;
using ViajantesTurismo.HostedTests.Catalog.Infrastructure.Branding;
using SharedKernel.Testing.Snapshots;
using ViajantesTurismo.Testing.Reproduction;
using PostgreSqlTestDatabaseLease = SharedKernel.Npgsql.Testing.PostgreSqlTestDatabaseLease;

namespace ViajantesTurismo.HostedTests.Admin.Infrastructure.Branding;

/// <summary>Runs the Admin worker against separately owned Admin and Catalog databases.</summary>
internal sealed class BrandingWorkerReproductionScenario : IAsyncDisposable
{
    /// <summary>The synthetic name committed before the recovery fence.</summary>
    public const string OriginalBrandName = "Synthetic original";

    /// <summary>The synthetic message value kept out of the reproduction manifest.</summary>
    public const string ReplayBrandName = "Synthetic replay payload marker";

    /// <summary>The synthetic recovery operator kept out of the reproduction manifest.</summary>
    public const string Operator = "operator@example.test";

    /// <summary>The fixed event identity retried and delivered twice within each reset.</summary>
    public static Guid ReplayEventId { get; } = Guid.Parse("0199f0aa-0000-7000-8000-000000000102");

    /// <summary>The fixed synthetic time at which the transport is seeded.</summary>
    public static DateTimeOffset InitialTime { get; } = new(2026, 10, 24, 12, 0, 0, TimeSpan.Zero);

    /// <summary>The reviewed manifest outcome for an observed successful retry and duplicate settlement.</summary>
    public static ReproductionOutcome Replayed
    {
        get;
    } = new("reproduced", "replayed",
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["admin-worker"] = "ready"
        });

    /// <summary>Registers the worker fixture and binds its permitted outcome to the compiled source revision.</summary>
    public static ReproductionSpecification Specification
    {
        get;
    } = new(
        "background.branding-redelivery",
        ReproductionFixtureEnvironment.GetSourceRevision(typeof(BrandingWorkerReproductionScenario).Assembly),
        [Replayed]);

    private readonly IHost host;
    private readonly NpgsqlDataSource transport;
    private readonly BrandingWorkerReproductionClock clock;
    private readonly TaskCompletionSource<ActivityTraceId> retryTrace;

    private BrandingWorkerReproductionScenario(
        IHost host,
        NpgsqlDataSource transport,
        BrandingWorkerReproductionClock clock,
        TaskCompletionSource<ActivityTraceId> retryTrace)
    {
        this.host = host;
        this.transport = transport;
        this.clock = clock;
        this.retryTrace = retryTrace;
    }

    /// <summary>Reproduces a fenced delivery, its authorized retry and a repeat delivery from fresh records.</summary>
    public static async Task<BrandingWorkerReproductionRun> Run(PostgreSqlTestServerFixture fixture, CancellationToken ct)
    {
        var startedUtc = TimeProvider.System.GetUtcNow();
        var started = Stopwatch.GetTimestamp();
        var attemptId = Guid.NewGuid();
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(ct);
        deadline.CancelAfter(TimeSpan.FromSeconds(45));
        var attemptCancellation = deadline.Token;
        await using var admin = await fixture.CreateDatabase(attemptCancellation);
        await using var catalog = await fixture.CreateDatabase(attemptCancellation);
        await using var projection = await BrandingProjectionPostgreSqlScenario.Create(admin, attemptCancellation);
        using var attempt = new Activity(Specification.Scenario);
        attempt.SetIdFormat(ActivityIdFormat.W3C)
            .SetParentId(ActivityTraceId.CreateRandom(), ActivitySpanId.CreateRandom(), ActivityTraceFlags.Recorded)
            .Start();
        await using var worker = await Start(admin, catalog, attemptCancellation);
        var original = BrandingProjectionTestEvents.Create(3, OriginalBrandName) with
        {
            EventId = Guid.Parse("0199f0aa-0000-7000-8000-000000000100")
        };
        var replay = BrandingProjectionTestEvents.Create(
            1, ReplayBrandName, Guid.Parse("0199f0aa-0000-7000-8000-000000000101")) with
        {
            EventId = ReplayEventId
        };

        await worker.Enqueue(original, attemptCancellation);
        await worker.WaitForDelivery(original.EventId, static delivery => delivery.Processed, attemptCancellation);
        await worker.Enqueue(replay, attemptCancellation);
        var blocked = await worker.WaitForDelivery(replay.EventId, static delivery => delivery.RetryAt.HasValue, attemptCancellation);
        var trace = await worker.ObserveRetryTrace(attemptCancellation);
        var beforeRetry = await projection.Read(attemptCancellation);
        var recovery = await projection.ReadRecovery(attemptCancellation);
        var authorization = await projection.CreateRecoveryService().AuthorizeReseed(
            new BrandingReseedRequest(original.SourceEpoch, original.SourceRevision, replay.EventId, replay.SourceEpoch,
                replay.SourceRevision, "restore", "REPRO-7", Operator),
            attemptCancellation);
        worker.AdvanceTo(blocked.RetryAt.ShouldNotBeNull());
        var retried = await worker.WaitForDelivery(replay.EventId, static delivery => delivery.Processed, attemptCancellation);
        await worker.ReopenDelivery(replay.EventId, attemptCancellation);
        var duplicate = await worker.WaitForDelivery(replay.EventId, static delivery => delivery.Processed, attemptCancellation);
        var afterReplay = await projection.Read(attemptCancellation);
        var afterRecovery = await projection.ReadRecovery(attemptCancellation);

        return new BrandingWorkerReproductionRun(attemptId, trace, startedUtc,
            checked((int)Stopwatch.GetElapsedTime(started).TotalMilliseconds), beforeRetry, recovery,
            blocked, authorization.IsSuccess, retried, duplicate, afterReplay, afterRecovery);
    }

    /// <summary>Migrates the owned transport database and starts the normal Admin consumer and telemetry registration.</summary>
    private static async Task<BrandingWorkerReproductionScenario> Start(
        PostgreSqlTestDatabaseLease admin,
        PostgreSqlTestDatabaseLease catalog,
        CancellationToken ct)
    {
        await using var dbContext = BrandingTransportPostgreSqlScenario.CreateDbContext(catalog.ConnectionString);
        await dbContext.Database.MigrateAsync(ct);

        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings { EnvironmentName = Environments.Production });
        builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"] = string.Empty;
        builder.Configuration[$"ConnectionStrings:{ResourceNames.AdminDatabase}"] = admin.ConnectionString;
        builder.Configuration[$"ConnectionStrings:{ResourceNames.CatalogDatabase}"] = catalog.ConnectionString;
        var clock = new BrandingWorkerReproductionClock(InitialTime);
        var retryTrace = new TaskCompletionSource<ActivityTraceId>(TaskCreationOptions.RunContinuationsAsynchronously);
        builder.Services.AddSingleton<TimeProvider>(clock);
        builder.AddApplicationDefaults(ViajantesTurismoServiceDefaults.Configure).AddViajantesTurismoTelemetry();
        builder.Logging.AddOpenTelemetry(options => options.AddProcessor(
            new SimpleLogRecordExportProcessor(new BrandingWorkerRetryTraceExporter(retryTrace))));
        builder.AddAdminIntegrationEventWorkerInfrastructure();
        AdminWorker::ViajantesTurismo.Admin.IntegrationEventWorker.AdminWorkerIntegrationEvents
            .AddAdminWorkerIntegrationEventDelivery(builder.Services);
        var host = builder.Build();
        try
        {
            _ = host.Services.GetRequiredService<TracerProvider>();
            await host.StartAsync(ct);
            return new BrandingWorkerReproductionScenario(host, catalog.DataSource, clock, retryTrace);
        }
        catch
        {
            host.Dispose();
            throw;
        }
    }

    /// <summary>Seeds the worker's actual transport with one reviewed synthetic event.</summary>
    private async Task Enqueue(BrandingSettingsChangedIntegrationEvent integrationEvent, CancellationToken ct)
    {
        var payload = JsonSerializer.Serialize(
            integrationEvent, BrandingIntegrationEventJsonContext.Default.BrandingSettingsChangedIntegrationEvent);
        await using var command = transport.CreateCommand("""
            INSERT INTO messaging.transport_messages
                ("Id", "ConsumerName", "ReceivedAt", "ConsumeAttempts", "EnvelopeSpec", "EnvelopeSpecVersion",
                 "EventId", "Source", "EventType", "EventVersion", "Time", "DataContentType", "Payload", "PayloadEncoding")
            VALUES
                (@id, 'admin', @now, 0, 'cloudevents', '1.0',
                 @eventId, 'urn:viajantes:branding', @eventType, @eventVersion, @time, 'application/json', @payload, 'json')
            """);
        command.Parameters.AddWithValue("id", integrationEvent.EventId);
        command.Parameters.AddWithValue("now", clock.GetUtcNow());
        command.Parameters.AddWithValue("eventId", integrationEvent.EventId.ToString("N"));
        command.Parameters.AddWithValue("eventType", BrandingSettingsChangedIntegrationEvent.EventType);
        command.Parameters.AddWithValue("eventVersion", BrandingSettingsChangedIntegrationEvent.EventVersion);
        command.Parameters.AddWithValue("time", integrationEvent.OccurredAt);
        command.Parameters.AddWithValue("payload", payload);
        _ = await command.ExecuteNonQueryAsync(ct);
    }

    /// <summary>Waits for matching persisted delivery state within the test's cancellation boundary.</summary>
    private async Task<BrandingWorkerDelivery> WaitForDelivery(
        Guid id, Func<BrandingWorkerDelivery, bool> condition, CancellationToken ct)
    {
        using var poll = new PeriodicTimer(TimeSpan.FromMilliseconds(50));
        while (true)
        {
            var state = await ReadDelivery(id, ct);
            if (condition(state))
            {
                return state;
            }

            _ = await poll.WaitForNextTickAsync(ct);
        }
    }

    /// <summary>Advances the fixture clock to the due time persisted by the actual worker.</summary>
    private void AdvanceTo(DateTimeOffset time) => clock.AdvanceTo(time);

    /// <summary>Models a lost transport acknowledgement for a settled row owned by this fixture.</summary>
    private async Task ReopenDelivery(Guid id, CancellationToken ct)
    {
        await using var command = transport.CreateCommand("""
            UPDATE messaging.transport_messages
            SET "ProcessedAt" = NULL
            WHERE "Id" = @id AND "ProcessedAt" IS NOT NULL
            """);
        command.Parameters.AddWithValue("id", id);
        var reopened = await command.ExecuteNonQueryAsync(ct);
        reopened.ShouldBe(1);
    }

    /// <summary>Gets the observed trace ID from the transport's retry event.</summary>
    private async Task<ActivityTraceId> ObserveRetryTrace(CancellationToken ct) => await retryTrace.Task.WaitAsync(ct);

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        try
        {
            await host.StopAsync(stop.Token);
        }
        finally
        {
            host.Dispose();
        }
    }

    private async Task<BrandingWorkerDelivery> ReadDelivery(Guid id, CancellationToken ct)
    {
        await using var command = transport.CreateCommand("""
            SELECT "ProcessedAt" IS NOT NULL, "ConsumeAttempts", "NextConsumeAttemptAt", "LastConsumeError",
                   "PoisonedAt" IS NOT NULL, "ClaimedUntil" IS NOT NULL
            FROM messaging.transport_messages WHERE "Id" = @id
            """);
        command.Parameters.AddWithValue("id", id);
        await using var reader = await command.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct))
        {
            throw new InvalidOperationException("The owned transport row is missing.");
        }

        return new BrandingWorkerDelivery(
            reader.GetBoolean(0),
            reader.GetInt32(1),
            await reader.IsDBNullAsync(2, ct) ? null : await reader.GetFieldValueAsync<DateTimeOffset>(2, ct),
            await reader.IsDBNullAsync(3, ct) ? null : reader.GetString(3),
            reader.GetBoolean(4),
            reader.GetBoolean(5));
    }
}
