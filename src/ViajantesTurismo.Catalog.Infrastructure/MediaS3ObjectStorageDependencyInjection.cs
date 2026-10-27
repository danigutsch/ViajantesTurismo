using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PandaBear.ObjectStorage.S3;
using ViajantesTurismo.Catalog.Domain;

namespace ViajantesTurismo.Catalog.Infrastructure;

/// <summary>Registers S3-backed media object storage.</summary>
internal static class MediaS3ObjectStorageDependencyInjection
{
    /// <summary>Adds the S3 object store bound to the given configuration section, limited to the catalog media object key length.</summary>
    public static IServiceCollection AddMediaS3ObjectStorage(this IServiceCollection services, IConfiguration configuration, string sectionName = MediaObjectStorageConfiguration.SectionName)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        return services.AddS3ObjectStore(options =>
        {
            configuration.GetSection(sectionName).Bind(options);
            options.MaxObjectKeyLength = CatalogDomainLimits.MaxMediaObjectKeyLength;
        });
    }
}
