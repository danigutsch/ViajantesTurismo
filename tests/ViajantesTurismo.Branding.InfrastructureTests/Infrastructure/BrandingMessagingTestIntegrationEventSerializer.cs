using PandaBear.Messaging.IntegrationEvents;

namespace ViajantesTurismo.Branding.InfrastructureTests.Infrastructure;

/// <summary>Serializer stub that writes every integration event as an empty JSON object.</summary>
internal sealed class BrandingMessagingTestIntegrationEventSerializer : IIntegrationEventSerializer
{
    /// <summary>Returns <c>{}</c> for any non-null event.</summary>
    public string Serialize<TIntegrationEvent>(TIntegrationEvent integrationEvent)
        where TIntegrationEvent : IIntegrationEvent
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        return "{}";
    }
}
