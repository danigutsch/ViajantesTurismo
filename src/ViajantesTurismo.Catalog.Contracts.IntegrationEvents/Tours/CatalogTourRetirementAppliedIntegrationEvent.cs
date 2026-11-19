using PandaBear.Messaging.IntegrationEvents;

namespace ViajantesTurismo.Catalog.Contracts.IntegrationEvents.Tours;

/// <summary>Confirms that Catalog applied the withdrawal of a retired Admin tour.</summary>
/// <param name="EventId">The stable event identifier for idempotent delivery.</param>
/// <param name="OccurredAt">The UTC instant when Catalog applied the retirement.</param>
/// <param name="AdminTourId">The Admin tour identifier.</param>
/// <param name="RetirementId">The identifier of the originating retirement operation.</param>
public sealed record CatalogTourRetirementAppliedIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid AdminTourId,
    Guid RetirementId) : IIntegrationEvent
{
    /// <inheritdoc />
    public static string EventType => "catalog.tour.retirement-applied";

    /// <inheritdoc />
    public static int EventVersion => 1;
}
