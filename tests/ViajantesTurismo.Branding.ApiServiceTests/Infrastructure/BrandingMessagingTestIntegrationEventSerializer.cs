using SharedKernel.Messaging.IntegrationEvents;

namespace ViajantesTurismo.Branding.ApiServiceTests.Infrastructure;

internal sealed class BrandingMessagingTestIntegrationEventSerializer : IIntegrationEventSerializer
{
    public string Serialize<TIntegrationEvent>(TIntegrationEvent integrationEvent)
        where TIntegrationEvent : IIntegrationEvent
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        return "{}";
    }
}
