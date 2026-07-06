using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using SharedKernel.Idempotency;
using SharedKernel.Messaging.IntegrationEvents;
using ViajantesTurismo.Catalog.Contracts;
using ViajantesTurismo.Catalog.Contracts.Media;
using ViajantesTurismo.Catalog.Application.IntegrationEvents;

namespace ViajantesTurismo.Catalog.Application.Media;

/// <summary>
/// Provides extension methods for Catalog media application services.
/// </summary>
public static class MediaDependencyInjection
{
    /// <summary>
    /// Adds Catalog media application services.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The configured service collection.</returns>
    public static IServiceCollection AddCatalogMediaApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddOptions<MediaUploadValidationOptions>()
            .BindConfiguration(MediaUploadValidationOptions.SectionName)
            .ValidateOnStart();
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<
                IValidateOptions<MediaUploadValidationOptions>,
                MediaUploadValidationOptionsValidator>());
        services.TryAddSingleton<IMediaUploadValidator>(sp => new MediaUploadValidator(sp.GetRequiredService<IOptions<MediaUploadValidationOptions>>().Value));
        services.TryAddScoped<MediaImageUploadIntake>();
        services.TryAddScoped<MediaObjectReconciliationService>();
        services.TryAddScoped<MediaImageOriginalStoredIntegrationHandler>();
        services.TryAddScoped<IIntegrationEventHandler<MediaImageOriginalStoredIntegrationEvent>>(sp =>
            new IdempotentIntegrationHandler<MediaImageOriginalStoredIntegrationEvent>(
                sp.GetRequiredService<MediaImageOriginalStoredIntegrationHandler>(),
                sp.GetRequiredService<IIdempotencyStore>(),
                sp.GetRequiredService<IOptions<IntegrationEventOptions>>()));
        services.AddIntegrationEventConsumer(
            MediaImageOriginalStoredIntegrationEvent.EventType,
            CatalogIntegrationEventJsonContext.Default.MediaImageOriginalStoredIntegrationEvent);

        return services;
    }
}
