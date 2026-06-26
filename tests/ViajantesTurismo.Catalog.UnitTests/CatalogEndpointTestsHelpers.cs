using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SharedKernel.Testing;
using ViajantesTurismo.Catalog.ApiService;
using ViajantesTurismo.Catalog.Application.Tours;

namespace ViajantesTurismo.Catalog.UnitTests;

internal static class CatalogEndpointTestsHelpers
{
    public static WebApplicationFactory<ICatalogApiAssemblyMarker> CreateFactory(ICatalogTourReadModelStore store)
    {
        return WebApplicationTestHost.Create<ICatalogApiAssemblyMarker>(
            configureTestServices: services =>
            {
                services.RemoveAll<ICatalogTourReadModelStore>();
                services.AddSingleton(store);
            });
    }

    public static CatalogTourDraftReadModel CreateTour(string identifier, string title)
    {
        return new CatalogTourDraftReadModel(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            identifier,
            title,
            1,
            DateTimeOffset.UtcNow);
    }
}
