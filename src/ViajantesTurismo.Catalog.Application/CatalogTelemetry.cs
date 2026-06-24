using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace ViajantesTurismo.Catalog.Application;

/// <summary>
/// Defines stable telemetry names emitted by the Catalog bounded context.
/// </summary>
public static class CatalogTelemetry
{
    /// <summary>
    /// Gets the shared activity source and meter name for Catalog operations.
    /// </summary>
    public static string Name => "ViajantesTurismo.Catalog";

    /// <summary>
    /// Gets the activity name for Catalog integration-event handling.
    /// </summary>
    public static string ActivityIntegrationEventHandle => "catalog.integration_event.handle";

    /// <summary>
    /// Gets the activity name for Catalog tour stream updates.
    /// </summary>
    public static string ActivityTourStreamUpdate => "catalog.tour.stream_update";

    /// <summary>
    /// Gets the activity name for Catalog projection processing.
    /// </summary>
    public static string ActivityProjectionProcess => "catalog.projection.process";

    /// <summary>
    /// Gets the counter name for Catalog integration events.
    /// </summary>
    public static string MetricIntegrationEvent => "catalog.integration_event";

    /// <summary>
    /// Gets the counter name for Catalog idempotency outcomes.
    /// </summary>
    public static string MetricIdempotencyOperation => "catalog.idempotency.operation";

    /// <summary>
    /// Gets the counter name for Catalog tour stream updates.
    /// </summary>
    public static string MetricTourStreamUpdate => "catalog.tour.stream_update";

    /// <summary>
    /// Gets the counter name for processed projection events.
    /// </summary>
    public static string MetricProjectionEvent => "catalog.projection.event";

    /// <summary>
    /// Gets the counter name for projection processing batches.
    /// </summary>
    public static string MetricProjectionBatch => "catalog.projection.batch";

    /// <summary>
    /// Gets the bounded-context tag name.
    /// </summary>
    public static string TagBoundedContext => "catalog.bounded_context";

    /// <summary>
    /// Gets the integration event type tag name.
    /// </summary>
    public static string TagIntegrationEventType => "catalog.integration_event.type";

    /// <summary>
    /// Gets the integration event version tag name.
    /// </summary>
    public static string TagIntegrationEventVersion => "catalog.integration_event.version";

    /// <summary>
    /// Gets the idempotency outcome tag name.
    /// </summary>
    public static string TagIdempotencyOutcome => "catalog.idempotency.outcome";

    /// <summary>
    /// Gets the operation outcome tag name.
    /// </summary>
    public static string TagOutcome => "catalog.outcome";

    /// <summary>
    /// Gets the projection name tag name.
    /// </summary>
    public static string TagProjectionName => "catalog.projection.name";

    /// <summary>
    /// Gets the event count tag name.
    /// </summary>
    public static string TagEventCount => "catalog.event.count";

    /// <summary>
    /// Gets the checkpoint position tag name.
    /// </summary>
    public static string TagCheckpointPosition => "catalog.checkpoint.position";

    /// <summary>
    /// Gets the success outcome value.
    /// </summary>
    public static string OutcomeSuccess => "success";

    /// <summary>
    /// Gets the skipped outcome value.
    /// </summary>
    public static string OutcomeSkipped => "skipped";

    /// <summary>
    /// Gets the acquired idempotency outcome value.
    /// </summary>
    public static string OutcomeAcquired => "acquired";

    /// <summary>
    /// Gets the completed idempotency outcome value.
    /// </summary>
    public static string OutcomeCompleted => "completed";

    /// <summary>
    /// Gets the error outcome value.
    /// </summary>
    public static string OutcomeError => "error";

    internal static ActivitySource ActivitySource { get; } = new(Name);

    internal static Meter Meter { get; } = new(Name);

    internal static Counter<long> IntegrationEvents { get; } = Meter.CreateCounter<long>(
        MetricIntegrationEvent,
        unit: "{event}",
        description: "Number of Catalog integration events handled, skipped, or failed.");

    internal static Counter<long> IdempotencyOperations { get; } = Meter.CreateCounter<long>(
        MetricIdempotencyOperation,
        unit: "{operation}",
        description: "Number of Catalog integration-event idempotency outcomes.");

    internal static Counter<long> TourStreamUpdates { get; } = Meter.CreateCounter<long>(
        MetricTourStreamUpdate,
        unit: "{operation}",
        description: "Number of Catalog tour event-stream updates.");

    internal static Counter<long> ProjectionEvents { get; } = Meter.CreateCounter<long>(
        MetricProjectionEvent,
        unit: "{event}",
        description: "Number of events processed by Catalog projections.");

    internal static Counter<long> ProjectionBatches { get; } = Meter.CreateCounter<long>(
        MetricProjectionBatch,
        unit: "{batch}",
        description: "Number of Catalog projection batches processed.");
}
