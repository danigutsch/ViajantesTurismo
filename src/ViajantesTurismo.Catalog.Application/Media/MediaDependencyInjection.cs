using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

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

        return services;
    }
}
