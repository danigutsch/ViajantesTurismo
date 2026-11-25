using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using SharedKernel.Messaging.IntegrationEvents;
using ViajantesTurismo.Catalog.Application.IntegrationEvents;
using ViajantesTurismo.Catalog.Application.Media;
using ViajantesTurismo.Catalog.Application.PublicContent;
using ViajantesTurismo.Catalog.Application.Tours;
using ViajantesTurismo.Catalog.Contracts.IntegrationEvents;
using ViajantesTurismo.Catalog.Contracts.IntegrationEvents.Tours;

namespace ViajantesTurismo.Catalog.Application;

/// <summary>
/// Provides extension methods for setting up Catalog application services.
/// </summary>
public static class ApplicationDependencyInjection
{
    /// <summary>
    /// Adds Catalog application services to the service collection.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The configured service collection.</returns>
    public static IServiceCollection AddCatalogApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddSingleton(TimeProvider.System);

        services.AddOptions<IntegrationEventOptions>()
            .BindConfiguration(IntegrationEventOptions.SectionName)
            .ValidateOnStart();
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<
                IValidateOptions<IntegrationEventOptions>,
                IntegrationEventOptionsValidator>());
        services.AddCatalogToursApplication();
        services.AddCatalogMediaApplication();
        services.AddIntegrationEventContract(
            CatalogTourRetirementAppliedIntegrationEvent.EventType,
            CatalogIntegrationEventJsonContext.Default.CatalogTourRetirementAppliedIntegrationEvent);
        services.AddGeneratedIntegrationEvents();
        services.TryAddScoped<PublicContentUpsertService>();

        return services;
    }
}
