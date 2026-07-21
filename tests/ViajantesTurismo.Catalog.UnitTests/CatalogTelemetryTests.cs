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
using ViajantesTurismo.Catalog.Testing.Infrastructure;

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
            new AdminTourCreatedIntegrationHandler(new CapturingEventStore(), new TestCatalogTourSlugLock()),
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
        (CatalogTelemetryTestsHelpers.HasTag(handlingActivity, CatalogTelemetry.TagIntegrationEventType, AdminTourCreatedIntegrationEvent.EventType)).ShouldBeTrue();
        (CatalogTelemetryTestsHelpers.HasTag(handlingActivity, CatalogTelemetry.TagOutcome, CatalogTelemetry.OutcomeSuccess)).ShouldBeTrue();
        (measurements).ShouldContain(CatalogTelemetry.MetricIntegrationEvent, StringComparer.Ordinal);
        (measurements).ShouldContain(CatalogTelemetry.MetricIdempotencyOperation, StringComparer.Ordinal);
        (measurements).ShouldContain(CatalogTelemetry.MetricTourStreamUpdate, StringComparer.Ordinal);
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
            new AdminTourCreatedIntegrationHandler(new CapturingEventStore(), new TestCatalogTourSlugLock()),
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
        (CatalogTelemetryTestsHelpers.HasTag(handlingActivity, CatalogTelemetry.TagOutcome, CatalogTelemetry.OutcomeSkipped)).ShouldBeTrue();
        (CatalogTelemetryTestsHelpers.HasTag(handlingActivity, CatalogTelemetry.TagIdempotencyOutcome, CatalogTelemetry.OutcomeSkipped)).ShouldBeTrue();
        (measurements).ShouldContain(CatalogTelemetry.MetricIdempotencyOperation, StringComparer.Ordinal);
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
            Guid.CreateVersion7(),
            "andes-2026");
        eventStore.AddReplayEvent(CatalogTelemetryTestsHelpers.CreateEnvelope(11, draftCreated, DateTimeOffset.UtcNow));

        // Act
        await runner.Project(TestContext.Current.CancellationToken);

        // Assert
        var projectionActivity = CatalogTelemetryTestsHelpers.SingleActivity(stoppedActivities, rootActivity, CatalogTelemetry.ActivityProjectionProcess);
        (CatalogTelemetryTestsHelpers.HasTag(projectionActivity, CatalogTelemetry.TagProjectionName, projection.Name)).ShouldBeTrue();
        (CatalogTelemetryTestsHelpers.HasTag(projectionActivity, CatalogTelemetry.TagEventCount, 1)).ShouldBeTrue();
        (CatalogTelemetryTestsHelpers.HasTag(projectionActivity, CatalogTelemetry.TagCheckpointPosition, 11L)).ShouldBeTrue();
        (CatalogTelemetryTestsHelpers.HasTag(projectionActivity, CatalogTelemetry.TagOutcome, CatalogTelemetry.OutcomeSuccess)).ShouldBeTrue();
        (measurements).ShouldContain(CatalogTelemetry.MetricProjectionEvent, StringComparer.Ordinal);
        (measurements).ShouldContain(CatalogTelemetry.MetricProjectionBatch, StringComparer.Ordinal);
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
            new AdminTourCreatedIntegrationHandler(new ThrowingEventStore(), new TestCatalogTourSlugLock()),
            new CapturingIdempotencyStore(),
            Options.Create(new IntegrationEventOptions()));
        var integrationEvent = new AdminTourCreatedIntegrationEvent(
            Guid.CreateVersion7(),
            DateTimeOffset.UtcNow,
            Guid.CreateVersion7(),
            "andes-2026",
            "Andes 2026");

        // Act
        var exception = await ((Func<Task>)(async () =>
            await handler.Handle(integrationEvent, TestContext.Current.CancellationToken))).ShouldThrow<InvalidOperationException>();

        // Assert
        var handlingActivity = CatalogTelemetryTestsHelpers.SingleActivity(stoppedActivities, rootActivity, CatalogTelemetry.ActivityIntegrationEventHandle);
        (handlingActivity.Status).ShouldBe(ActivityStatusCode.Error);
        (handlingActivity.StatusDescription).ShouldBe(exception.Message);
        (CatalogTelemetryTestsHelpers.HasTag(handlingActivity, CatalogTelemetry.TagOutcome, CatalogTelemetry.OutcomeError)).ShouldBeTrue();
        (handlingActivity.Events).ShouldContain(activityEvent => string.Equals(activityEvent.Name, "exception", StringComparison.Ordinal));
        (measurements).ShouldContain(CatalogTelemetry.MetricIntegrationEvent, StringComparer.Ordinal);
        (measurements).ShouldContain(CatalogTelemetry.MetricIdempotencyOperation, StringComparer.Ordinal);
        (measurements).ShouldContain(CatalogTelemetry.MetricTourStreamUpdate, StringComparer.Ordinal);
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
            new AdminTourCreatedIntegrationHandler(new UnexpectedCancelledEventStore(), new TestCatalogTourSlugLock()),
            new CapturingIdempotencyStore(),
            Options.Create(new IntegrationEventOptions()));
        var integrationEvent = new AdminTourCreatedIntegrationEvent(
            Guid.CreateVersion7(),
            DateTimeOffset.UtcNow,
            Guid.CreateVersion7(),
            "andes-2026",
            "Andes 2026");

        // Act
        var exception = await ((Func<Task>)(async () =>
            await handler.Handle(integrationEvent, TestContext.Current.CancellationToken))).ShouldThrow<OperationCanceledException>();

        // Assert
        var handlingActivity = CatalogTelemetryTestsHelpers.SingleActivity(stoppedActivities, rootActivity, CatalogTelemetry.ActivityIntegrationEventHandle);
        var streamActivity = CatalogTelemetryTestsHelpers.SingleActivity(stoppedActivities, rootActivity, CatalogTelemetry.ActivityTourStreamUpdate);
        (handlingActivity.Status).ShouldBe(ActivityStatusCode.Error);
        (streamActivity.Status).ShouldBe(ActivityStatusCode.Error);
        (handlingActivity.StatusDescription).ShouldBe(exception.Message);
        (streamActivity.StatusDescription).ShouldBe(exception.Message);
        (CatalogTelemetryTestsHelpers.HasTag(handlingActivity, CatalogTelemetry.TagOutcome, CatalogTelemetry.OutcomeError)).ShouldBeTrue();
        (CatalogTelemetryTestsHelpers.HasTag(streamActivity, CatalogTelemetry.TagOutcome, CatalogTelemetry.OutcomeError)).ShouldBeTrue();
        (measurements).ShouldContain(CatalogTelemetry.MetricIntegrationEvent, StringComparer.Ordinal);
        (measurements).ShouldContain(CatalogTelemetry.MetricIdempotencyOperation, StringComparer.Ordinal);
        (measurements).ShouldContain(CatalogTelemetry.MetricTourStreamUpdate, StringComparer.Ordinal);
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
            new AdminTourCreatedIntegrationHandler(new CapturingEventStore(), new TestCatalogTourSlugLock()),
            new UnexpectedCancelledIdempotencyStore(),
            Options.Create(new IntegrationEventOptions()));
        var integrationEvent = new AdminTourCreatedIntegrationEvent(
            Guid.CreateVersion7(),
            DateTimeOffset.UtcNow,
            Guid.CreateVersion7(),
            "andes-2026",
            "Andes 2026");

        // Act
        var exception = await ((Func<Task>)(async () =>
            await handler.Handle(integrationEvent, TestContext.Current.CancellationToken))).ShouldThrow<OperationCanceledException>();

        // Assert
        var handlingActivity = CatalogTelemetryTestsHelpers.SingleActivity(stoppedActivities, rootActivity, CatalogTelemetry.ActivityIntegrationEventHandle);
        (handlingActivity.Status).ShouldBe(ActivityStatusCode.Error);
        (handlingActivity.StatusDescription).ShouldBe(exception.Message);
        (CatalogTelemetryTestsHelpers.HasTag(handlingActivity, CatalogTelemetry.TagOutcome, CatalogTelemetry.OutcomeError)).ShouldBeTrue();
        (measurements).ShouldContain(CatalogTelemetry.MetricIntegrationEvent, StringComparer.Ordinal);
        (measurements).ShouldContain(CatalogTelemetry.MetricIdempotencyOperation, StringComparer.Ordinal);
        (measurements).ShouldNotContain(CatalogTelemetry.MetricTourStreamUpdate, StringComparer.Ordinal);
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
        var handler = new AdminTourCreatedIntegrationHandler(new UnexpectedCancelledEventStore(), new TestCatalogTourSlugLock());
        var integrationEvent = new AdminTourCreatedIntegrationEvent(
            Guid.CreateVersion7(),
            DateTimeOffset.UtcNow,
            Guid.CreateVersion7(),
            "andes-2026",
            "Andes 2026");

        // Act
        var exception = await ((Func<Task>)(async () =>
            await handler.Handle(integrationEvent, TestContext.Current.CancellationToken))).ShouldThrow<OperationCanceledException>();

        // Assert
        var streamActivity = CatalogTelemetryTestsHelpers.SingleActivity(stoppedActivities, rootActivity, CatalogTelemetry.ActivityTourStreamUpdate);
        (streamActivity.Status).ShouldBe(ActivityStatusCode.Error);
        (streamActivity.StatusDescription).ShouldBe(exception.Message);
        (CatalogTelemetryTestsHelpers.HasTag(streamActivity, CatalogTelemetry.TagOutcome, CatalogTelemetry.OutcomeError)).ShouldBeTrue();
        (measurements).ShouldContain(CatalogTelemetry.MetricTourStreamUpdate, StringComparer.Ordinal);
        (measurements).ShouldNotContain(CatalogTelemetry.MetricIntegrationEvent, StringComparer.Ordinal);
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
            Guid.CreateVersion7(),
            "andes-2026");
        eventStore.AddReplayEvent(CatalogTelemetryTestsHelpers.CreateEnvelope(1, draftCreated, DateTimeOffset.UtcNow));

        // Act
        var exception = await ((Func<Task>)(async () =>
            await runner.Project(TestContext.Current.CancellationToken))).ShouldThrow<InvalidOperationException>();

        // Assert
        var projectionActivity = CatalogTelemetryTestsHelpers.SingleActivity(stoppedActivities, rootActivity, CatalogTelemetry.ActivityProjectionProcess);
        (projectionActivity.Status).ShouldBe(ActivityStatusCode.Error);
        (projectionActivity.StatusDescription).ShouldBe(exception.Message);
        (CatalogTelemetryTestsHelpers.HasTag(projectionActivity, CatalogTelemetry.TagProjectionName, projection.Name)).ShouldBeTrue();
        (CatalogTelemetryTestsHelpers.HasTag(projectionActivity, CatalogTelemetry.TagOutcome, CatalogTelemetry.OutcomeError)).ShouldBeTrue();
        (projectionActivity.Events).ShouldContain(activityEvent => string.Equals(activityEvent.Name, "exception", StringComparison.Ordinal));
        (measurements).ShouldContain(CatalogTelemetry.MetricProjectionBatch, StringComparer.Ordinal);
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
            Guid.CreateVersion7(),
            "andes-2026");
        eventStore.AddReplayEvent(CatalogTelemetryTestsHelpers.CreateEnvelope(1, draftCreated, DateTimeOffset.UtcNow));

        // Act
        var exception = await ((Func<Task>)(async () =>
            await runner.Project(TestContext.Current.CancellationToken))).ShouldThrow<OperationCanceledException>();

        // Assert
        var projectionActivity = CatalogTelemetryTestsHelpers.SingleActivity(stoppedActivities, rootActivity, CatalogTelemetry.ActivityProjectionProcess);
        (projectionActivity.Status).ShouldBe(ActivityStatusCode.Error);
        (projectionActivity.StatusDescription).ShouldBe(exception.Message);
        (CatalogTelemetryTestsHelpers.HasTag(projectionActivity, CatalogTelemetry.TagOutcome, CatalogTelemetry.OutcomeError)).ShouldBeTrue();
        (measurements).ShouldContain(CatalogTelemetry.MetricProjectionBatch, StringComparer.Ordinal);
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
        var exception = await ((Func<Task>)(async () =>
            await runner.Project(TestContext.Current.CancellationToken))).ShouldThrow<OperationCanceledException>();

        // Assert
        var projectionActivity = CatalogTelemetryTestsHelpers.SingleActivity(stoppedActivities, rootActivity, CatalogTelemetry.ActivityProjectionProcess);
        (projectionActivity.Status).ShouldBe(ActivityStatusCode.Error);
        (projectionActivity.StatusDescription).ShouldBe(exception.Message);
        (CatalogTelemetryTestsHelpers.HasTag(projectionActivity, CatalogTelemetry.TagOutcome, CatalogTelemetry.OutcomeError)).ShouldBeTrue();
        (measurements).ShouldContain(CatalogTelemetry.MetricProjectionBatch, StringComparer.Ordinal);
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
            new AdminTourCreatedIntegrationHandler(new CancelledEventStore(), new TestCatalogTourSlugLock()),
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
        await ((Func<Task>)(async () =>
            await handler.Handle(integrationEvent, cancellation.Token))).ShouldThrow<OperationCanceledException>();

        // Assert
        var handlingActivity = CatalogTelemetryTestsHelpers.SingleActivity(stoppedActivities, rootActivity, CatalogTelemetry.ActivityIntegrationEventHandle);
        var streamActivity = CatalogTelemetryTestsHelpers.SingleActivity(stoppedActivities, rootActivity, CatalogTelemetry.ActivityTourStreamUpdate);
        (handlingActivity.Status).ShouldBe(ActivityStatusCode.Unset);
        (streamActivity.Status).ShouldBe(ActivityStatusCode.Unset);
        (handlingActivity.Events).ShouldNotContain(static activityEvent => string.Equals(activityEvent.Name, "exception", StringComparison.Ordinal));
        (streamActivity.Events).ShouldNotContain(static activityEvent => string.Equals(activityEvent.Name, "exception", StringComparison.Ordinal));
        (measurements).ShouldNotContain(CatalogTelemetry.MetricIntegrationEvent, StringComparer.Ordinal);
        (measurements).ShouldNotContain(CatalogTelemetry.MetricTourStreamUpdate, StringComparer.Ordinal);
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
            new AdminTourCreatedIntegrationHandler(new CapturingEventStore(), new TestCatalogTourSlugLock()),
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
        await ((Func<Task>)(async () =>
            await handler.Handle(integrationEvent, cancellation.Token))).ShouldThrow<OperationCanceledException>();

        // Assert
        var handlingActivity = CatalogTelemetryTestsHelpers.SingleActivity(stoppedActivities, rootActivity, CatalogTelemetry.ActivityIntegrationEventHandle);
        (handlingActivity.Status).ShouldBe(ActivityStatusCode.Unset);
        (handlingActivity.Events).ShouldNotContain(static activityEvent => string.Equals(activityEvent.Name, "exception", StringComparison.Ordinal));
        (measurements).ShouldNotContain(CatalogTelemetry.MetricIntegrationEvent, StringComparer.Ordinal);
        (measurements).ShouldNotContain(CatalogTelemetry.MetricIdempotencyOperation, StringComparer.Ordinal);
        (measurements).ShouldNotContain(CatalogTelemetry.MetricTourStreamUpdate, StringComparer.Ordinal);
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
        var handler = new AdminTourCreatedIntegrationHandler(new CancelledEventStore(), new TestCatalogTourSlugLock());
        var integrationEvent = new AdminTourCreatedIntegrationEvent(
            Guid.CreateVersion7(),
            DateTimeOffset.UtcNow,
            Guid.CreateVersion7(),
            "andes-2026",
            "Andes 2026");
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        // Act
        await ((Func<Task>)(async () =>
            await handler.Handle(integrationEvent, cancellation.Token))).ShouldThrow<OperationCanceledException>();

        // Assert
        var streamActivity = CatalogTelemetryTestsHelpers.SingleActivity(stoppedActivities, rootActivity, CatalogTelemetry.ActivityTourStreamUpdate);
        (streamActivity.Status).ShouldBe(ActivityStatusCode.Unset);
        (streamActivity.Events).ShouldNotContain(static activityEvent => string.Equals(activityEvent.Name, "exception", StringComparison.Ordinal));
        (measurements).ShouldNotContain(CatalogTelemetry.MetricTourStreamUpdate, StringComparer.Ordinal);
        (measurements).ShouldNotContain(CatalogTelemetry.MetricIntegrationEvent, StringComparer.Ordinal);
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
            Guid.CreateVersion7(),
            "andes-2026");
        eventStore.AddReplayEvent(CatalogTelemetryTestsHelpers.CreateEnvelope(1, draftCreated, DateTimeOffset.UtcNow));
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        // Act
        await ((Func<Task>)(async () =>
            await runner.Project(cancellation.Token))).ShouldThrow<OperationCanceledException>();

        // Assert
        var projectionActivity = CatalogTelemetryTestsHelpers.SingleActivity(stoppedActivities, rootActivity, CatalogTelemetry.ActivityProjectionProcess);
        (projectionActivity.Status).ShouldBe(ActivityStatusCode.Unset);
        (CatalogTelemetryTestsHelpers.HasTag(projectionActivity, CatalogTelemetry.TagProjectionName, projection.Name)).ShouldBeTrue();
        (projectionActivity.Events).ShouldNotContain(static activityEvent => string.Equals(activityEvent.Name, "exception", StringComparison.Ordinal));
        (measurements).ShouldNotContain(CatalogTelemetry.MetricProjectionBatch, StringComparer.Ordinal);
        (measurements).ShouldNotContain(CatalogTelemetry.MetricProjectionEvent, StringComparer.Ordinal);
    }

}
