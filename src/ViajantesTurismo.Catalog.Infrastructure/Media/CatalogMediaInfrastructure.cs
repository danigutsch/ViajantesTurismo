using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SharedKernel.MalwareScanning.ClamAv;
using SharedKernel.OpenApi;
using ViajantesTurismo.Catalog.Application.Media;

namespace ViajantesTurismo.Catalog.Infrastructure.Media;

/// <summary>Composes media storage, scanning, and persistence for the Catalog host and worker.</summary>
internal static class CatalogMediaInfrastructure
{
    /// <summary>Registers the configured media adapters and their shared dependencies.</summary>
    internal static TApplicationBuilder AddCatalogMediaInfrastructure<TApplicationBuilder>(this TApplicationBuilder builder)
        where TApplicationBuilder : IHostApplicationBuilder
    {
        builder.AddCatalogAiTextGeneration();

        if (builder.Configuration.GetSection(MediaObjectStorageConfiguration.S3SectionName).Exists())
        {
            builder.Services.AddMediaS3ObjectStorage(builder.Configuration, MediaObjectStorageConfiguration.S3SectionName);
        }
        else if (builder.Configuration.GetSection(MediaObjectStorageConfiguration.SectionName).Exists())
        {
            builder.Services.AddMediaS3ObjectStorage(builder.Configuration);
        }
        else if (builder.Environment.IsDevelopment() || OpenApiGenerationMode.IsEnabled(builder.Environment))
        {
            builder.Services.AddLocalMediaObjectStorage();
        }
        else
        {
            throw new InvalidOperationException(
                $"Catalog durable media object storage requires configuration section '{MediaObjectStorageConfiguration.SectionName}'.");
        }
        builder.Services.AddConfiguredClamAvMalwareScanner(builder.Configuration, builder.Environment);
        builder.Services.AddSingleton<IMediaUploadScanner, MalwareScannerMediaUploadScanner>();
        builder.Services.AddScoped<EfPublicMediaImageStore>();
        builder.Services.AddScoped<IPublicMediaImageStore>(services => services.GetRequiredService<EfPublicMediaImageStore>());
        builder.Services.AddScoped<IMediaImageUploadStore>(services => services.GetRequiredService<EfPublicMediaImageStore>());

        return builder;
    }
}
