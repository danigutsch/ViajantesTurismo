using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ViajantesTurismo.Catalog.Api;
using ViajantesTurismo.Catalog.Application.Media;
using ViajantesTurismo.Catalog.Application.Tours;
using ViajantesTurismo.Catalog.Infrastructure;
using ViajantesTurismo.Catalog.Infrastructure.Media;
using ViajantesTurismo.Catalog.Infrastructure.Tours;

namespace ViajantesTurismo.HostedTests.Catalog.Infrastructure.Tours;

/// <summary>Connects the public Catalog API path to an isolated PostgreSQL database.</summary>
internal static class CatalogPublicTourBrowsingTestHost
{
    /// <summary>Uses the production EF tour-page reader for the configured test host.</summary>
    public static WebApplicationFactory<CatalogApiHostEntryPoint> Connect(
        WebApplicationFactory<CatalogApiHostEntryPoint> host, string connectionString) =>
        host.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.Replace(ServiceDescriptor.Scoped(_ =>
            {
                var context = new CatalogDbContextDesignTimeFactory().CreateDbContext([]);
                context.Database.SetConnectionString(connectionString);
                return context;
            }));
            services.Replace(ServiceDescriptor.Scoped<ICatalogTourListReader, EfCatalogTourListReader>());
            services.Replace(ServiceDescriptor.Scoped<ICatalogTourReadModelStore, EfCatalogTourReadModelStore>());
            services.Replace(ServiceDescriptor.Scoped<IPublicMediaImageStore, EfPublicMediaImageStore>());
        }));
}
