using SharedKernel.IntegrationEvents;

namespace ViajantesTurismo.Admin.Contracts.Tours;

/// <summary>
/// Published when an Admin tour is created and Catalog should create a draft presentation stream.
/// </summary>
/// <param name="EventId">The stable event identifier.</param>
/// <param name="OccurredAt">The UTC instant when the event occurred.</param>
/// <param name="AdminTourId">The Admin tour identifier.</param>
/// <param name="Identifier">The Admin tour business identifier.</param>
/// <param name="Name">The Admin tour name.</param>
public sealed record AdminTourCreatedIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid AdminTourId,
    string Identifier,
    string Name) : IIntegrationEvent
{
    /// <inheritdoc />
    public static string EventType => "admin.tour.created";

    /// <inheritdoc />
    public static int EventVersion => 1;
}
