using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ViajantesTurismo.Catalog.Application.Media;

namespace ViajantesTurismo.Catalog.UnitTests;

/// <summary>Builds Catalog media application service providers for tests.</summary>
internal static class CatalogMediaTestServices
{
    /// <summary>Creates a provider with media application services and optional validation option overrides.</summary>
    public static ServiceProvider CreateProvider(Action<MediaUploadValidationOptions>? configureValidation = null)
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddCatalogMediaApplication();

        if (configureValidation is not null)
        {
            builder.Services.Configure(configureValidation);
        }

        return builder.Services.BuildServiceProvider();
    }
}
