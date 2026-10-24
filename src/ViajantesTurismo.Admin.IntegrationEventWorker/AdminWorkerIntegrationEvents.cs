using System.Text.Json.Serialization.Metadata;
using PandaBear.Messaging.IntegrationEvents;
using ViajantesTurismo.Branding.Contracts.IntegrationEvents;
using ViajantesTurismo.Branding.Contracts.IntegrationEvents.Branding;

namespace ViajantesTurismo.Admin.IntegrationEventWorker;

/// <summary>
/// Registers delivery of the events the Admin worker consumes. It lives in the worker assembly because the
/// integration-event generator must see a consume-only set of contracts here.
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
