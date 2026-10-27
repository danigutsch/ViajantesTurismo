using PandaBear.Messaging.IntegrationEvents;
using ViajantesTurismo.Admin.Contracts.IntegrationEvents.Tours;
using ViajantesTurismo.Admin.Domain.Tours;
using ViajantesTurismo.Admin.Testing.Fakes;

namespace ViajantesTurismo.Admin.UnitTests.Infrastructure;

/// <summary>
/// Captures dispatched domain events and enqueues an admin tour-created integration event for each tour-created event.
/// </summary>
/// <param name="timeProvider">The clock used to stamp enqueued integration events.</param>
internal sealed class OutboxEnqueuingDomainEventDispatcher(TimeProvider timeProvider) : CapturingDomainEventDispatcher
{
    /// <summary>
    /// Gets the clock used to stamp enqueued integration events.
    /// </summary>
    public TimeProvider TimeProvider { get; } = timeProvider;

    /// <summary>
    /// Gets or sets the outbox that receives enqueued integration events; must be set before a tour-created event is dispatched.
    /// </summary>
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
