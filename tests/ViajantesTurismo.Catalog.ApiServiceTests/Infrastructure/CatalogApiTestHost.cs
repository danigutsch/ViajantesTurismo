using ViajantesTurismo.Catalog.ApiService;

namespace ViajantesTurismo.Catalog.ApiServiceTests.Infrastructure;

internal static class CatalogApiTestHost
{
    public static WebApplicationFactory<CatalogApiEntryPoint> Create(string? environment = null)
    {
        var factory = new WebApplicationFactory<CatalogApiEntryPoint>();
        return environment is null
            ? factory
            : factory.WithWebHostBuilder(builder => builder.UseEnvironment(environment));
    }
}
