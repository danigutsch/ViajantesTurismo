using PandaBear.Messaging.IntegrationEvents;

namespace ViajantesTurismo.Admin.Contracts.IntegrationEvents.Tours;

/// <summary>Records the committed retirement of an Admin tour.</summary>
/// <param name="EventId">The stable event identifier for idempotent delivery.</param>
/// <param name="OccurredAt">The UTC instant of the retirement.</param>
/// <param name="AdminTourId">The Admin tour identifier.</param>
/// <param name="RetirementId">The identifier of the retirement operation.</param>
public sealed record AdminTourRetiredIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid AdminTourId,
    Guid RetirementId) : IIntegrationEvent
{
    /// <inheritdoc />
    public static string EventType => "admin.tour.retired";

    /// <inheritdoc />
    public static int EventVersion => 1;
}
