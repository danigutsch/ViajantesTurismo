using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SharedKernel.Testing.AspNetCore;
using ViajantesTurismo.Catalog.ApiService;
using ViajantesTurismo.Catalog.Application.Media;
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
                services.RemoveAll<IPublicMediaImageStore>();
                services.AddSingleton(store);
                services.AddSingleton<IPublicMediaImageStore, StubPublicMediaImageStore>();
                ApiTestAuthentication.ConfigureJwtBearer(services, "catalog-api");
            },
            configureClient: client => ApiTestAuthentication.ConfigureAuthenticatedClient(client, "catalog-api", "Admin"),
            configuration: new Dictionary<string, string?>
            {
                ["Authentication:Authority"] = ApiTestAuthentication.Authority,
                ["Authentication:Issuer"] = ApiTestAuthentication.Authority
            });
    }

    public static CatalogTourDraftReadModel CreateTour(string identifier, string title)
    {
        return new CatalogTourDraftReadModel(
            Guid.CreateVersion7(),
            Guid.CreateVersion7(),
            identifier,
            title,
            identifier,
            false,
            1,
            DateTimeOffset.UtcNow);
    }
}
