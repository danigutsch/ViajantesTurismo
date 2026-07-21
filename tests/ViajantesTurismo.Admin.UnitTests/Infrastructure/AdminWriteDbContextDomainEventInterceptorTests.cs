using Microsoft.EntityFrameworkCore;
using System.Reflection;
using SharedKernel.EntityFrameworkCore;
using SharedKernel.Messaging.IntegrationEvents.CloudEvents;
using SharedKernel.Messaging.IntegrationEvents.EntityFrameworkCore;
using ViajantesTurismo.Admin.Contracts.IntegrationEvents.Tours;
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
        outboxMessage.EnvelopeSpec.ShouldBe(CloudEventConstants.Spec);
        outboxMessage.EnvelopeSpecVersion.ShouldBe(CloudEventConstants.SpecVersion);
        outboxMessage.EventType.ShouldBe(AdminTourCreatedIntegrationEvent.EventType);
        outboxMessage.EventVersion.ShouldBe(AdminTourCreatedIntegrationEvent.EventVersion);
        outboxMessage.EventId.ShouldNotBeEmpty();
        outboxMessage.Time.ShouldBe(new DateTimeOffset(2026, 6, 22, 12, 30, 0, TimeSpan.Zero));
        outboxMessage.Payload.ShouldContain("andes-2026", StringComparison.Ordinal);
    }

    [Fact]
    public async Task SaveChanges_rejects_synchronous_domain_event_dispatch()
    {
        // Arrange
        var dispatcher = new OutboxEnqueuingDomainEventDispatcher(new FakeTimeProvider(
            new DateTimeOffset(2026, 6, 22, 12, 30, 0, TimeSpan.Zero)));
        await using var scope = AdminWriteDbContextTestFactory.CreateWithDomainEventDispatcher(dispatcher);
        var dbContext = scope.DbContext;
        dispatcher.DbContext = dbContext;
        var tour = EntityBuilders.BuildTour(new TourOptions(Identifier: "andes-sync-2026", Name: "Andes Sync 2026"));
        dbContext.Tours.Add(tour);
        var saveChanges = typeof(DbContext).GetMethod(nameof(DbContext.SaveChanges), [typeof(bool)]).ShouldNotBeNull();
        Action action = () => _ = saveChanges.Invoke(dbContext, [true]);

        // Act
        var invocationException = action.ShouldThrow<TargetInvocationException>();

        // Assert
        var exception = invocationException.InnerException.ShouldBeOfType<InvalidOperationException>();
        exception.Message.ShouldBe("Synchronous SaveChanges is not supported; use SaveChangesAsync.");
        dispatcher.DispatchedEvents.ShouldBeEmpty();
        tour.GetDomainEvents().ShouldNotBeEmpty();
        dbContext.Set<IntegrationEventOutboxMessage>().ShouldBeEmpty();
    }

    [Fact]
    public async Task SaveEntities_dispatches_generated_integration_events_with_scope_validation_enabled()
    {
        await using var scope = AdminWriteDbContextTestFactory.CreateWithGeneratedIntegrationEventDispatcher();
        var dbContext = scope.DbContext;
        var tour = EntityBuilders.BuildTour(new TourOptions(Identifier: "andes-generated-2026", Name: "Andes Generated 2026"));
        dbContext.Tours.Add(tour);

        await dbContext.SaveEntities(CancellationToken.None);

        tour.GetDomainEvents().ShouldBeEmpty();
        var outboxMessage = dbContext.Set<IntegrationEventOutboxMessage>().ShouldHaveSingleItem();
        outboxMessage.EventType.ShouldBe(AdminTourCreatedIntegrationEvent.EventType);
        outboxMessage.Payload.ShouldContain("andes-generated-2026", StringComparison.Ordinal);
    }

    [Fact]
    public async Task SaveEntities_uses_one_dispatch_scope_for_all_events_and_restores_the_ambient_context()
    {
        // Arrange
        var probe = new DomainEventDispatchLifecycleProbe();
        await using var scope = AdminWriteDbContextTestFactory.CreateWithGeneratedIntegrationEventDispatcher(probe);
        var dbContext = scope.DbContext;
        var firstTour = EntityBuilders.BuildTour(new TourOptions(Identifier: "andes-scope-1-2026", Name: "Andes Scope 1"));
        var secondTour = EntityBuilders.BuildTour(new TourOptions(Identifier: "andes-scope-2-2026", Name: "Andes Scope 2"));
        dbContext.Tours.AddRange(firstTour, secondTour);
        await using var previousContext = new DbContext(
            new DbContextOptionsBuilder().UseInMemoryDatabase(Guid.NewGuid().ToString("N")).Options);
        DbContext? restoredContext;

        // Act
        using (CurrentSaveChangesDbContext.Enter(previousContext))
        {
            await dbContext.SaveEntities(CancellationToken.None);
            restoredContext = CurrentSaveChangesDbContext.Current;
        }

        // Assert
        probe.CreatedHandlers.ShouldHaveSingleItem();
        probe.HandledEvents.ShouldHaveCount(2);
        probe.HandledEvents[0].Handler.ShouldBeSameAs(probe.HandledEvents[1].Handler);
        probe.HandledEvents.ShouldAllSatisfy(observation => observation.CurrentContext.ShouldBeSameAs(dbContext));
        var disposed = probe.DisposedHandlers.ShouldHaveSingleItem();
        disposed.Handler.ShouldBeSameAs(probe.CreatedHandlers[0]);
        disposed.CurrentContext.ShouldBeSameAs(dbContext);
        restoredContext.ShouldBeSameAs(previousContext);
        CurrentSaveChangesDbContext.Current.ShouldBeNull();
        firstTour.GetDomainEvents().ShouldBeEmpty();
        secondTour.GetDomainEvents().ShouldBeEmpty();
        dbContext.Set<IntegrationEventOutboxMessage>().ShouldHaveCount(2);
    }

    [Fact]
    public async Task SaveEntities_disposes_the_dispatch_scope_when_dispatch_fails()
    {
        // Arrange
        var failure = new InvalidOperationException("Simulated dispatch failure.");
        var probe = new DomainEventDispatchLifecycleProbe { DispatchFailure = failure };
        await using var scope = AdminWriteDbContextTestFactory.CreateWithGeneratedIntegrationEventDispatcher(probe);
        var dbContext = scope.DbContext;
        var tour = EntityBuilders.BuildTour();
        dbContext.Tours.Add(tour);
        Func<Task> action = () => dbContext.SaveEntities(CancellationToken.None);

        // Act
        var exception = await action.ShouldThrow<InvalidOperationException>();

        // Assert
        exception.ShouldBeSameAs(failure);
        probe.CreatedHandlers.ShouldHaveSingleItem();
        probe.HandledEvents.ShouldHaveSingleItem();
        var disposed = probe.DisposedHandlers.ShouldHaveSingleItem();
        disposed.CurrentContext.ShouldBeSameAs(dbContext);
        CurrentSaveChangesDbContext.Current.ShouldBeNull();
        tour.GetDomainEvents().ShouldNotBeEmpty();
    }

    [Fact]
    public async Task SaveEntities_disposes_the_dispatch_scope_when_dispatch_is_cancelled()
    {
        // Arrange
        using var cancellation = new CancellationTokenSource();
        var probe = new DomainEventDispatchLifecycleProbe { CancellationSource = cancellation };
        await using var scope = AdminWriteDbContextTestFactory.CreateWithGeneratedIntegrationEventDispatcher(probe);
        var dbContext = scope.DbContext;
        var tour = EntityBuilders.BuildTour();
        dbContext.Tours.Add(tour);
        Func<Task> action = () => dbContext.SaveEntities(cancellation.Token);

        // Act
        _ = await action.ShouldThrowAssignableTo<OperationCanceledException>();

        // Assert
        probe.CreatedHandlers.ShouldHaveSingleItem();
        probe.HandledEvents.ShouldHaveSingleItem();
        var disposed = probe.DisposedHandlers.ShouldHaveSingleItem();
        disposed.CurrentContext.ShouldBeSameAs(dbContext);
        CurrentSaveChangesDbContext.Current.ShouldBeNull();
        tour.GetDomainEvents().ShouldNotBeEmpty();
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
    public async Task SaveEntities_retry_after_save_failure_does_not_dispatch_domain_events_twice()
    {
        var dispatcher = new OutboxEnqueuingDomainEventDispatcher(new FakeTimeProvider(
            new DateTimeOffset(2026, 6, 22, 12, 30, 0, TimeSpan.Zero)));
        var failingInterceptor = new FailingSaveChangesInterceptor();
        await using var scope = AdminWriteDbContextTestFactory.CreateWithDomainEventDispatcher(dispatcher, failingInterceptor);
        var dbContext = scope.DbContext;
        dispatcher.DbContext = dbContext;
        var tour = EntityBuilders.BuildTour(new TourOptions(Identifier: "andes-retry-2026", Name: "Andes Retry 2026"));
        dbContext.Tours.Add(tour);

        Func<Task> firstSave = () => dbContext.SaveEntities(CancellationToken.None);

        await firstSave.ShouldThrow<InvalidOperationException>();
        await dbContext.SaveEntities(CancellationToken.None);

        dispatcher.DispatchedEvents.ShouldHaveSingleItem().ShouldBeOfType<TourCreatedDomainEvent>();
        tour.GetDomainEvents().ShouldBeEmpty();
        var outboxMessage = dbContext.Set<IntegrationEventOutboxMessage>().ShouldHaveSingleItem();
        outboxMessage.EventType.ShouldBe(AdminTourCreatedIntegrationEvent.EventType);
        outboxMessage.Payload.ShouldContain("andes-retry-2026", StringComparison.Ordinal);
    }

    [Fact]
    public async Task SaveEntities_redispatches_after_a_failed_context_pool_lease()
    {
        // Arrange
        var dispatcher = new CapturingDomainEventDispatcher();
        var failingInterceptor = new FailingSaveChangesInterceptor();
        await using var scope = AdminWriteDbContextTestFactory.CreateWithDomainEventDispatcher(dispatcher, failingInterceptor);
        var firstContext = scope.DbContext;
        var tour = EntityBuilders.BuildTour(new TourOptions(Identifier: "andes-pooled-retry-2026", Name: "Andes Pooled Retry 2026"));
        firstContext.Tours.Add(tour);
        var domainEvent = tour.GetDomainEvents().ShouldHaveSingleItem();
        Func<Task> firstSave = () => firstContext.SaveEntities(CancellationToken.None);
        await firstSave.ShouldThrow<InvalidOperationException>();

        // Act
        var secondContext = await scope.LeaseNextDbContext();
        secondContext.Tours.Add(tour);
        await secondContext.SaveEntities(CancellationToken.None);

        // Assert
        secondContext.ShouldBeSameAs(firstContext);
        dispatcher.DispatchedEvents.ShouldHaveCount(2);
        dispatcher.DispatchedEvents[0].ShouldBeSameAs(domainEvent);
        dispatcher.DispatchedEvents[1].ShouldBeSameAs(domainEvent);
        tour.GetDomainEvents().ShouldBeEmpty();
    }

    [Fact]
    public async Task SaveEntities_skips_dispatch_when_tracked_aggregates_have_no_domain_events()
    {
        var probe = new DomainEventDispatchLifecycleProbe();
        await using var scope = AdminWriteDbContextTestFactory.CreateWithGeneratedIntegrationEventDispatcher(probe);
        var dbContext = scope.DbContext;
        var tour = EntityBuilders.BuildTour();
        tour.ClearDomainEvents();
        dbContext.Tours.Add(tour);

        await dbContext.SaveEntities(CancellationToken.None);

        probe.CreatedHandlers.ShouldBeEmpty();
        probe.HandledEvents.ShouldBeEmpty();
        probe.DisposedHandlers.ShouldBeEmpty();
        tour.GetDomainEvents().ShouldBeEmpty();
        dbContext.Set<IntegrationEventOutboxMessage>().ShouldBeEmpty();
    }

}
