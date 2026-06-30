using ViajantesTurismo.Catalog.ApiService;

namespace ViajantesTurismo.Catalog.ApiServiceTests.Infrastructure;

internal static class CatalogApiTestHost
{
    public static WebApplicationFactory<CatalogApiEntryPoint> Create(string? environment = null)
    {
        return new CatalogApiWebApplicationFactory(environment);
    }

    public static WebApplicationFactory<CatalogApiEntryPoint> Create(
        TestCatalogTourReadModelStore tourStore,
        TestPublicContentStore publicContentStore)
    {
        return new CatalogApiWebApplicationFactory(null, tourStore, publicContentStore);
    }

    public static WebApplicationFactory<CatalogApiEntryPoint> Create(
        TestCatalogTourReadModelStore tourStore,
        TestPublicContentStore publicContentStore,
        TestPublicMediaImageStore mediaStore)
    {
        return new CatalogApiWebApplicationFactory(null, tourStore, publicContentStore, mediaStore);
    }
}
