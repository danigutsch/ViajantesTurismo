using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using ViajantesTurismo.Catalog.Application.Media;

namespace ViajantesTurismo.Catalog.UnitTests;

/// <summary>Owns the Catalog media validation services used by unit tests.</summary>
internal sealed class CatalogMediaTestServices : IDisposable
{
    private readonly ServiceProvider _provider;

    /// <summary>Creates the media services with optional validation settings.</summary>
    /// <param name="configureValidation">The validation settings to override for the test.</param>
    public CatalogMediaTestServices(Action<MediaUploadValidationOptions>? configureValidation = null)
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddCatalogMediaApplication();

        if (configureValidation is not null)
        {
            builder.Services.Configure(configureValidation);
        }

        _provider = builder.Services.BuildServiceProvider();
    }

    /// <summary>Gets the upload options after the application validates them.</summary>
    public MediaUploadValidationOptions ValidationOptions => _provider.GetRequiredService<IOptions<MediaUploadValidationOptions>>().Value;

    /// <summary>Gets the registered media upload validator.</summary>
    public IMediaUploadValidator Validator => _provider.GetRequiredService<IMediaUploadValidator>();

    /// <inheritdoc />
    public void Dispose() => _provider.Dispose();
}
