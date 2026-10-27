using Microsoft.Extensions.Configuration;
using PandaBear.EventSourcing;
using PandaBear.Idempotency;
using PandaBear.Messaging;
using PandaBear.Messaging.IntegrationEvents;
using ViajantesTurismo.Admin.Contracts.IntegrationEvents.Tours;
using ViajantesTurismo.Catalog.Application;
using ViajantesTurismo.Catalog.Application.Tours;

namespace ViajantesTurismo.Catalog.Testing.Infrastructure;

/// <summary>Wires the Catalog application services over in-memory stores to deliver Admin tour integration events and read the resulting drafts.</summary>
internal sealed class CatalogIntegrationEventScenario : IAsyncDisposable
{
    private readonly ServiceProvider provider;
    private readonly TestEventStore eventStore = new();
    private readonly TestCatalogTourReadModelStore readModelStore = new();

    /// <summary>Builds the service provider with in-memory event store, idempotency store and slug lock.</summary>
    public CatalogIntegrationEventScenario()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddLogging();
        services.AddSingleton<IEventStore>(eventStore);
        services.AddSingleton<IIdempotencyStore>(new CapturingIdempotencyStore());
        services.AddSingleton<ICatalogTourSlugLock>(new TestCatalogTourSlugLock());
        services.AddCatalogApplication();
        provider = services.BuildServiceProvider();
    }

    /// <summary>Serializes the event into a CloudEvents envelope and publishes it through the application publisher.</summary>
    public async Task Deliver(AdminTourCreatedIntegrationEvent integrationEvent, CancellationToken ct)
    {
        var serializer = provider.GetRequiredService<IIntegrationEventSerializer>();
        var envelope = new PandaBear.Messaging.EventEnvelope(
            "cloudevents", "1.0", integrationEvent.EventId.ToString(), new Uri("urn:viajantes:admin"),
            AdminTourCreatedIntegrationEvent.EventType, AdminTourCreatedIntegrationEvent.EventVersion,
            integrationEvent.OccurredAt, null, "application/json", null, serializer.Serialize(integrationEvent),
            EventPayloadEncoding.Json, null);
        await provider.GetRequiredService<IEventEnvelopePublisher>().Publish(envelope, ct);
    }

    /// <summary>Projects all stored events (up to 100) into the read model and returns its snapshot.</summary>
    public async Task<IReadOnlyCollection<CatalogTourDraftReadModel>> ReadDrafts(CancellationToken ct)
    {
        var projection = new CatalogTourReadModelProjection(readModelStore);
        foreach (var envelope in await eventStore.LoadAfter(0, 100, ct))
        {
            await projection.Apply(envelope, ct);
        }

        return readModelStore.GetSnapshot();
    }

    /// <summary>Disposes the underlying service provider.</summary>
    public ValueTask DisposeAsync() => provider.DisposeAsync();
}
