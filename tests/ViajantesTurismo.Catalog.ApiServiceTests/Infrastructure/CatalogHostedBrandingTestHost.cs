using Microsoft.AspNetCore.TestHost;
using PandaBear.Branding;
using ViajantesTurismo.Branding.Infrastructure;
using ViajantesTurismo.Catalog.ApiService;

namespace ViajantesTurismo.Catalog.ApiServiceTests.Infrastructure;

internal static class CatalogHostedBrandingTestHost
{
    public static WebApplicationFactory<CatalogApiHostEntryPoint> Create(WebApplicationFactory<CatalogApiHostEntryPoint> baseFactory)
    {
        ArgumentNullException.ThrowIfNull(baseFactory);

        var store = new CatalogHostedBrandingStore();
        return baseFactory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.Replace(ServiceDescriptor.Singleton<IBrandingSettingsStore>(store));
            services.Replace(ServiceDescriptor.Singleton<IBrandingSettingsRevisionStore>(store));
        }));
    }
}
