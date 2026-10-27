using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using PandaBear.AspNetCore;
using PandaBear.MalwareScanning.ClamAv;
using PandaBear.Testing.AspNetCore;
using ViajantesTurismo.Catalog.ApiService;
using ViajantesTurismo.Catalog.Application.Media;
using ViajantesTurismo.Catalog.Application.Tours;

namespace ViajantesTurismo.Catalog.UnitTests;

/// <summary>Helpers for Catalog endpoint tests.</summary>
internal static class CatalogEndpointTestsHelpers
{
    /// <summary>Creates a test-server factory using the given read-model store, a stub media image store, JWT auth, and malware scanning disabled.</summary>
    public static WebApplicationFactory<ICatalogApiAssemblyMarker> CreateFactory(ICatalogTourReadModelStore store)
    {
        return WebApplicationTestHost.Create<ICatalogApiAssemblyMarker>(
            environment: Environments.Development,
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
                [ApiAuthenticationDefaults.AuthorityConfigurationKey] = ApiTestAuthentication.Authority,
                [ApiAuthenticationDefaults.IssuerConfigurationKey] = ApiTestAuthentication.Authority,
                [ClamAvMalwareScannerConfigurationKeys.DisabledConfigurationKey] = bool.TrueString
            });
    }

    /// <summary>Creates an unpublished draft tour read model with the identifier used for the slug.</summary>
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
            TimeProvider.System.GetUtcNow());
    }
}
