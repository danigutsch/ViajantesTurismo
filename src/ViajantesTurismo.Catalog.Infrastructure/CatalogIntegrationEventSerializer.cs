using System.Text.Json;
using SharedKernel.Messaging.IntegrationEvents;
using ViajantesTurismo.Admin.Contracts.Tours;
using ViajantesTurismo.Catalog.Application.Media;

namespace ViajantesTurismo.Catalog.Infrastructure;

internal sealed class CatalogIntegrationEventSerializer : IIntegrationEventSerializer
{
    public string Serialize<TIntegrationEvent>(TIntegrationEvent integrationEvent)
        where TIntegrationEvent : IIntegrationEvent
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        return integrationEvent switch
        {
            MediaImageOriginalStoredIntegrationEvent originalStored => JsonSerializer.Serialize(
                originalStored,
                CatalogIntegrationEventJsonContext.Default.MediaImageOriginalStoredIntegrationEvent),
            AdminTourCreatedIntegrationEvent tourCreated => JsonSerializer.Serialize(
                tourCreated,
                CatalogIntegrationEventJsonContext.Default.AdminTourCreatedIntegrationEvent),
            _ => throw new NotSupportedException($"Integration event type '{integrationEvent.GetType().FullName}' is not configured for durable serialization."),
        };
    }
}
