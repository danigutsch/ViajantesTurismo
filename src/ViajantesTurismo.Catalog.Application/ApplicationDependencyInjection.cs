using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using SharedKernel.EventSourcing;
using SharedKernel.Idempotency;
using SharedKernel.IntegrationEvents;
using ViajantesTurismo.Admin.Contracts.Tours;
using ViajantesTurismo.Catalog.Application.IntegrationEvents;
using ViajantesTurismo.Catalog.Application.Media;
using ViajantesTurismo.Catalog.Application.Tours;

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
        services.TryAddScoped<IIntegrationEventHandler<AdminTourCreatedIntegrationEvent>>(sp =>
            new IdempotentIntegrationHandler<AdminTourCreatedIntegrationEvent>(
                new AdminTourCreatedIntegrationHandler(sp.GetRequiredService<IEventStore>()),
                sp.GetRequiredService<IIdempotencyStore>(),
                sp.GetRequiredService<IOptions<IntegrationEventOptions>>()));
        services.AddCatalogMediaApplication();

        return services;
    }
}
