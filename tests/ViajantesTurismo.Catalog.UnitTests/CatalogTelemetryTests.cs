using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using SharedKernel.EventSourcing;
using ViajantesTurismo.Admin.Contracts.Tours;
using ViajantesTurismo.Catalog.Application;
using ViajantesTurismo.Catalog.Application.IntegrationEvents;
using ViajantesTurismo.Catalog.Application.Projections;
using ViajantesTurismo.Catalog.Application.Tours;
using ViajantesTurismo.Catalog.Domain.Tours;

namespace ViajantesTurismo.Catalog.UnitTests;

[Collection("Catalog telemetry")]
public sealed class CatalogTelemetryTests
{
    [Fact]
    public async Task Integration_event_handling_emits_success_span_and_metrics()
    {
        // Arrange
        var stoppedActivities = new ConcurrentQueue<Activity>();
        var measurements = new ConcurrentQueue<string>();
        using var activityListener = CreateActivityListener(stoppedActivities);
        using var meterListener = CreateMeterListener(measurements);
        using var rootActivity = StartRootActivity();
        var handler = new IdempotentIntegrationEventConsumer<AdminTourCreatedIntegrationEvent>(
            new AdminTourCreatedIntegrationEventConsumer(new CapturingEventStore()),
            new CapturingIdempotencyStore());
        var integrationEvent = new AdminTourCreatedIntegrationEvent(
            Guid.CreateVersion7(),
            DateTimeOffset.UtcNow,
            Guid.CreateVersion7(),
            "andes-2026",
            "Andes 2026");

        // Act
        await handler.Handle(integrationEvent, TestContext.Current.CancellationToken);

        // Assert
        var handlingActivity = SingleActivity(stoppedActivities, rootActivity, CatalogTelemetry.ActivityIntegrationEventHandle);
        Assert.True(HasTag(handlingActivity, CatalogTelemetry.TagIntegrationEventType, AdminTourCreatedIntegrationEvent.EventType));
        Assert.True(HasTag(handlingActivity, CatalogTelemetry.TagOutcome, CatalogTelemetry.OutcomeSuccess));
        Assert.Contains(CatalogTelemetry.MetricIntegrationEvent, measurements, StringComparer.Ordinal);
        Assert.Contains(CatalogTelemetry.MetricIdempotencyOperation, measurements, StringComparer.Ordinal);
        Assert.Contains(CatalogTelemetry.MetricTourStreamUpdate, measurements, StringComparer.Ordinal);
    }

    [Fact]
    public async Task Duplicate_integration_event_delivery_emits_skipped_idempotency_outcome()
    {
        // Arrange
        var stoppedActivities = new ConcurrentQueue<Activity>();
        var measurements = new ConcurrentQueue<string>();
        using var activityListener = CreateActivityListener(stoppedActivities);
        using var meterListener = CreateMeterListener(measurements);
        using var rootActivity = StartRootActivity();
        var handler = new IdempotentIntegrationEventConsumer<AdminTourCreatedIntegrationEvent>(
            new AdminTourCreatedIntegrationEventConsumer(new CapturingEventStore()),
            new CapturingIdempotencyStore(started: false));
        var integrationEvent = new AdminTourCreatedIntegrationEvent(
            Guid.CreateVersion7(),
            DateTimeOffset.UtcNow,
            Guid.CreateVersion7(),
            "andes-2026",
            "Andes 2026");

        // Act
        await handler.Handle(integrationEvent, TestContext.Current.CancellationToken);

        // Assert
        var handlingActivity = SingleActivity(stoppedActivities, rootActivity, CatalogTelemetry.ActivityIntegrationEventHandle);
        Assert.True(HasTag(handlingActivity, CatalogTelemetry.TagOutcome, CatalogTelemetry.OutcomeSkipped));
        Assert.True(HasTag(handlingActivity, CatalogTelemetry.TagIdempotencyOutcome, CatalogTelemetry.OutcomeSkipped));
        Assert.Contains(CatalogTelemetry.MetricIdempotencyOperation, measurements, StringComparer.Ordinal);
    }

