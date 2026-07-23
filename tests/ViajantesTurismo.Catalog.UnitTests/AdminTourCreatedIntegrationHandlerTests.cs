using Microsoft.Extensions.Options;
using SharedKernel.EventSourcing;
using SharedKernel.Idempotency;
using SharedKernel.Testing;
using ViajantesTurismo.Admin.Contracts.IntegrationEvents.Tours;
using ViajantesTurismo.Catalog.Application.IntegrationEvents;
using ViajantesTurismo.Catalog.Application.Tours;
using ViajantesTurismo.Catalog.Domain.Tours;
using ViajantesTurismo.Catalog.Testing.Infrastructure;

namespace ViajantesTurismo.Catalog.UnitTests;

public sealed class AdminTourCreatedIntegrationHandlerTests
{
    [Fact]
    public async Task Handle_creates_a_draft_catalog_tour_stream()
    {
        var idempotencyStore = new CapturingIdempotencyStore();
        var eventStore = new CapturingEventStore();
        var handler = new IdempotentIntegrationHandler<AdminTourCreatedIntegrationEvent>(
            new AdminTourCreatedIntegrationHandler(eventStore, new TestCatalogTourSlugLock()),
            idempotencyStore,
            Options.Create(new IntegrationEventOptions()));
        var integrationEvent = new AdminTourCreatedIntegrationEvent(
            Guid.CreateVersion7(),
            DateTimeOffset.UtcNow,
            Guid.CreateVersion7(),
            "andes-2026",
            "Andes 2026");

        await handler.Handle(integrationEvent, CancellationToken.None);

        eventStore.StreamId.ShouldBe(CatalogTourStreamIds.FromAdminTourId(integrationEvent.AdminTourId));
        eventStore.ExpectedRevision.ShouldBe(ExpectedStreamRevision.NoStream);
        var draftCreated = eventStore.Events.ShouldHaveSingleItem();
        var typedEvent = draftCreated.ShouldBeOfType<CatalogTourDraftCreated>();
        typedEvent.AdminTourId.ShouldBe(integrationEvent.AdminTourId);
        typedEvent.Identifier.ShouldBe(integrationEvent.Identifier);
        typedEvent.Title.ShouldBe(integrationEvent.Name);
        typedEvent.SourceEventId.ShouldBe(integrationEvent.EventId);
        idempotencyStore.CompletedState.ShouldBe(IdempotencyEntryState.Completed);
    }

    [Fact]
    public async Task Handle_sanitizes_catalog_tour_identifier_and_title()
    {
        var idempotencyStore = new CapturingIdempotencyStore();
        var eventStore = new CapturingEventStore();
        var handler = new IdempotentIntegrationHandler<AdminTourCreatedIntegrationEvent>(
            new AdminTourCreatedIntegrationHandler(eventStore, new TestCatalogTourSlugLock()),
            idempotencyStore,
            Options.Create(new IntegrationEventOptions()));
        var integrationEvent = new AdminTourCreatedIntegrationEvent(
            Guid.CreateVersion7(),
            DateTimeOffset.UtcNow,
            Guid.CreateVersion7(),
            "  andes\u0000   2026  ",
            "  Andes\u0001   2026  ");

        await handler.Handle(integrationEvent, CancellationToken.None);

        var draftCreated = eventStore.Events.ShouldHaveSingleItem();
        var typedEvent = draftCreated.ShouldBeOfType<CatalogTourDraftCreated>();
        typedEvent.Identifier.ShouldBe("andes 2026");
        typedEvent.Title.ShouldBe("Andes 2026");
    }

    [Fact]
    public async Task Handle_uses_an_id_fallback_when_the_normalized_initial_slug_is_already_owned()
    {
        // Arrange
        var eventStore = new CapturingEventStore();
        var ownerId = Guid.CreateVersion7();
        eventStore.AddReplayEvent(new EventEnvelope(
            CatalogTourStreamIds.FromAdminTourId(Guid.CreateVersion7()),
            1,
            StreamRevision.From(1),
            Guid.CreateVersion7(),
            typeof(CatalogTourDraftCreated).FullName ?? nameof(CatalogTourDraftCreated),
            new CatalogTourDraftCreated(
                ownerId,
                Guid.CreateVersion7(),
                "TOUR-1",
                "Owner Tour",
                Guid.CreateVersion7(),
                "tour-1"),
            DateTimeOffset.UtcNow));
        var slugLock = new TestCatalogTourSlugLock();
        var handler = new AdminTourCreatedIntegrationHandler(eventStore, slugLock);
        var integrationEvent = new AdminTourCreatedIntegrationEvent(
            Guid.CreateVersion7(),
            DateTimeOffset.UtcNow,
            Guid.CreateVersion7(),
            "TOUR_1",
            "Second Tour");

        // Act
        await handler.Handle(integrationEvent, TestContext.Current.CancellationToken);

        // Assert
        var draftCreated = eventStore.Events.ShouldHaveSingleItem().ShouldBeOfType<CatalogTourDraftCreated>();
        var createdTour = CatalogTour.Rehydrate([draftCreated]);
        createdTour.Slug.ShouldBe($"tour-{draftCreated.CatalogTourId:N}");
        slugLock.AcquiredSlugs.ShouldMatchCollection(
            preferred => preferred.ShouldBe("tour-1"),
            fallback => fallback.ShouldBe(createdTour.Slug));
    }

