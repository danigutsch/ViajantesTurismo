using ViajantesTurismo.Admin.Contracts.IntegrationEvents.Tours;
using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.Admin.UnitTests.Application.DomainEvents;

public sealed class TourCreatedDomainEventHandlerTests
{
    [Fact]
    public async Task Dispatcher_maps_tour_created_domain_event_to_admin_tour_created_integration_event()
    {
        var now = new DateTimeOffset(2026, 6, 22, 12, 30, 0, TimeSpan.Zero);
        using var dispatchScope = TestDomainEventDispatchScope.Create(now);
        var tourId = Guid.CreateVersion7();
        var domainEvent = new TourCreatedDomainEvent(tourId, "andes-2026", "Andes 2026");

        await dispatchScope.Dispatcher.Dispatch(domainEvent, CancellationToken.None);

        var integrationEvent = dispatchScope.Outbox.IntegrationEvent.ShouldBeOfType<AdminTourCreatedIntegrationEvent>();
        integrationEvent.AdminTourId.ShouldBe(tourId);
        integrationEvent.Identifier.ShouldBe(domainEvent.Identifier);
        integrationEvent.Name.ShouldBe(domainEvent.Name);
        integrationEvent.OccurredAt.ShouldBe(now);
        integrationEvent.EventId.ShouldNotBe(Guid.Empty);
    }
}
