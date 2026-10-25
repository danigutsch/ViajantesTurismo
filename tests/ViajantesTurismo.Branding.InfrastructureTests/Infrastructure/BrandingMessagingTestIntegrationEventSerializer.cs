using PandaBear.Messaging.IntegrationEvents;

namespace ViajantesTurismo.Branding.InfrastructureTests.Infrastructure;

internal sealed class BrandingMessagingTestIntegrationEventSerializer : IIntegrationEventSerializer
{
    public string Serialize<TIntegrationEvent>(TIntegrationEvent integrationEvent)
        where TIntegrationEvent : IIntegrationEvent
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        return "{}";
    }
}
