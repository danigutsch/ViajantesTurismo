using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.Options;
using SharedKernel.EventSourcing;
using ViajantesTurismo.Admin.Contracts.IntegrationEvents.Tours;
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
        var handler = new IdempotentIntegrationHandler<AdminTourCreatedIntegrationEvent>(
            new AdminTourCreatedIntegrationHandler(new CapturingEventStore()),
            new CapturingIdempotencyStore(),
            Options.Create(new IntegrationEventOptions()));
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
        TestAssert.True(CatalogTelemetryTestsHelpers.HasTag(handlingActivity, CatalogTelemetry.TagIntegrationEventType, AdminTourCreatedIntegrationEvent.EventType));
        TestAssert.True(CatalogTelemetryTestsHelpers.HasTag(handlingActivity, CatalogTelemetry.TagOutcome, CatalogTelemetry.OutcomeSuccess));
        TestAssert.Contains(CatalogTelemetry.MetricIntegrationEvent, measurements, StringComparer.Ordinal);
        TestAssert.Contains(CatalogTelemetry.MetricIdempotencyOperation, measurements, StringComparer.Ordinal);
        TestAssert.Contains(CatalogTelemetry.MetricTourStreamUpdate, measurements, StringComparer.Ordinal);
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
        var handler = new IdempotentIntegrationHandler<AdminTourCreatedIntegrationEvent>(
            new AdminTourCreatedIntegrationHandler(new CapturingEventStore()),
            new CapturingIdempotencyStore(started: false),
            Options.Create(new IntegrationEventOptions()));
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
        TestAssert.True(CatalogTelemetryTestsHelpers.HasTag(handlingActivity, CatalogTelemetry.TagOutcome, CatalogTelemetry.OutcomeSkipped));
        TestAssert.True(CatalogTelemetryTestsHelpers.HasTag(handlingActivity, CatalogTelemetry.TagIdempotencyOutcome, CatalogTelemetry.OutcomeSkipped));
        TestAssert.Contains(CatalogTelemetry.MetricIdempotencyOperation, measurements, StringComparer.Ordinal);
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
        TestAssert.True(CatalogTelemetryTestsHelpers.HasTag(projectionActivity, CatalogTelemetry.TagProjectionName, projection.Name));
        TestAssert.True(CatalogTelemetryTestsHelpers.HasTag(projectionActivity, CatalogTelemetry.TagEventCount, 1));
        TestAssert.True(CatalogTelemetryTestsHelpers.HasTag(projectionActivity, CatalogTelemetry.TagCheckpointPosition, 11L));
        TestAssert.True(CatalogTelemetryTestsHelpers.HasTag(projectionActivity, CatalogTelemetry.TagOutcome, CatalogTelemetry.OutcomeSuccess));
        TestAssert.Contains(CatalogTelemetry.MetricProjectionEvent, measurements, StringComparer.Ordinal);
        TestAssert.Contains(CatalogTelemetry.MetricProjectionBatch, measurements, StringComparer.Ordinal);
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
        var handler = new IdempotentIntegrationHandler<AdminTourCreatedIntegrationEvent>(
            new AdminTourCreatedIntegrationHandler(new ThrowingEventStore()),
            new CapturingIdempotencyStore(),
            Options.Create(new IntegrationEventOptions()));
        var integrationEvent = new AdminTourCreatedIntegrationEvent(
            Guid.CreateVersion7(),
            DateTimeOffset.UtcNow,
            Guid.CreateVersion7(),
            "andes-2026",
            "Andes 2026");

        // Act
        var exception = await TestAssert.Throws<InvalidOperationException>(async () =>
            await handler.Handle(integrationEvent, TestContext.Current.CancellationToken));

        // Assert
        var handlingActivity = CatalogTelemetryTestsHelpers.SingleActivity(stoppedActivities, rootActivity, CatalogTelemetry.ActivityIntegrationEventHandle);
        TestAssert.Equal(ActivityStatusCode.Error, handlingActivity.Status);
        TestAssert.Equal(exception.Message, handlingActivity.StatusDescription);
        TestAssert.True(CatalogTelemetryTestsHelpers.HasTag(handlingActivity, CatalogTelemetry.TagOutcome, CatalogTelemetry.OutcomeError));
        TestAssert.Contains(handlingActivity.Events, activityEvent => string.Equals(activityEvent.Name, "exception", StringComparison.Ordinal));
        TestAssert.Contains(CatalogTelemetry.MetricIntegrationEvent, measurements, StringComparer.Ordinal);
        TestAssert.Contains(CatalogTelemetry.MetricIdempotencyOperation, measurements, StringComparer.Ordinal);
        TestAssert.Contains(CatalogTelemetry.MetricTourStreamUpdate, measurements, StringComparer.Ordinal);
    }

    [Fact]
    public async Task Unexpected_operation_cancelled_exception_emits_error_span_and_metrics()
    {
        // Arrange
        var stoppedActivities = new ConcurrentQueue<Activity>();
        var measurements = new ConcurrentQueue<string>();
        using var activityListener = CatalogTelemetryTestsHelpers.CreateActivityListener(stoppedActivities);
        using var meterListener = CatalogTelemetryTestsHelpers.CreateMeterListener(measurements);
        using var rootActivity = CatalogTelemetryTestsHelpers.StartRootActivity();
        var handler = new IdempotentIntegrationHandler<AdminTourCreatedIntegrationEvent>(
            new AdminTourCreatedIntegrationHandler(new UnexpectedCancelledEventStore()),
            new CapturingIdempotencyStore(),
            Options.Create(new IntegrationEventOptions()));
        var integrationEvent = new AdminTourCreatedIntegrationEvent(
            Guid.CreateVersion7(),
            DateTimeOffset.UtcNow,
            Guid.CreateVersion7(),
            "andes-2026",
            "Andes 2026");

        // Act
        var exception = await TestAssert.Throws<OperationCanceledException>(async () =>
            await handler.Handle(integrationEvent, TestContext.Current.CancellationToken));

        // Assert
        var handlingActivity = CatalogTelemetryTestsHelpers.SingleActivity(stoppedActivities, rootActivity, CatalogTelemetry.ActivityIntegrationEventHandle);
        var streamActivity = CatalogTelemetryTestsHelpers.SingleActivity(stoppedActivities, rootActivity, CatalogTelemetry.ActivityTourStreamUpdate);
        TestAssert.Equal(ActivityStatusCode.Error, handlingActivity.Status);
        TestAssert.Equal(ActivityStatusCode.Error, streamActivity.Status);
        TestAssert.Equal(exception.Message, handlingActivity.StatusDescription);
        TestAssert.Equal(exception.Message, streamActivity.StatusDescription);
        TestAssert.True(CatalogTelemetryTestsHelpers.HasTag(handlingActivity, CatalogTelemetry.TagOutcome, CatalogTelemetry.OutcomeError));
        TestAssert.True(CatalogTelemetryTestsHelpers.HasTag(streamActivity, CatalogTelemetry.TagOutcome, CatalogTelemetry.OutcomeError));
        TestAssert.Contains(CatalogTelemetry.MetricIntegrationEvent, measurements, StringComparer.Ordinal);
        TestAssert.Contains(CatalogTelemetry.MetricIdempotencyOperation, measurements, StringComparer.Ordinal);
        TestAssert.Contains(CatalogTelemetry.MetricTourStreamUpdate, measurements, StringComparer.Ordinal);
    }

    [Fact]
    public async Task Unexpected_idempotency_operation_cancelled_exception_emits_error_span_and_metrics()
    {
        // Arrange
        var stoppedActivities = new ConcurrentQueue<Activity>();
        var measurements = new ConcurrentQueue<string>();
        using var activityListener = CatalogTelemetryTestsHelpers.CreateActivityListener(stoppedActivities);
        using var meterListener = CatalogTelemetryTestsHelpers.CreateMeterListener(measurements);
        using var rootActivity = CatalogTelemetryTestsHelpers.StartRootActivity();
        var handler = new IdempotentIntegrationHandler<AdminTourCreatedIntegrationEvent>(
            new AdminTourCreatedIntegrationHandler(new CapturingEventStore()),
            new UnexpectedCancelledIdempotencyStore(),
            Options.Create(new IntegrationEventOptions()));
        var integrationEvent = new AdminTourCreatedIntegrationEvent(
            Guid.CreateVersion7(),
            DateTimeOffset.UtcNow,
            Guid.CreateVersion7(),
            "andes-2026",
            "Andes 2026");

        // Act
        var exception = await TestAssert.Throws<OperationCanceledException>(async () =>
            await handler.Handle(integrationEvent, TestContext.Current.CancellationToken));

        // Assert
        var handlingActivity = CatalogTelemetryTestsHelpers.SingleActivity(stoppedActivities, rootActivity, CatalogTelemetry.ActivityIntegrationEventHandle);
        TestAssert.Equal(ActivityStatusCode.Error, handlingActivity.Status);
        TestAssert.Equal(exception.Message, handlingActivity.StatusDescription);
        TestAssert.True(CatalogTelemetryTestsHelpers.HasTag(handlingActivity, CatalogTelemetry.TagOutcome, CatalogTelemetry.OutcomeError));
        TestAssert.Contains(CatalogTelemetry.MetricIntegrationEvent, measurements, StringComparer.Ordinal);
        TestAssert.Contains(CatalogTelemetry.MetricIdempotencyOperation, measurements, StringComparer.Ordinal);
        TestAssert.DoesNotContain(CatalogTelemetry.MetricTourStreamUpdate, measurements, StringComparer.Ordinal);
    }

    [Fact]
    public async Task Tour_stream_unexpected_operation_cancelled_exception_emits_error_span_and_metric()
    {
        // Arrange
        var stoppedActivities = new ConcurrentQueue<Activity>();
        var measurements = new ConcurrentQueue<string>();
        using var activityListener = CatalogTelemetryTestsHelpers.CreateActivityListener(stoppedActivities);
        using var meterListener = CatalogTelemetryTestsHelpers.CreateMeterListener(measurements);
        using var rootActivity = CatalogTelemetryTestsHelpers.StartRootActivity();
        var handler = new AdminTourCreatedIntegrationHandler(new UnexpectedCancelledEventStore());
        var integrationEvent = new AdminTourCreatedIntegrationEvent(
            Guid.CreateVersion7(),
            DateTimeOffset.UtcNow,
            Guid.CreateVersion7(),
            "andes-2026",
            "Andes 2026");

        // Act
        var exception = await TestAssert.Throws<OperationCanceledException>(async () =>
            await handler.Handle(integrationEvent, TestContext.Current.CancellationToken));

        // Assert
        var streamActivity = CatalogTelemetryTestsHelpers.SingleActivity(stoppedActivities, rootActivity, CatalogTelemetry.ActivityTourStreamUpdate);
        TestAssert.Equal(ActivityStatusCode.Error, streamActivity.Status);
        TestAssert.Equal(exception.Message, streamActivity.StatusDescription);
        TestAssert.True(CatalogTelemetryTestsHelpers.HasTag(streamActivity, CatalogTelemetry.TagOutcome, CatalogTelemetry.OutcomeError));
        TestAssert.Contains(CatalogTelemetry.MetricTourStreamUpdate, measurements, StringComparer.Ordinal);
        TestAssert.DoesNotContain(CatalogTelemetry.MetricIntegrationEvent, measurements, StringComparer.Ordinal);
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
        var exception = await TestAssert.Throws<InvalidOperationException>(async () =>
            await runner.Project(TestContext.Current.CancellationToken));

        // Assert
        var projectionActivity = CatalogTelemetryTestsHelpers.SingleActivity(stoppedActivities, rootActivity, CatalogTelemetry.ActivityProjectionProcess);
        TestAssert.Equal(ActivityStatusCode.Error, projectionActivity.Status);
        TestAssert.Equal(exception.Message, projectionActivity.StatusDescription);
        TestAssert.True(CatalogTelemetryTestsHelpers.HasTag(projectionActivity, CatalogTelemetry.TagProjectionName, projection.Name));
        TestAssert.True(CatalogTelemetryTestsHelpers.HasTag(projectionActivity, CatalogTelemetry.TagOutcome, CatalogTelemetry.OutcomeError));
        TestAssert.Contains(projectionActivity.Events, activityEvent => string.Equals(activityEvent.Name, "exception", StringComparison.Ordinal));
        TestAssert.Contains(CatalogTelemetry.MetricProjectionBatch, measurements, StringComparer.Ordinal);
    }

    [Fact]
    public async Task Unexpected_projection_operation_cancelled_exception_emits_error_span_and_metric()
    {
        // Arrange
        var stoppedActivities = new ConcurrentQueue<Activity>();
        var measurements = new ConcurrentQueue<string>();
        using var activityListener = CatalogTelemetryTestsHelpers.CreateActivityListener(stoppedActivities);
        using var meterListener = CatalogTelemetryTestsHelpers.CreateMeterListener(measurements);
        using var rootActivity = CatalogTelemetryTestsHelpers.StartRootActivity();
        var eventStore = new CapturingEventStore();
        var projection = new UnexpectedCancelledProjection();
        var runner = new CatalogProjectionRunner(eventStore, new CapturingProjectionCheckpointStore(), [projection]);
        var draftCreated = new CatalogTourDraftCreated(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            "andes-2026",
            "Andes 2026",
            Guid.CreateVersion7());
        eventStore.AddReplayEvent(CatalogTelemetryTestsHelpers.CreateEnvelope(1, draftCreated, DateTimeOffset.UtcNow));

        // Act
        var exception = await TestAssert.Throws<OperationCanceledException>(async () =>
            await runner.Project(TestContext.Current.CancellationToken));

        // Assert
        var projectionActivity = CatalogTelemetryTestsHelpers.SingleActivity(stoppedActivities, rootActivity, CatalogTelemetry.ActivityProjectionProcess);
        TestAssert.Equal(ActivityStatusCode.Error, projectionActivity.Status);
        TestAssert.Equal(exception.Message, projectionActivity.StatusDescription);
        TestAssert.True(CatalogTelemetryTestsHelpers.HasTag(projectionActivity, CatalogTelemetry.TagOutcome, CatalogTelemetry.OutcomeError));
        TestAssert.Contains(CatalogTelemetry.MetricProjectionBatch, measurements, StringComparer.Ordinal);
    }

    [Fact]
    public async Task Projection_loading_unexpected_operation_cancelled_exception_emits_error_span_and_metric()
    {
        // Arrange
        var stoppedActivities = new ConcurrentQueue<Activity>();
        var measurements = new ConcurrentQueue<string>();
        using var activityListener = CatalogTelemetryTestsHelpers.CreateActivityListener(stoppedActivities);
        using var meterListener = CatalogTelemetryTestsHelpers.CreateMeterListener(measurements);
        using var rootActivity = CatalogTelemetryTestsHelpers.StartRootActivity();
        var runner = new CatalogProjectionRunner(
            new UnexpectedCancelledEventStore(),
            new CapturingProjectionCheckpointStore(),
            [new CatalogTourReadModelProjection(new CapturingCatalogTourReadModelStore())]);

        // Act
        var exception = await TestAssert.Throws<OperationCanceledException>(async () =>
            await runner.Project(TestContext.Current.CancellationToken));

        // Assert
        var projectionActivity = CatalogTelemetryTestsHelpers.SingleActivity(stoppedActivities, rootActivity, CatalogTelemetry.ActivityProjectionProcess);
        TestAssert.Equal(ActivityStatusCode.Error, projectionActivity.Status);
        TestAssert.Equal(exception.Message, projectionActivity.StatusDescription);
        TestAssert.True(CatalogTelemetryTestsHelpers.HasTag(projectionActivity, CatalogTelemetry.TagOutcome, CatalogTelemetry.OutcomeError));
        TestAssert.Contains(CatalogTelemetry.MetricProjectionBatch, measurements, StringComparer.Ordinal);
    }

    [Fact]
    public async Task Integration_event_cancellation_does_not_emit_error_telemetry()
    {
        // Arrange
        var stoppedActivities = new ConcurrentQueue<Activity>();
        var measurements = new ConcurrentQueue<string>();
        using var activityListener = CatalogTelemetryTestsHelpers.CreateActivityListener(stoppedActivities);
        using var meterListener = CatalogTelemetryTestsHelpers.CreateMeterListener(measurements);
        using var rootActivity = CatalogTelemetryTestsHelpers.StartRootActivity();
        var handler = new IdempotentIntegrationHandler<AdminTourCreatedIntegrationEvent>(
            new AdminTourCreatedIntegrationHandler(new CancelledEventStore()),
            new CapturingIdempotencyStore(),
            Options.Create(new IntegrationEventOptions()));
        var integrationEvent = new AdminTourCreatedIntegrationEvent(
            Guid.CreateVersion7(),
            DateTimeOffset.UtcNow,
            Guid.CreateVersion7(),
            "andes-2026",
            "Andes 2026");
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        // Act
        await TestAssert.Throws<OperationCanceledException>(async () =>
            await handler.Handle(integrationEvent, cancellation.Token));

        // Assert
        var handlingActivity = CatalogTelemetryTestsHelpers.SingleActivity(stoppedActivities, rootActivity, CatalogTelemetry.ActivityIntegrationEventHandle);
        var streamActivity = CatalogTelemetryTestsHelpers.SingleActivity(stoppedActivities, rootActivity, CatalogTelemetry.ActivityTourStreamUpdate);
        TestAssert.Equal(ActivityStatusCode.Unset, handlingActivity.Status);
        TestAssert.Equal(ActivityStatusCode.Unset, streamActivity.Status);
        TestAssert.DoesNotContain(handlingActivity.Events, static activityEvent => string.Equals(activityEvent.Name, "exception", StringComparison.Ordinal));
        TestAssert.DoesNotContain(streamActivity.Events, static activityEvent => string.Equals(activityEvent.Name, "exception", StringComparison.Ordinal));
        TestAssert.DoesNotContain(CatalogTelemetry.MetricIntegrationEvent, measurements, StringComparer.Ordinal);
        TestAssert.DoesNotContain(CatalogTelemetry.MetricTourStreamUpdate, measurements, StringComparer.Ordinal);
    }

    [Fact]
    public async Task Idempotency_cancellation_does_not_emit_error_telemetry()
    {
        // Arrange
        var stoppedActivities = new ConcurrentQueue<Activity>();
        var measurements = new ConcurrentQueue<string>();
        using var activityListener = CatalogTelemetryTestsHelpers.CreateActivityListener(stoppedActivities);
        using var meterListener = CatalogTelemetryTestsHelpers.CreateMeterListener(measurements);
        using var rootActivity = CatalogTelemetryTestsHelpers.StartRootActivity();
        var handler = new IdempotentIntegrationHandler<AdminTourCreatedIntegrationEvent>(
            new AdminTourCreatedIntegrationHandler(new CapturingEventStore()),
            new CancelledIdempotencyStore(),
            Options.Create(new IntegrationEventOptions()));
        var integrationEvent = new AdminTourCreatedIntegrationEvent(
            Guid.CreateVersion7(),
            DateTimeOffset.UtcNow,
            Guid.CreateVersion7(),
            "andes-2026",
            "Andes 2026");
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        // Act
        await TestAssert.Throws<OperationCanceledException>(async () =>
            await handler.Handle(integrationEvent, cancellation.Token));

        // Assert
        var handlingActivity = CatalogTelemetryTestsHelpers.SingleActivity(stoppedActivities, rootActivity, CatalogTelemetry.ActivityIntegrationEventHandle);
        TestAssert.Equal(ActivityStatusCode.Unset, handlingActivity.Status);
        TestAssert.DoesNotContain(handlingActivity.Events, static activityEvent => string.Equals(activityEvent.Name, "exception", StringComparison.Ordinal));
        TestAssert.DoesNotContain(CatalogTelemetry.MetricIntegrationEvent, measurements, StringComparer.Ordinal);
        TestAssert.DoesNotContain(CatalogTelemetry.MetricIdempotencyOperation, measurements, StringComparer.Ordinal);
        TestAssert.DoesNotContain(CatalogTelemetry.MetricTourStreamUpdate, measurements, StringComparer.Ordinal);
    }

    [Fact]
    public async Task Tour_stream_cancellation_does_not_emit_error_telemetry()
    {
        // Arrange
        var stoppedActivities = new ConcurrentQueue<Activity>();
        var measurements = new ConcurrentQueue<string>();
        using var activityListener = CatalogTelemetryTestsHelpers.CreateActivityListener(stoppedActivities);
        using var meterListener = CatalogTelemetryTestsHelpers.CreateMeterListener(measurements);
        using var rootActivity = CatalogTelemetryTestsHelpers.StartRootActivity();
        var handler = new AdminTourCreatedIntegrationHandler(new CancelledEventStore());
        var integrationEvent = new AdminTourCreatedIntegrationEvent(
            Guid.CreateVersion7(),
            DateTimeOffset.UtcNow,
            Guid.CreateVersion7(),
            "andes-2026",
            "Andes 2026");
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        // Act
        await TestAssert.Throws<OperationCanceledException>(async () =>
            await handler.Handle(integrationEvent, cancellation.Token));

        // Assert
        var streamActivity = CatalogTelemetryTestsHelpers.SingleActivity(stoppedActivities, rootActivity, CatalogTelemetry.ActivityTourStreamUpdate);
        TestAssert.Equal(ActivityStatusCode.Unset, streamActivity.Status);
        TestAssert.DoesNotContain(streamActivity.Events, static activityEvent => string.Equals(activityEvent.Name, "exception", StringComparison.Ordinal));
        TestAssert.DoesNotContain(CatalogTelemetry.MetricTourStreamUpdate, measurements, StringComparer.Ordinal);
        TestAssert.DoesNotContain(CatalogTelemetry.MetricIntegrationEvent, measurements, StringComparer.Ordinal);
    }

    [Fact]
    public async Task Projection_cancellation_does_not_emit_error_telemetry()
    {
        // Arrange
        var stoppedActivities = new ConcurrentQueue<Activity>();
        var measurements = new ConcurrentQueue<string>();
        using var activityListener = CatalogTelemetryTestsHelpers.CreateActivityListener(stoppedActivities);
        using var meterListener = CatalogTelemetryTestsHelpers.CreateMeterListener(measurements);
        using var rootActivity = CatalogTelemetryTestsHelpers.StartRootActivity();
        var eventStore = new CapturingEventStore();
        var projection = new CancelledProjection();
        var runner = new CatalogProjectionRunner(eventStore, new CapturingProjectionCheckpointStore(), [projection]);
        var draftCreated = new CatalogTourDraftCreated(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            "andes-2026",
            "Andes 2026",
            Guid.CreateVersion7());
        eventStore.AddReplayEvent(CatalogTelemetryTestsHelpers.CreateEnvelope(1, draftCreated, DateTimeOffset.UtcNow));
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        // Act
        await TestAssert.Throws<OperationCanceledException>(async () =>
            await runner.Project(cancellation.Token));

        // Assert
        var projectionActivity = CatalogTelemetryTestsHelpers.SingleActivity(stoppedActivities, rootActivity, CatalogTelemetry.ActivityProjectionProcess);
        TestAssert.Equal(ActivityStatusCode.Unset, projectionActivity.Status);
        TestAssert.True(CatalogTelemetryTestsHelpers.HasTag(projectionActivity, CatalogTelemetry.TagProjectionName, projection.Name));
        TestAssert.DoesNotContain(projectionActivity.Events, static activityEvent => string.Equals(activityEvent.Name, "exception", StringComparison.Ordinal));
        TestAssert.DoesNotContain(CatalogTelemetry.MetricProjectionBatch, measurements, StringComparer.Ordinal);
        TestAssert.DoesNotContain(CatalogTelemetry.MetricProjectionEvent, measurements, StringComparer.Ordinal);
    }

}