    [Fact]
    public async Task Projection_processing_emits_checkpoint_event_count_and_metrics()
    {
        // Arrange
        var stoppedActivities = new ConcurrentQueue<Activity>();
        var measurements = new ConcurrentQueue<string>();
        using var activityListener = CreateActivityListener(stoppedActivities);
        using var meterListener = CreateMeterListener(measurements);
        using var rootActivity = StartRootActivity();
        var eventStore = new CapturingEventStore();
        var checkpointStore = new CapturingProjectionCheckpointStore
        {
            CurrentCheckpoint = new ProjectionCheckpoint("catalog.tours.read-model", 10),
        };
        var projection = new CatalogTourReadModelProjection(new CapturingCatalogTourReadModelStore());
        var runner = new CatalogProjectionRunner(eventStore, checkpointStore, [projection]);
        var draftCreated = new CatalogTourDraftCreated(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            "andes-2026",
            "Andes 2026",
            Guid.CreateVersion7());
        eventStore.AddReplayEvent(CreateEnvelope(11, draftCreated, DateTimeOffset.UtcNow));

        // Act
        await runner.Project(TestContext.Current.CancellationToken);

        // Assert
        var projectionActivity = SingleActivity(stoppedActivities, rootActivity, CatalogTelemetry.ActivityProjectionProcess);
        Assert.True(HasTag(projectionActivity, CatalogTelemetry.TagProjectionName, projection.Name));
        Assert.True(HasTag(projectionActivity, CatalogTelemetry.TagEventCount, 1));
        Assert.True(HasTag(projectionActivity, CatalogTelemetry.TagCheckpointPosition, 11L));
        Assert.True(HasTag(projectionActivity, CatalogTelemetry.TagOutcome, CatalogTelemetry.OutcomeSuccess));
        Assert.Contains(CatalogTelemetry.MetricProjectionEvent, measurements, StringComparer.Ordinal);
        Assert.Contains(CatalogTelemetry.MetricProjectionBatch, measurements, StringComparer.Ordinal);
    }

    [Fact]
    public async Task Integration_event_handling_emits_error_span_and_metrics()
    {
        // Arrange
        var stoppedActivities = new ConcurrentQueue<Activity>();
        var measurements = new ConcurrentQueue<string>();
        using var activityListener = CreateActivityListener(stoppedActivities);
        using var meterListener = CreateMeterListener(measurements);
        using var rootActivity = StartRootActivity();
        var handler = new IdempotentIntegrationEventConsumer<AdminTourCreatedIntegrationEvent>(
            new AdminTourCreatedIntegrationEventConsumer(new ThrowingEventStore()),
            new CapturingIdempotencyStore());
        var integrationEvent = new AdminTourCreatedIntegrationEvent(
            Guid.CreateVersion7(),
            DateTimeOffset.UtcNow,
            Guid.CreateVersion7(),
            "andes-2026",
            "Andes 2026");

        // Act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await handler.Handle(integrationEvent, TestContext.Current.CancellationToken));

        // Assert
        var handlingActivity = SingleActivity(stoppedActivities, rootActivity, CatalogTelemetry.ActivityIntegrationEventHandle);
        Assert.Equal(ActivityStatusCode.Error, handlingActivity.Status);
        Assert.Equal(exception.Message, handlingActivity.StatusDescription);
        Assert.True(HasTag(handlingActivity, CatalogTelemetry.TagOutcome, CatalogTelemetry.OutcomeError));
        Assert.Contains(handlingActivity.Events, activityEvent => string.Equals(activityEvent.Name, "exception", StringComparison.Ordinal));
        Assert.Contains(CatalogTelemetry.MetricIntegrationEvent, measurements, StringComparer.Ordinal);
        Assert.Contains(CatalogTelemetry.MetricIdempotencyOperation, measurements, StringComparer.Ordinal);
        Assert.Contains(CatalogTelemetry.MetricTourStreamUpdate, measurements, StringComparer.Ordinal);
    }

