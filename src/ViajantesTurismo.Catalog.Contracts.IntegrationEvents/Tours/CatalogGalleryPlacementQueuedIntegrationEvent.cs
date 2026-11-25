using SharedKernel.Messaging.IntegrationEvents;

namespace ViajantesTurismo.Catalog.Contracts.IntegrationEvents.Tours;

/// <summary>Signals that Catalog has a committed placement intent to process.</summary>
/// <param name="EventId">The durable identity of an accepted intent or the committed bootstrap event.</param>
/// <param name="OccurredAt">The time the intent was queued.</param>
/// <param name="CatalogTourId">The affected tour.</param>
/// <param name="Sequence">The per-tour upper bound this wake-up can drain.</param>
public sealed record CatalogGalleryPlacementQueuedIntegrationEvent(
    Guid EventId, DateTimeOffset OccurredAt, Guid CatalogTourId, long Sequence) : IIntegrationEvent
{
    /// <inheritdoc />
    public static string EventType => "catalog.gallery-placement.queued";

    /// <inheritdoc />
    public static int EventVersion => 1;
}
