using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using ViajantesTurismo.Catalog.Application.Media;

namespace ViajantesTurismo.Catalog.Infrastructure;

/// <summary>
/// Provides extension methods for local media object storage.
/// </summary>
public static class LocalMediaObjectStorageDependencyInjection
{
    /// <summary>
    /// Adds local filesystem media object storage.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The configured service collection.</returns>
    public static IServiceCollection AddLocalMediaObjectStorage(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddOptions<LocalMediaObjectStorageOptions>()
            .BindConfiguration(LocalMediaObjectStorageOptions.SectionName)
            .ValidateOnStart();
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<
                IValidateOptions<LocalMediaObjectStorageOptions>,
                LocalMediaObjectStorageOptionsValidator>());
        services.TryAddScoped<IMediaObjectStore, LocalMediaObjectStore>();
        services.TryAddSingleton<IMediaUploadScanner, NoOpMediaUploadScanner>();

        return services;
    }
}
