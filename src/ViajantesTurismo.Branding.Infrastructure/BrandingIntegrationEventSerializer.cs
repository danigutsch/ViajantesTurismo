using System.Text.Json;
using SharedKernel.Messaging.IntegrationEvents;
using ViajantesTurismo.Branding.Contracts.IntegrationEvents;
using ViajantesTurismo.Branding.Contracts.IntegrationEvents.Branding;

namespace ViajantesTurismo.Branding.Infrastructure;

internal sealed class BrandingIntegrationEventSerializer : IIntegrationEventSerializer
{
    public string Serialize<TIntegrationEvent>(TIntegrationEvent integrationEvent)
        where TIntegrationEvent : IIntegrationEvent
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        return integrationEvent switch
        {
            BrandingSettingsChangedIntegrationEvent brandingSettingsChanged => JsonSerializer.Serialize(
                brandingSettingsChanged,
                BrandingIntegrationEventJsonContext.Default.BrandingSettingsChangedIntegrationEvent),
            _ => throw new NotSupportedException(
                $"Integration event type '{integrationEvent.GetType().FullName}' is not registered for Branding serialization."),
        };
    }
}
