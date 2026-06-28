using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using ViajantesTurismo.Catalog.Application.IntegrationEvents;

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

        services.AddOptions<IntegrationEventOptions>()
            .BindConfiguration(IntegrationEventOptions.SectionName)
            .ValidateOnStart();
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<
                IValidateOptions<IntegrationEventOptions>,
                IntegrationEventOptionsValidator>());

        return services;
    }
}
