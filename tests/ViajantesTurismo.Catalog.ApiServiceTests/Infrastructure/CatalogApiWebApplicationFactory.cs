using ViajantesTurismo.Catalog.ApiService;
using ViajantesTurismo.Catalog.Application.PublicContent;
using ViajantesTurismo.Catalog.Infrastructure;

namespace ViajantesTurismo.Catalog.ApiServiceTests.Infrastructure;

internal sealed class CatalogApiWebApplicationFactory(string? environment) : WebApplicationFactory<CatalogApiEntryPoint>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        if (environment is not null)
        {
            builder.UseEnvironment(environment);
        }

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IPublicContentStore>();
            services.AddSingleton<IPublicContentStore, InMemoryPublicContentStore>();
            services.Configure<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckServiceOptions>(options => options.Registrations.Clear());
        });
    }
}