    [Fact]
    public async Task Handle_ignores_duplicate_event_delivery()
    {
        var idempotencyStore = new CapturingIdempotencyStore();
        var eventStore = new CapturingEventStore();
        var handler = new IdempotentIntegrationHandler<AdminTourCreatedIntegrationEvent>(
            new AdminTourCreatedIntegrationHandler(eventStore, new TestCatalogTourSlugLock()),
            idempotencyStore,
            Options.Create(new IntegrationEventOptions()));
        var integrationEvent = new AdminTourCreatedIntegrationEvent(
            Guid.CreateVersion7(),
            DateTimeOffset.UtcNow,
            Guid.CreateVersion7(),
            "andes-2026",
            "Andes 2026");

        await handler.Handle(integrationEvent, CancellationToken.None);
        await handler.Handle(integrationEvent, CancellationToken.None);

        var draftCreated = eventStore.Events.ShouldHaveSingleItem();
        var typedEvent = draftCreated.ShouldBeOfType<CatalogTourDraftCreated>();
        typedEvent.SourceEventId.ShouldBe(integrationEvent.EventId);
        idempotencyStore.CompletedState.ShouldBe(IdempotencyEntryState.Completed);
    }

    [Fact]
    public async Task Handle_uses_configured_idempotency_lock_duration()
    {
        // Arrange
        var idempotencyStore = new CapturingIdempotencyStore();
        var configuredDuration = TimeSpan.FromMinutes(2);
        var handler = new IdempotentIntegrationHandler<AdminTourCreatedIntegrationEvent>(
            new AdminTourCreatedIntegrationHandler(new CapturingEventStore(), new TestCatalogTourSlugLock()),
            idempotencyStore,
            Options.Create(new IntegrationEventOptions { IdempotencyLockDuration = configuredDuration }));
        var integrationEvent = new AdminTourCreatedIntegrationEvent(
            Guid.CreateVersion7(),
            DateTimeOffset.UtcNow,
            Guid.CreateVersion7(),
            "andes-2026",
            "Andes 2026");

        // Act
        await handler.Handle(integrationEvent, CancellationToken.None);

        // Assert
        idempotencyStore.CapturedLockDuration.ShouldBe(configuredDuration);
    }

    [Fact]
    [Trait(SharedKernelTestTraitNames.CapabilityName, TestTraits.IntegrationEventTransportCapability)]
    public async Task Handle_surfaces_an_existing_started_entry_for_transport_retry()
    {
        // Arrange
        var idempotencyStore = new CapturingIdempotencyStore(
            started: false,
            existingState: IdempotencyEntryState.Started);
        var eventStore = new CapturingEventStore();
        var handler = new IdempotentIntegrationHandler<AdminTourCreatedIntegrationEvent>(
            new AdminTourCreatedIntegrationHandler(eventStore, new TestCatalogTourSlugLock()),
            idempotencyStore,
            Options.Create(new IntegrationEventOptions()));
        var integrationEvent = new AdminTourCreatedIntegrationEvent(
            Guid.CreateVersion7(),
            DateTimeOffset.UtcNow,
            Guid.CreateVersion7(),
            "andes-2026",
            "Andes 2026");

        // Act
        Func<Task> handle = () => handler.Handle(integrationEvent, CancellationToken.None).AsTask();

        // Assert
        var exception = await handle.ShouldThrow<InvalidOperationException>();
        exception.Message.ShouldContain("already being processed", StringComparison.Ordinal);
        eventStore.Events.ShouldBeEmpty();
        idempotencyStore.CompletedState.ShouldBeNull();
    }

