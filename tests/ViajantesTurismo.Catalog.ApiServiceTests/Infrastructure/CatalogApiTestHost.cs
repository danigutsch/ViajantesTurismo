using ViajantesTurismo.Catalog.ApiService;

namespace ViajantesTurismo.Catalog.ApiServiceTests.Infrastructure;

internal static class CatalogApiTestHost
{
    public static WebApplicationFactory<CatalogApiEntryPoint> Create(string? environment = null)
    {
        return new CatalogApiWebApplicationFactory(environment);
    }
}
