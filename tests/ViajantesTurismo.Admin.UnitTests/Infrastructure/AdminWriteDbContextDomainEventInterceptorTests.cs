using SharedKernel.EntityFrameworkCore;
using SharedKernel.Testing.Assertions;
using ViajantesTurismo.Admin.Contracts.Tours;
using ViajantesTurismo.Admin.Domain.Tours;
using ViajantesTurismo.Admin.Testing.Behavior;
using ViajantesTurismo.Admin.Testing.Fakes;

namespace ViajantesTurismo.Admin.UnitTests.Infrastructure;

public sealed class AdminWriteDbContextDomainEventInterceptorTests
{
    [Fact]
    public async Task SaveEntities_dispatches_domain_events_and_persists_outbox_messages_in_the_same_save()
    {
        var dispatcher = new OutboxEnqueuingDomainEventDispatcher(new FakeTimeProvider(
            new DateTimeOffset(2026, 6, 22, 12, 30, 0, TimeSpan.Zero)));
        await using var scope = AdminWriteDbContextTestFactory.CreateWithDomainEventDispatcher(dispatcher);
        var dbContext = scope.DbContext;
        dispatcher.DbContext = dbContext;
        var tour = EntityBuilders.BuildTour(new TourOptions(Identifier: "andes-2026", Name: "Andes 2026"));
        dbContext.Tours.Add(tour);

        await dbContext.SaveEntities(CancellationToken.None);

        dispatcher.DispatchedEvents.ShouldHaveSingleItem().ShouldBeOfType<TourCreatedDomainEvent>();
        tour.GetDomainEvents().ShouldBeEmpty();
        var outboxMessage = dbContext.Set<IntegrationEventOutboxMessage>().ShouldHaveSingleItem();
        outboxMessage.EventType.ShouldBe(AdminTourCreatedIntegrationEvent.EventType);
        outboxMessage.EventVersion.ShouldBe(AdminTourCreatedIntegrationEvent.EventVersion);
        outboxMessage.EventId.ShouldNotBe(Guid.Empty);
        outboxMessage.OccurredAt.ShouldBe(new DateTimeOffset(2026, 6, 22, 12, 30, 0, TimeSpan.Zero));
        outboxMessage.PayloadJson.ShouldContain("andes-2026", StringComparison.Ordinal);
    }

    [Fact]
    public async Task SaveEntities_does_not_clear_domain_events_when_save_fails_after_dispatch()
    {
        var dispatcher = new CapturingDomainEventDispatcher();
        var failingInterceptor = new FailingSaveChangesInterceptor();
        await using var scope = AdminWriteDbContextTestFactory.CreateWithDomainEventDispatcher(dispatcher, failingInterceptor);
        var dbContext = scope.DbContext;
        var tour = EntityBuilders.BuildTour();
        dbContext.Tours.Add(tour);

        Func<Task> action = () => dbContext.SaveEntities(CancellationToken.None);

        await action.ShouldThrow<InvalidOperationException>();
        dispatcher.DispatchedEvents.ShouldHaveSingleItem().ShouldBeOfType<TourCreatedDomainEvent>();
        tour.GetDomainEvents().ShouldNotBeEmpty();
    }

    [Fact]
    public async Task SaveEntities_skips_dispatch_when_tracked_aggregates_have_no_domain_events()
    {
        var dispatcher = new CapturingDomainEventDispatcher();
        await using var scope = AdminWriteDbContextTestFactory.CreateWithDomainEventDispatcher(dispatcher);
        var dbContext = scope.DbContext;
        var tour = EntityBuilders.BuildTour();
        tour.ClearDomainEvents();
        dbContext.Tours.Add(tour);

        await dbContext.SaveEntities(CancellationToken.None);

        dispatcher.DispatchedEvents.ShouldBeEmpty();
        tour.GetDomainEvents().ShouldBeEmpty();
    }

}