    [Fact]
    [Trait(SharedKernelTestTraitNames.CapabilityName, TestTraits.IntegrationEventTransportCapability)]
    public async Task Handle_retries_a_failed_event_after_the_lease_is_reacquired()
    {
        // Arrange
        var idempotencyStore = new CapturingIdempotencyStore();
        var eventStore = new CapturingEventStore(appendFailures: 1);
        var handler = new IdempotentIntegrationHandler<AdminTourCreatedIntegrationEvent>(
            new AdminTourCreatedIntegrationHandler(eventStore, new TestCatalogTourSlugLock()),
            idempotencyStore,
            Options.Create(new IntegrationEventOptions()));
        var integrationEvent = new AdminTourCreatedIntegrationEvent(
            Guid.CreateVersion7(),
            DateTimeOffset.UtcNow,
            Guid.CreateVersion7(),
            "andes-2026",
            "Andes 2026");

        // Act
        Func<Task> firstAttempt = () => handler.Handle(integrationEvent, CancellationToken.None).AsTask();
        _ = await firstAttempt.ShouldThrow<InvalidOperationException>();
        idempotencyStore.SimulateExpiredLease();
        await handler.Handle(integrationEvent, CancellationToken.None);

        // Assert
        eventStore.AppendAttempts.ShouldBe(2);
        eventStore.Events.ShouldHaveSingleItem();
        idempotencyStore.CompletedState.ShouldBe(IdempotencyEntryState.Completed);
    }

    [Fact]
    [Trait(SharedKernelTestTraitNames.CapabilityName, TestTraits.IntegrationEventTransportCapability)]
    public async Task Handle_completes_retry_when_append_succeeded_before_idempotency_completion_failed()
    {
        // Arrange
        var idempotencyStore = new CapturingIdempotencyStore(completeFailures: 1);
        var eventStore = new CapturingEventStore();
        var handler = new IdempotentIntegrationHandler<AdminTourCreatedIntegrationEvent>(
            new AdminTourCreatedIntegrationHandler(eventStore, new TestCatalogTourSlugLock()),
            idempotencyStore,
            Options.Create(new IntegrationEventOptions()));
        var integrationEvent = new AdminTourCreatedIntegrationEvent(
            Guid.CreateVersion7(),
            DateTimeOffset.UtcNow,
            Guid.CreateVersion7(),
            "andes-2026",
            "Andes 2026");

        // Act
        Func<Task> firstAttempt = () => handler.Handle(integrationEvent, CancellationToken.None).AsTask();
        _ = await firstAttempt.ShouldThrow<InvalidOperationException>();
        idempotencyStore.SimulateExpiredLease();
        await handler.Handle(integrationEvent, CancellationToken.None);

        // Assert
        eventStore.AppendAttempts.ShouldBe(2);
        var draftCreated = eventStore.Events.ShouldHaveSingleItem().ShouldBeOfType<CatalogTourDraftCreated>();
        draftCreated.SourceEventId.ShouldBe(integrationEvent.EventId);
        idempotencyStore.CompletedState.ShouldBe(IdempotencyEntryState.Completed);
    }

    [Fact]
    [Trait(SharedKernelTestTraitNames.CapabilityName, TestTraits.IntegrationEventTransportCapability)]
    public async Task Handle_preserves_conflict_when_existing_initial_event_has_a_different_source_event_id()
    {
        // Arrange
        var eventStore = new CapturingEventStore();
        var integrationEvent = new AdminTourCreatedIntegrationEvent(
            Guid.CreateVersion7(),
            DateTimeOffset.UtcNow,
            Guid.CreateVersion7(),
            "andes-2026",
            "Andes 2026");
        var streamId = CatalogTourStreamIds.FromAdminTourId(integrationEvent.AdminTourId);
        eventStore.AddReplayEvent(new EventEnvelope(
            streamId,
            1,
            StreamRevision.From(1),
            Guid.CreateVersion7(),
            typeof(CatalogTourDraftCreated).FullName ?? nameof(CatalogTourDraftCreated),
            new CatalogTourDraftCreated(
                Guid.CreateVersion7(),
                integrationEvent.AdminTourId,
                integrationEvent.Identifier,
                integrationEvent.Name,
                Guid.CreateVersion7(),
                integrationEvent.Identifier),
            DateTimeOffset.UtcNow));
        var handler = new AdminTourCreatedIntegrationHandler(eventStore, new TestCatalogTourSlugLock());

        // Act
        Func<Task> handle = () => handler.Handle(integrationEvent, CancellationToken.None).AsTask();
        var exception = await handle.ShouldThrow<ExpectedStreamRevisionConflictException>();

        // Assert
        exception.StreamId.ShouldBe(streamId);
        exception.ExpectedRevision.ShouldBe(ExpectedStreamRevision.NoStream);
        eventStore.Events.ShouldBeEmpty();
    }
}
