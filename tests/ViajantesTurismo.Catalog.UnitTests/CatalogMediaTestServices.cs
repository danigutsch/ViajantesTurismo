using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ViajantesTurismo.Catalog.Application.Media;

namespace ViajantesTurismo.Catalog.UnitTests;

internal static class CatalogMediaTestServices
{
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
