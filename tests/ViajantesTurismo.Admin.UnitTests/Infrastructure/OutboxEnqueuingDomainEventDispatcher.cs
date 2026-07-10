using SharedKernel.Messaging.IntegrationEvents.EntityFrameworkCore;
using SharedKernel.Testing.Assertions;
using ViajantesTurismo.Admin.Contracts.IntegrationEvents.Tours;
using ViajantesTurismo.Admin.Domain.Tours;
using ViajantesTurismo.Admin.Infrastructure;
using ViajantesTurismo.Admin.Testing.Fakes;

namespace ViajantesTurismo.Admin.UnitTests.Infrastructure;

internal sealed class OutboxEnqueuingDomainEventDispatcher(TimeProvider timeProvider) : CapturingDomainEventDispatcher
{
    public AdminWriteDbContext? DbContext { get; set; }

    public override async ValueTask Dispatch<TDomainEvent>(TDomainEvent domainEvent, CancellationToken ct)
    {
        await Dispatch((SharedKernel.Domain.IDomainEvent)domainEvent, ct);
    }

    public override async ValueTask Dispatch(SharedKernel.Domain.IDomainEvent domainEvent, CancellationToken ct)
    {
        await base.Dispatch(domainEvent, ct);

        if (domainEvent is not TourCreatedDomainEvent tourCreated)
        {
            return;
        }

        var dbContext = DbContext.ShouldNotBeNull();
        var outbox = new EfIntegrationEventOutbox<AdminWriteDbContext>(
            dbContext,
            timeProvider,
            RegisteredIntegrationEventSerializerTestServices.CreateSerializer());
        await outbox.Enqueue(
            new AdminTourCreatedIntegrationEvent(
                Guid.CreateVersion7(),
                timeProvider.GetUtcNow(),
                tourCreated.TourId,
                tourCreated.Identifier,
                tourCreated.Name),
            ct);
    }
}
