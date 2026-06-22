using SharedKernel.EventSourcing;
using SharedKernel.Idempotency;
using ViajantesTurismo.Admin.Contracts.Tours;
using ViajantesTurismo.Catalog.Application.IntegrationEvents;
using ViajantesTurismo.Catalog.Application.Tours;
using ViajantesTurismo.Catalog.Domain.Tours;

namespace ViajantesTurismo.Catalog.UnitTests;

public sealed class AdminTourCreatedIntegrationEventHandlerTests
{
    [Fact]
    public async Task Handle_creates_a_draft_catalog_tour_stream()
    {
        var idempotencyStore = new CapturingIdempotencyStore();
        var eventStore = new CapturingEventStore();
        var handler = new IdempotentIntegrationEventConsumer<AdminTourCreatedIntegrationEvent>(
            new AdminTourCreatedIntegrationEventConsumer(eventStore),
            idempotencyStore);
        var integrationEvent = new AdminTourCreatedIntegrationEvent(
            Guid.CreateVersion7(),
            DateTimeOffset.UtcNow,
            Guid.CreateVersion7(),
            "andes-2026",
            "Andes 2026");

        await handler.Handle(integrationEvent, CancellationToken.None);

        Assert.Equal(CatalogTourStreamIds.FromAdminTourId(integrationEvent.AdminTourId), eventStore.StreamId);
        Assert.Equal(ExpectedStreamRevision.NoStream, eventStore.ExpectedRevision);
        var draftCreated = Assert.Single(eventStore.Events);
        var typedEvent = Assert.IsType<CatalogTourDraftCreated>(draftCreated);
        Assert.Equal(integrationEvent.AdminTourId, typedEvent.AdminTourId);
        Assert.Equal(integrationEvent.Identifier, typedEvent.Identifier);
        Assert.Equal(integrationEvent.Name, typedEvent.Title);
        Assert.Equal(integrationEvent.EventId, typedEvent.SourceEventId);
        Assert.Equal(IdempotencyEntryState.Completed, idempotencyStore.CompletedState);
    }

    [Fact]
    public async Task Handle_ignores_duplicate_event_delivery()
    {
        var idempotencyStore = new CapturingIdempotencyStore(started: false);
        var eventStore = new CapturingEventStore();
        var handler = new IdempotentIntegrationEventConsumer<AdminTourCreatedIntegrationEvent>(
            new AdminTourCreatedIntegrationEventConsumer(eventStore),
            idempotencyStore);
        var integrationEvent = new AdminTourCreatedIntegrationEvent(
            Guid.CreateVersion7(),
            DateTimeOffset.UtcNow,
            Guid.CreateVersion7(),
            "andes-2026",
            "Andes 2026");

        await handler.Handle(integrationEvent, CancellationToken.None);

        Assert.Empty(eventStore.Events);
        Assert.Null(idempotencyStore.CompletedState);
    }
}
