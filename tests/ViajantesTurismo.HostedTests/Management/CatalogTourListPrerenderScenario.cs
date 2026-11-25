using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ViajantesTurismo.Catalog.Api;
using ViajantesTurismo.Catalog.Application.Tours;
using ViajantesTurismo.Catalog.Infrastructure;
using ViajantesTurismo.Catalog.Infrastructure.Tours;

namespace ViajantesTurismo.HostedTests.Management;

/// <summary>Observes Catalog page reads from a test host using the scenario database.</summary>
internal sealed class CatalogTourListPrerenderScenario
{
    private int _pageReads;

    /// <summary>Gets the number of resolved page readers.</summary>
    internal int PageReads => Volatile.Read(ref _pageReads);

    /// <summary>Uses the scenario database and counts each scoped page reader.</summary>
    internal WebApplicationFactory<CatalogApiHostEntryPoint> WithObservedPageReads(
        WebApplicationFactory<CatalogApiHostEntryPoint> catalog,
        string connectionString) =>
        catalog.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.Replace(ServiceDescriptor.Scoped(_ =>
            {
                var context = new CatalogDbContextDesignTimeFactory().CreateDbContext([]);
                context.Database.SetConnectionString(connectionString);
                return context;
            }));
            services.Replace(ServiceDescriptor.Scoped<ICatalogTourListReader>(provider =>
            {
                Interlocked.Increment(ref _pageReads);
                return new EfCatalogTourListReader(provider.GetRequiredService<CatalogDbContext>());
            }));
        }));
}
