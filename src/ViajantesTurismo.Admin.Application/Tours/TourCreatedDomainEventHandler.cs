using SharedKernel.DomainEvents;
using ViajantesTurismo.Admin.Contracts.Tours;
using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.Admin.Application.Tours;

internal sealed class TourCreatedDomainEventHandler(
    IIntegrationEventOutbox integrationEventOutbox,
    TimeProvider timeProvider) : IDomainEventHandler<TourCreatedDomainEvent>
{
    public ValueTask Handle(TourCreatedDomainEvent domainEvent, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        return integrationEventOutbox.Enqueue(
            new AdminTourCreatedIntegrationEvent(
                Guid.CreateVersion7(),
                timeProvider.GetUtcNow(),
                domainEvent.TourId,
                domainEvent.Identifier,
                domainEvent.Name),
            ct);
    }
}
