using System.Text.Json.Serialization.Metadata;
using PandaBear.Messaging.IntegrationEvents;
using ViajantesTurismo.Branding.Contracts.IntegrationEvents;
using ViajantesTurismo.Branding.Contracts.IntegrationEvents.Branding;

namespace ViajantesTurismo.Admin.IntegrationEventWorker;

/// <summary>
/// Registers delivery of the events the Admin worker consumes. The handler lives in this assembly because the
/// integration-event generator requires each consumed contract's handler beside its registration (SKMSG001).
/// </summary>
internal static class AdminWorkerIntegrationEvents
{
    /// <summary>Adds the generated envelope publisher for Branding settings revisions.</summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddAdminWorkerIntegrationEventDelivery(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        JsonTypeInfo<BrandingSettingsChangedIntegrationEvent> brandingSettingsChangedJsonTypeInfo =
            BrandingIntegrationEventJsonContext.Default.BrandingSettingsChangedIntegrationEvent;
        services.AddScoped<IIntegrationEventHandler<BrandingSettingsChangedIntegrationEvent>, BrandingSettingsChangedIntegrationHandler>();
        services.AddIntegrationEventConsumer(
            BrandingSettingsChangedIntegrationEvent.EventType,
            brandingSettingsChangedJsonTypeInfo);
        services.AddGeneratedIntegrationEvents();

        return services;
    }
}
