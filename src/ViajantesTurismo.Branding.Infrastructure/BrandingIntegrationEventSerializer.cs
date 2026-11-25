using System.Text.Json;
using SharedKernel.Messaging.IntegrationEvents;
using ViajantesTurismo.Branding.Contracts.IntegrationEvents;
using ViajantesTurismo.Branding.Contracts.IntegrationEvents.Branding;

namespace ViajantesTurismo.Branding.Infrastructure;

/// <summary>Serializes Branding integration events with the source-generated JSON context.</summary>
internal sealed class BrandingIntegrationEventSerializer : IIntegrationEventSerializer
{
    /// <inheritdoc />
    public string Serialize<TIntegrationEvent>(TIntegrationEvent integrationEvent)
        where TIntegrationEvent : IIntegrationEvent
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        return integrationEvent switch
        {
            BrandingSettingsChangedIntegrationEvent brandingSettingsChanged => SerializeBranding(brandingSettingsChanged),
            _ => throw new NotSupportedException(
                $"Integration event type '{integrationEvent.GetType().FullName}' is not registered for Branding serialization."),
        };
    }

    private static string SerializeBranding(BrandingSettingsChangedIntegrationEvent integrationEvent)
    {
        if (integrationEvent.LogoContent is { Length: > BrandingEventLimits.MaxLogoBytes })
        {
            throw new InvalidOperationException("The Branding event logo exceeds the configured byte limit.");
        }

        var payload = JsonSerializer.Serialize(
            integrationEvent, BrandingIntegrationEventJsonContext.Default.BrandingSettingsChangedIntegrationEvent);
        BrandingEventLimits.EnsurePayloadFits(payload);
        return payload;
    }
}