    [Fact]
    public async Task Projection_processing_emits_error_span_and_metric()
    {
        // Arrange
        var stoppedActivities = new ConcurrentQueue<Activity>();
        var measurements = new ConcurrentQueue<string>();
        using var activityListener = CreateActivityListener(stoppedActivities);
        using var meterListener = CreateMeterListener(measurements);
        using var rootActivity = StartRootActivity();
        var eventStore = new CapturingEventStore();
        var projection = new ThrowingProjection();
        var runner = new CatalogProjectionRunner(eventStore, new CapturingProjectionCheckpointStore(), [projection]);
        var draftCreated = new CatalogTourDraftCreated(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            "andes-2026",
            "Andes 2026",
            Guid.CreateVersion7());
        eventStore.AddReplayEvent(CreateEnvelope(1, draftCreated, DateTimeOffset.UtcNow));

        // Act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await runner.Project(TestContext.Current.CancellationToken));

        // Assert
        var projectionActivity = SingleActivity(stoppedActivities, rootActivity, CatalogTelemetry.ActivityProjectionProcess);
        Assert.Equal(ActivityStatusCode.Error, projectionActivity.Status);
        Assert.Equal(exception.Message, projectionActivity.StatusDescription);
        Assert.True(HasTag(projectionActivity, CatalogTelemetry.TagProjectionName, projection.Name));
        Assert.True(HasTag(projectionActivity, CatalogTelemetry.TagOutcome, CatalogTelemetry.OutcomeError));
        Assert.Contains(projectionActivity.Events, activityEvent => string.Equals(activityEvent.Name, "exception", StringComparison.Ordinal));
        Assert.Contains(CatalogTelemetry.MetricProjectionBatch, measurements, StringComparer.Ordinal);
    }

    private static ActivityListener CreateActivityListener(ConcurrentQueue<Activity> stoppedActivities)
    {
        var listener = new ActivityListener
        {
            ShouldListenTo = static source => string.Equals(source.Name, CatalogTelemetry.Name, StringComparison.Ordinal),
            Sample = static (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = stoppedActivities.Enqueue,
        };

        ActivitySource.AddActivityListener(listener);
        return listener;
    }

    private static Activity StartRootActivity()
    {
        var activity = new Activity("test.root");
        activity.Start();

        return activity;
    }

    private static Activity SingleActivity(
        ConcurrentQueue<Activity> stoppedActivities,
        Activity rootActivity,
        string operationName)
    {
        return Assert.Single(stoppedActivities, activity =>
            activity.TraceId == rootActivity.TraceId
            && string.Equals(activity.OperationName, operationName, StringComparison.Ordinal));
    }

    private static MeterListener CreateMeterListener(ConcurrentQueue<string> measurements)
    {
        var listener = new MeterListener
        {
            InstrumentPublished = static (instrument, listener) =>
            {
                if (string.Equals(instrument.Meter.Name, CatalogTelemetry.Name, StringComparison.Ordinal))
                {
                    listener.EnableMeasurementEvents(instrument);
                }
            },
        };

        listener.SetMeasurementEventCallback<long>((instrument, _, _, _) => measurements.Enqueue(instrument.Name));
        listener.Start();
        return listener;
    }

    private static bool HasTag(Activity activity, string key, object expectedValue)
    {
        foreach (var tag in activity.TagObjects)
        {
            if (string.Equals(tag.Key, key, StringComparison.Ordinal) && Equals(tag.Value, expectedValue))
            {
                return true;
            }
        }

        return false;
    }

    private static EventEnvelope CreateEnvelope(long position, CatalogTourDraftCreated draftCreated, DateTimeOffset recordedAt)
    {
        return new EventEnvelope(
            CatalogTourStreamIds.FromAdminTourId(draftCreated.AdminTourId),
            position,
            StreamRevision.From(1),
            Guid.CreateVersion7(),
            nameof(CatalogTourDraftCreated),
            draftCreated,
            recordedAt);
    }

    private sealed class ThrowingEventStore : IEventStore
    {
        public ValueTask Append(
            StreamId streamId,
            ExpectedStreamRevision expectedRevision,
            IReadOnlyCollection<object> events,
            CancellationToken ct) => throw new InvalidOperationException("append failed");

        public ValueTask<IReadOnlyCollection<EventEnvelope>> Load(
            StreamId streamId,
            StreamRevision? afterRevision,
            CancellationToken ct) => ValueTask.FromResult<IReadOnlyCollection<EventEnvelope>>([]);

        public ValueTask<IReadOnlyCollection<EventEnvelope>> LoadAfter(
            long position,
            int maxCount,
            CancellationToken ct) => ValueTask.FromResult<IReadOnlyCollection<EventEnvelope>>([]);
    }

    private sealed class ThrowingProjection : IProjection
    {
        public string Name => "catalog.throwing";

        public ValueTask Apply(EventEnvelope envelope, CancellationToken ct) => throw new InvalidOperationException("projection failed");
    }
}
