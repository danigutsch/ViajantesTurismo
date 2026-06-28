using Microsoft.Extensions.Diagnostics.HealthChecks;
using ViajantesTurismo.Catalog.ApiService;
using ViajantesTurismo.Catalog.Application.PublicContent;
using ViajantesTurismo.Catalog.Application.Tours;

namespace ViajantesTurismo.Catalog.ApiServiceTests.Infrastructure;

internal sealed class CatalogApiWebApplicationFactory(
    string? environment,
    TestCatalogTourReadModelStore? tourStore = null,
    TestPublicContentStore? publicContentStore = null) : WebApplicationFactory<CatalogApiEntryPoint>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        if (environment is not null)
        {
            builder.UseEnvironment(environment);
        }

        builder.ConfigureServices(services =>
        {
            services.Replace(ServiceDescriptor.Singleton<IPublicContentStore>(publicContentStore ?? new TestPublicContentStore()));
            services.Replace(ServiceDescriptor.Singleton<ICatalogTourReadModelStore>(tourStore ?? new TestCatalogTourReadModelStore()));
            services.Configure<HealthCheckServiceOptions>(options => options.Registrations.Clear());
        });
    }
}
