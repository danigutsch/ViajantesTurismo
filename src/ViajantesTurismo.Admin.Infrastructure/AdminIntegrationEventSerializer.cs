using System.Text.Json;
using SharedKernel.Messaging.IntegrationEvents;
using ViajantesTurismo.Admin.Contracts.Tours;

namespace ViajantesTurismo.Admin.Infrastructure;

internal sealed class AdminIntegrationEventSerializer : IIntegrationEventSerializer
{
    public string Serialize<TIntegrationEvent>(TIntegrationEvent integrationEvent)
        where TIntegrationEvent : IIntegrationEvent
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        return integrationEvent switch
        {
            AdminTourCreatedIntegrationEvent tourCreated => JsonSerializer.Serialize(
                tourCreated,
                AdminIntegrationEventJsonContext.Default.AdminTourCreatedIntegrationEvent),
            _ => throw new NotSupportedException($"Integration event type '{integrationEvent.GetType().FullName}' is not configured for durable serialization."),
        };
    }
}
