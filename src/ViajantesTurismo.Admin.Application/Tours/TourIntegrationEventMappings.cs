using PandaBear.Messaging.IntegrationEvents;
using ViajantesTurismo.Admin.Contracts.IntegrationEvents.Tours;
using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.Admin.Application.Tours;

/// <summary>Maps tour domain events to integration events.</summary>
internal static class TourIntegrationEventMappings
{
    /// <summary>Maps a tour created domain event to its integration event.</summary>
    [IntegrationEventMapping]
    public static AdminTourCreatedIntegrationEvent MapTourCreated(
        TourCreatedDomainEvent domainEvent,
        Guid eventId,
        DateTimeOffset occurredAt)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        return new AdminTourCreatedIntegrationEvent(
            eventId,
            occurredAt,
            domainEvent.TourId,
            domainEvent.Identifier,
            domainEvent.Name);
    }
}
