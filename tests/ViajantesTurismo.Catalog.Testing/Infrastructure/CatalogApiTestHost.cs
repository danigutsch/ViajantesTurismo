using Microsoft.Extensions.Diagnostics.HealthChecks;
using SharedKernel.AI;
using SharedKernel.AspNetCore;
using SharedKernel.Messaging.IntegrationEvents;
using SharedKernel.Testing.AspNetCore;
using ViajantesTurismo.Catalog.ApiService;
using ViajantesTurismo.Catalog.Application.Media;
using ViajantesTurismo.Catalog.Application.PublicContent;
using ViajantesTurismo.Catalog.Application.Tours;

namespace ViajantesTurismo.Catalog.Testing.Infrastructure;

internal static class CatalogApiTestHost
{
    private const string Audience = "catalog-api";
    private const string AdministratorRole = "Admin";
    private const string UntrustedIssuer = "https://untrusted.test";
    private const string WrongAudience = "wrong-audience";

    public static WebApplicationFactory<CatalogApiHostEntryPoint> Create(string? environment = null)
    {
        return Create(environment, null, null, null, null, null);
    }

    public static WebApplicationFactory<CatalogApiHostEntryPoint> Create(
        TestCatalogTourReadModelStore tourStore,
        TestPublicContentStore publicContentStore)
    {
        return Create(null, tourStore, publicContentStore, null, null, null);
    }

    public static WebApplicationFactory<CatalogApiHostEntryPoint> Create(
        TestCatalogTourReadModelStore tourStore,
        TestPublicContentStore publicContentStore,
        TestPublicMediaImageStore mediaStore)
    {
        return Create(null, tourStore, publicContentStore, mediaStore, null, null);
    }

    public static WebApplicationFactory<CatalogApiHostEntryPoint> Create(
        TestCatalogTourReadModelStore tourStore,
        TestPublicMediaImageStore mediaStore,
        TestMediaObjectStore objectStore)
    {
        return Create(null, tourStore, null, mediaStore, objectStore, null);
    }

    public static WebApplicationFactory<CatalogApiHostEntryPoint> Create(
        TestPublicMediaImageStore mediaStore,
        TestMediaObjectStore objectStore,
        IImageTextGenerator imageTextGenerator)
    {
        return Create(null, null, null, mediaStore, objectStore, imageTextGenerator);
    }

    public static WebApplicationFactory<CatalogApiHostEntryPoint> CreateAnonymous()
    {
        return Create(null, null, null, null, null, null, authenticateClient: false);
    }

    public static void ConfigureAuthenticatedClient(HttpClient client)
    {
        ApiTestAuthentication.ConfigureAuthenticatedClient(client, Audience, AdministratorRole);
    }

    public static void ConfigureAuthenticatedClient(HttpClient client, string role)
    {
        ApiTestAuthentication.ConfigureAuthenticatedClient(client, Audience, role);
    }

    public static void ConfigureClientWithUntrustedIssuer(HttpClient client)
    {
        ApiTestAuthentication.ConfigureClient(client, Audience, UntrustedIssuer, AdministratorRole);
    }

    public static void ConfigureClientWithWrongAudience(HttpClient client)
    {
        ApiTestAuthentication.ConfigureAuthenticatedClient(client, WrongAudience, AdministratorRole);
    }

    private static WebApplicationFactory<CatalogApiHostEntryPoint> Create(
        string? environment,
        TestCatalogTourReadModelStore? tourStore,
        TestPublicContentStore? publicContentStore,
        TestPublicMediaImageStore? mediaStore,
        TestMediaObjectStore? objectStore,
        IImageTextGenerator? imageTextGenerator,
        bool authenticateClient = true)
    {
        var configuration = new Dictionary<string, string?>
        {
            [ApiAuthenticationDefaults.AuthorityConfigurationKey] = ApiTestAuthentication.Authority,
            [ApiAuthenticationDefaults.IssuerConfigurationKey] = ApiTestAuthentication.Authority
        };

        return WebApplicationTestHost.Create<CatalogApiHostEntryPoint>(
            environment,
            services =>
            {
                services.Replace(ServiceDescriptor.Singleton<IPublicContentStore>(publicContentStore ?? new TestPublicContentStore()));
                services.Replace(ServiceDescriptor.Singleton<ICatalogTourReadModelStore>(tourStore ?? new TestCatalogTourReadModelStore()));
                services.Replace(ServiceDescriptor.Singleton<IPublicMediaImageStore>(mediaStore ?? new TestPublicMediaImageStore()));
                services.Replace(ServiceDescriptor.Singleton<IMediaObjectStore>(objectStore ?? new TestMediaObjectStore()));
                services.Replace(ServiceDescriptor.Singleton<IIntegrationEventOutbox, TestIntegrationEventOutbox>());
                if (imageTextGenerator is not null)
                {
                    services.Replace(ServiceDescriptor.Singleton(imageTextGenerator));
                }

                services.Configure<HealthCheckServiceOptions>(options => options.Registrations.Clear());
                ApiTestAuthentication.ConfigureJwtBearer(services, Audience);
            },
            authenticateClient
                ? ConfigureAuthenticatedClient
                : null,
            configuration);
    }
}
