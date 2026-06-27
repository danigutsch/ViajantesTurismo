using System.Collections.Concurrent;
using System.Diagnostics;
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
        using var activityListener = CatalogTelemetryTestsHelpers.CreateActivityListener(stoppedActivities);
        using var meterListener = CatalogTelemetryTestsHelpers.CreateMeterListener(measurements);
        using var rootActivity = CatalogTelemetryTestsHelpers.StartRootActivity();
        var handler = new IdempotentIntegrationEventConsumer<AdminTourCreatedIntegrationEvent>(
            new AdminTourCreatedIntegrationEventConsumer(new CapturingEventStore()),
            new CapturingIdempotencyStore(),
            new CatalogIntegrationEventOptions());
        var integrationEvent = new AdminTourCreatedIntegrationEvent(
            Guid.CreateVersion7(),
            DateTimeOffset.UtcNow,
            Guid.CreateVersion7(),
            "andes-2026",
            "Andes 2026");

        // Act
        await handler.Handle(integrationEvent, TestContext.Current.CancellationToken);

        // Assert
        var handlingActivity = CatalogTelemetryTestsHelpers.SingleActivity(stoppedActivities, rootActivity, CatalogTelemetry.ActivityIntegrationEventHandle);
        Assert.True(CatalogTelemetryTestsHelpers.HasTag(handlingActivity, CatalogTelemetry.TagIntegrationEventType, AdminTourCreatedIntegrationEvent.EventType));
        Assert.True(CatalogTelemetryTestsHelpers.HasTag(handlingActivity, CatalogTelemetry.TagOutcome, CatalogTelemetry.OutcomeSuccess));
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
        using var activityListener = CatalogTelemetryTestsHelpers.CreateActivityListener(stoppedActivities);
        using var meterListener = CatalogTelemetryTestsHelpers.CreateMeterListener(measurements);
        using var rootActivity = CatalogTelemetryTestsHelpers.StartRootActivity();
        var handler = new IdempotentIntegrationEventConsumer<AdminTourCreatedIntegrationEvent>(
            new AdminTourCreatedIntegrationEventConsumer(new CapturingEventStore()),
            new CapturingIdempotencyStore(started: false),
            new CatalogIntegrationEventOptions());
        var integrationEvent = new AdminTourCreatedIntegrationEvent(
            Guid.CreateVersion7(),
            DateTimeOffset.UtcNow,
            Guid.CreateVersion7(),
            "andes-2026",
            "Andes 2026");

        // Act
        await handler.Handle(integrationEvent, TestContext.Current.CancellationToken);

        // Assert
        var handlingActivity = CatalogTelemetryTestsHelpers.SingleActivity(stoppedActivities, rootActivity, CatalogTelemetry.ActivityIntegrationEventHandle);
        Assert.True(CatalogTelemetryTestsHelpers.HasTag(handlingActivity, CatalogTelemetry.TagOutcome, CatalogTelemetry.OutcomeSkipped));
        Assert.True(CatalogTelemetryTestsHelpers.HasTag(handlingActivity, CatalogTelemetry.TagIdempotencyOutcome, CatalogTelemetry.OutcomeSkipped));
        Assert.Contains(CatalogTelemetry.MetricIdempotencyOperation, measurements, StringComparer.Ordinal);
    }

    [Fact]
    public async Task Projection_processing_emits_checkpoint_event_count_and_metrics()
    {
        // Arrange
        var stoppedActivities = new ConcurrentQueue<Activity>();
        var measurements = new ConcurrentQueue<string>();
        using var activityListener = CatalogTelemetryTestsHelpers.CreateActivityListener(stoppedActivities);
        using var meterListener = CatalogTelemetryTestsHelpers.CreateMeterListener(measurements);
        using var rootActivity = CatalogTelemetryTestsHelpers.StartRootActivity();
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
        eventStore.AddReplayEvent(CatalogTelemetryTestsHelpers.CreateEnvelope(11, draftCreated, DateTimeOffset.UtcNow));

        // Act
        await runner.Project(TestContext.Current.CancellationToken);

        // Assert
        var projectionActivity = CatalogTelemetryTestsHelpers.SingleActivity(stoppedActivities, rootActivity, CatalogTelemetry.ActivityProjectionProcess);
        Assert.True(CatalogTelemetryTestsHelpers.HasTag(projectionActivity, CatalogTelemetry.TagProjectionName, projection.Name));
        Assert.True(CatalogTelemetryTestsHelpers.HasTag(projectionActivity, CatalogTelemetry.TagEventCount, 1));
        Assert.True(CatalogTelemetryTestsHelpers.HasTag(projectionActivity, CatalogTelemetry.TagCheckpointPosition, 11L));
        Assert.True(CatalogTelemetryTestsHelpers.HasTag(projectionActivity, CatalogTelemetry.TagOutcome, CatalogTelemetry.OutcomeSuccess));
        Assert.Contains(CatalogTelemetry.MetricProjectionEvent, measurements, StringComparer.Ordinal);
        Assert.Contains(CatalogTelemetry.MetricProjectionBatch, measurements, StringComparer.Ordinal);
    }

    [Fact]
    public async Task Integration_event_handling_emits_error_span_and_metrics()
    {
        // Arrange
        var stoppedActivities = new ConcurrentQueue<Activity>();
        var measurements = new ConcurrentQueue<string>();
        using var activityListener = CatalogTelemetryTestsHelpers.CreateActivityListener(stoppedActivities);
        using var meterListener = CatalogTelemetryTestsHelpers.CreateMeterListener(measurements);
        using var rootActivity = CatalogTelemetryTestsHelpers.StartRootActivity();
        var handler = new IdempotentIntegrationEventConsumer<AdminTourCreatedIntegrationEvent>(
            new AdminTourCreatedIntegrationEventConsumer(new ThrowingEventStore()),
            new CapturingIdempotencyStore(),
            new CatalogIntegrationEventOptions());
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
        var handlingActivity = CatalogTelemetryTestsHelpers.SingleActivity(stoppedActivities, rootActivity, CatalogTelemetry.ActivityIntegrationEventHandle);
        Assert.Equal(ActivityStatusCode.Error, handlingActivity.Status);
        Assert.Equal(exception.Message, handlingActivity.StatusDescription);
        Assert.True(CatalogTelemetryTestsHelpers.HasTag(handlingActivity, CatalogTelemetry.TagOutcome, CatalogTelemetry.OutcomeError));
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
        using var activityListener = CatalogTelemetryTestsHelpers.CreateActivityListener(stoppedActivities);
        using var meterListener = CatalogTelemetryTestsHelpers.CreateMeterListener(measurements);
        using var rootActivity = CatalogTelemetryTestsHelpers.StartRootActivity();
        var eventStore = new CapturingEventStore();
        var projection = new ThrowingProjection();
        var runner = new CatalogProjectionRunner(eventStore, new CapturingProjectionCheckpointStore(), [projection]);
        var draftCreated = new CatalogTourDraftCreated(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            "andes-2026",
            "Andes 2026",
            Guid.CreateVersion7());
        eventStore.AddReplayEvent(CatalogTelemetryTestsHelpers.CreateEnvelope(1, draftCreated, DateTimeOffset.UtcNow));

        // Act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await runner.Project(TestContext.Current.CancellationToken));

        // Assert
        var projectionActivity = CatalogTelemetryTestsHelpers.SingleActivity(stoppedActivities, rootActivity, CatalogTelemetry.ActivityProjectionProcess);
        Assert.Equal(ActivityStatusCode.Error, projectionActivity.Status);
        Assert.Equal(exception.Message, projectionActivity.StatusDescription);
        Assert.True(CatalogTelemetryTestsHelpers.HasTag(projectionActivity, CatalogTelemetry.TagProjectionName, projection.Name));
        Assert.True(CatalogTelemetryTestsHelpers.HasTag(projectionActivity, CatalogTelemetry.TagOutcome, CatalogTelemetry.OutcomeError));
        Assert.Contains(projectionActivity.Events, activityEvent => string.Equals(activityEvent.Name, "exception", StringComparison.Ordinal));
        Assert.Contains(CatalogTelemetry.MetricProjectionBatch, measurements, StringComparer.Ordinal);
    }

}
