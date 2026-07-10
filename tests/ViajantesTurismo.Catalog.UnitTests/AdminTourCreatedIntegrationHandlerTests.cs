using Microsoft.Extensions.Options;
using SharedKernel.EventSourcing;
using SharedKernel.Idempotency;
using SharedKernel.Testing.Assertions;
using ViajantesTurismo.Admin.Contracts.IntegrationEvents.Tours;
using ViajantesTurismo.Catalog.Application.IntegrationEvents;
using ViajantesTurismo.Catalog.Application.Tours;
using ViajantesTurismo.Catalog.Domain.Tours;

namespace ViajantesTurismo.Catalog.UnitTests;

public sealed class AdminTourCreatedIntegrationHandlerTests
{
    [Fact]
    public async Task Handle_creates_a_draft_catalog_tour_stream()
    {
        var idempotencyStore = new CapturingIdempotencyStore();
        var eventStore = new CapturingEventStore();
        var handler = new IdempotentIntegrationHandler<AdminTourCreatedIntegrationEvent>(
            new AdminTourCreatedIntegrationHandler(eventStore),
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
            new AdminTourCreatedIntegrationHandler(eventStore),
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
    public async Task Handle_ignores_duplicate_event_delivery()
    {
        var idempotencyStore = new CapturingIdempotencyStore();
        var eventStore = new CapturingEventStore();
        var handler = new IdempotentIntegrationHandler<AdminTourCreatedIntegrationEvent>(
            new AdminTourCreatedIntegrationHandler(eventStore),
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
            new AdminTourCreatedIntegrationHandler(new CapturingEventStore()),
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
}
