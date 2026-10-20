using Microsoft.Extensions.Configuration;
using SharedKernel.EventSourcing;
using SharedKernel.Idempotency;
using SharedKernel.Messaging;
using SharedKernel.Messaging.IntegrationEvents;
using ViajantesTurismo.Admin.Contracts.IntegrationEvents.Tours;
using ViajantesTurismo.Catalog.Application;
using ViajantesTurismo.Catalog.Application.Tours;

namespace ViajantesTurismo.Catalog.Testing.Infrastructure;

internal sealed class CatalogIntegrationEventScenario : IAsyncDisposable
{
    private readonly ServiceProvider provider;
    private readonly TestEventStore eventStore = new();
    private readonly TestCatalogTourReadModelStore readModelStore = new();

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

    public async Task Deliver(AdminTourCreatedIntegrationEvent integrationEvent, CancellationToken ct)
    {
        var serializer = provider.GetRequiredService<IIntegrationEventSerializer>();
        var envelope = new SharedKernel.Messaging.EventEnvelope(
            "cloudevents", "1.0", integrationEvent.EventId.ToString(), new Uri("urn:viajantes:admin"),
            AdminTourCreatedIntegrationEvent.EventType, AdminTourCreatedIntegrationEvent.EventVersion,
            integrationEvent.OccurredAt, null, "application/json", null, serializer.Serialize(integrationEvent),
            EventPayloadEncoding.Json, null);
        await provider.GetRequiredService<IEventEnvelopePublisher>().Publish(envelope, ct);
    }

    public async Task<IReadOnlyCollection<CatalogTourDraftReadModel>> ReadDrafts(CancellationToken ct)
    {
        var projection = new CatalogTourReadModelProjection(readModelStore);
        foreach (var envelope in await eventStore.LoadAfter(0, 100, ct))
        {
            await projection.Apply(envelope, ct);
        }

        return readModelStore.GetSnapshot();
    }

    public ValueTask DisposeAsync() => provider.DisposeAsync();
}
