using Microsoft.Extensions.Diagnostics.HealthChecks;
using SharedKernel.Testing.AspNetCore;
using ViajantesTurismo.Catalog.ApiService;
using ViajantesTurismo.Catalog.Application.Media;
using ViajantesTurismo.Catalog.Application.PublicContent;
using ViajantesTurismo.Catalog.Application.PublicTheme;
using ViajantesTurismo.Catalog.Application.Tours;

namespace ViajantesTurismo.Catalog.Testing.Infrastructure;

internal static class CatalogApiTestHost
{
    public static WebApplicationFactory<CatalogApiHostEntryPoint> Create(string? environment = null)
    {
        return Create(environment, null, null, null, null);
    }

    public static WebApplicationFactory<CatalogApiHostEntryPoint> Create(
        TestCatalogTourReadModelStore tourStore,
        TestPublicContentStore publicContentStore)
    {
        return Create(null, tourStore, publicContentStore, null, null);
    }

    public static WebApplicationFactory<CatalogApiHostEntryPoint> Create(
        TestCatalogTourReadModelStore tourStore,
        TestPublicContentStore publicContentStore,
        TestPublicMediaImageStore mediaStore)
    {
        return Create(null, tourStore, publicContentStore, mediaStore, null);
    }

    public static WebApplicationFactory<CatalogApiHostEntryPoint> Create(TestPublicThemeSettingsStore publicThemeStore)
    {
        return Create(null, null, null, null, publicThemeStore);
    }

    private static WebApplicationFactory<CatalogApiHostEntryPoint> Create(
        string? environment,
        TestCatalogTourReadModelStore? tourStore,
        TestPublicContentStore? publicContentStore,
        TestPublicMediaImageStore? mediaStore,
        TestPublicThemeSettingsStore? publicThemeStore)
    {
        return WebApplicationTestHost.Create<CatalogApiHostEntryPoint>(
            environment,
            services =>
            {
                services.Replace(ServiceDescriptor.Singleton<IPublicContentStore>(publicContentStore ?? new TestPublicContentStore()));
                services.Replace(ServiceDescriptor.Singleton<IPublicThemeSettingsStore>(publicThemeStore ?? new TestPublicThemeSettingsStore()));
                services.Replace(ServiceDescriptor.Singleton<ICatalogTourReadModelStore>(tourStore ?? new TestCatalogTourReadModelStore()));
                services.Replace(ServiceDescriptor.Singleton<IPublicMediaImageStore>(mediaStore ?? new TestPublicMediaImageStore()));
                services.Replace(ServiceDescriptor.Singleton<IMediaObjectStore>(new TestMediaObjectStore()));
                services.Configure<HealthCheckServiceOptions>(options => options.Registrations.Clear());
            });
    }
}
