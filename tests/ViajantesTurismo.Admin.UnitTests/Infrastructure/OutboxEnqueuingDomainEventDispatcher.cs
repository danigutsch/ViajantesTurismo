using PandaBear.Messaging.IntegrationEvents;
using ViajantesTurismo.Admin.Contracts.IntegrationEvents.Tours;
using ViajantesTurismo.Admin.Domain.Tours;
using ViajantesTurismo.Admin.Testing.Fakes;

namespace ViajantesTurismo.Admin.UnitTests.Infrastructure;

internal sealed class OutboxEnqueuingDomainEventDispatcher(TimeProvider timeProvider) : CapturingDomainEventDispatcher
{
    public TimeProvider TimeProvider { get; } = timeProvider;

    public IIntegrationEventOutbox? Outbox
    {
        get; set;
    }

    public override async ValueTask Dispatch<TDomainEvent>(TDomainEvent domainEvent, CancellationToken ct)
    {
        await Dispatch((PandaBear.Domain.IDomainEvent)domainEvent, ct);
    }

    public override async ValueTask Dispatch(PandaBear.Domain.IDomainEvent domainEvent, CancellationToken ct)
    {
        await base.Dispatch(domainEvent, ct);

        if (domainEvent is not TourCreatedDomainEvent tourCreated)
        {
            return;
        }

        var outbox = Outbox.ShouldNotBeNull();
        await outbox.Enqueue(
            new AdminTourCreatedIntegrationEvent(
                Guid.CreateVersion7(),
                TimeProvider.GetUtcNow(),
                tourCreated.TourId,
                tourCreated.Identifier,
                tourCreated.Name),
            ct);
    }
}
