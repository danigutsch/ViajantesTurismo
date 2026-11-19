using System.Text.Json.Serialization.Metadata;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PandaBear.Messaging;
using PandaBear.Messaging.IntegrationEvents;
using ViajantesTurismo.Branding.Contracts.IntegrationEvents;
using ViajantesTurismo.Branding.Contracts.IntegrationEvents.Branding;
using ViajantesTurismo.Catalog.Contracts.IntegrationEvents;
using ViajantesTurismo.Catalog.Contracts.IntegrationEvents.Tours;

namespace ViajantesTurismo.Admin.IntegrationEventWorker;

/// <summary>
/// Registers delivery of the events the Admin worker consumes. The handler lives in this assembly because the
/// integration-event generator requires each consumed contract's handler beside its registration (SKMSG001).
/// </summary>
internal static class AdminWorkerIntegrationEvents
{
    /// <summary>Service key for the generated publisher behind the Branding payload guard.</summary>
    internal const string GeneratedPublisherKey = "admin-branding-generated-publisher";

    /// <summary>Adds the generated envelope publisher for Branding revisions and Catalog retirement completions.</summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddAdminWorkerIntegrationEventDelivery(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        if (services.Any(static descriptor =>
            descriptor.ServiceType == typeof(IEventEnvelopePublisher) && Equals(descriptor.ServiceKey, GeneratedPublisherKey)))
        {
            return services;
        }

        JsonTypeInfo<BrandingSettingsChangedIntegrationEvent> brandingSettingsChangedJsonTypeInfo =
            BrandingIntegrationEventJsonContext.Default.BrandingSettingsChangedIntegrationEvent;
        services.AddScoped<IIntegrationEventHandler<BrandingSettingsChangedIntegrationEvent>, BrandingSettingsChangedIntegrationHandler>();
        services.AddIntegrationEventConsumer(
            BrandingSettingsChangedIntegrationEvent.EventType,
            brandingSettingsChangedJsonTypeInfo);
        JsonTypeInfo<CatalogTourRetirementAppliedIntegrationEvent> tourRetirementAppliedJsonTypeInfo =
            CatalogIntegrationEventJsonContext.Default.CatalogTourRetirementAppliedIntegrationEvent;
        services.AddScoped<IIntegrationEventHandler<CatalogTourRetirementAppliedIntegrationEvent>, CatalogTourRetirementAppliedIntegrationHandler>();
        services.AddIntegrationEventConsumer(
            CatalogTourRetirementAppliedIntegrationEvent.EventType,
            tourRetirementAppliedJsonTypeInfo);
        services.AddGeneratedIntegrationEvents();
        var publisher = services.Single(static descriptor =>
            descriptor.ServiceType == typeof(IEventEnvelopePublisher) && !descriptor.IsKeyedService);
        services.Add(ServiceDescriptor.DescribeKeyed(
            typeof(IEventEnvelopePublisher),
            GeneratedPublisherKey,
            publisher.ImplementationType ?? throw new InvalidOperationException("The Admin event dispatcher must be registered as an implementation type."),
            publisher.Lifetime));
        services.Replace(ServiceDescriptor.Scoped<IEventEnvelopePublisher, BrandingEventPayloadGuard>());

        return services;
    }
}
