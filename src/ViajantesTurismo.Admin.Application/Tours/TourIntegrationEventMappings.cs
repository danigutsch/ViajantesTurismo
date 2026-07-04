using SharedKernel.IntegrationEvents;
using ViajantesTurismo.Admin.Contracts.Tours;
using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.Admin.Application.Tours;

internal static class TourIntegrationEventMappings
{
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
