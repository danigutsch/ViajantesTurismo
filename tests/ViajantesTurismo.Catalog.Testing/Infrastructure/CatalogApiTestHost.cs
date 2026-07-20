using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using SharedKernel.AI;
using SharedKernel.AspNetCore;
using SharedKernel.EventSourcing;
using SharedKernel.Messaging.IntegrationEvents;
using SharedKernel.MalwareScanning.ClamAv;
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
        TestPublicContentStore publicContentStore,
        TestEventStore eventStore)
    {
        return Create(null, tourStore, publicContentStore, null, null, null, eventStore: eventStore);
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

    public static WebApplicationFactory<CatalogApiHostEntryPoint> CreateProductionComposition()
    {
        return WebApplicationTestHost.Create<CatalogApiHostEntryPoint>(
            Environments.Development,
            services =>
            {
                services.Configure<HealthCheckServiceOptions>(options => options.Registrations.Clear());
                ApiTestAuthentication.ConfigureJwtBearer(services, Audience);
                services.RemoveAll<IHostedService>();
            },
            null,
            new Dictionary<string, string?>
            {
                [ApiAuthenticationDefaults.AuthorityConfigurationKey] = ApiTestAuthentication.Authority,
                [ApiAuthenticationDefaults.IssuerConfigurationKey] = ApiTestAuthentication.Authority,
                ["ConnectionStrings:catalog-database"] = "Host=localhost;Database=viajantes-catalog",
                [ClamAvMalwareScannerConfigurationKeys.DisabledConfigurationKey] = bool.TrueString
            });
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

    public static void VerifyMappedMutationDependencies(WebApplicationFactory<CatalogApiHostEntryPoint> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);

        using var scope = factory.Services.CreateScope();
        _ = scope.ServiceProvider.GetRequiredService<ICatalogTourReadModelStore>();
        _ = scope.ServiceProvider.GetRequiredService<IPublicMediaImageStore>();
        _ = scope.ServiceProvider.GetRequiredService<MediaImageUploadIntake>();
        _ = scope.ServiceProvider.GetRequiredService<MediaImageAccessibilityDraftService>();
        _ = scope.ServiceProvider.GetRequiredService<PublicContentUpsertService>();
        _ = scope.ServiceProvider.GetRequiredService<IOutputCacheStore>();
    }

    private static WebApplicationFactory<CatalogApiHostEntryPoint> Create(
        string? environment,
        TestCatalogTourReadModelStore? tourStore,
        TestPublicContentStore? publicContentStore,
        TestPublicMediaImageStore? mediaStore,
        TestMediaObjectStore? objectStore,
        IImageTextGenerator? imageTextGenerator,
        bool authenticateClient = true,
        TestEventStore? eventStore = null)
    {
        var configuredTourStore = tourStore ?? new TestCatalogTourReadModelStore();
        var configuredEventStore = eventStore ?? new TestEventStore();
        if (eventStore is null)
        {
            foreach (var tour in configuredTourStore.GetSnapshot())
            {
                configuredEventStore.SeedTour(tour);
            }
        }

        var hostEnvironment = environment ?? Environments.Development;
        var configuration = new Dictionary<string, string?>
        {
            [ApiAuthenticationDefaults.AuthorityConfigurationKey] = ApiTestAuthentication.Authority,
            [ApiAuthenticationDefaults.IssuerConfigurationKey] = ApiTestAuthentication.Authority
        };

        if (string.Equals(hostEnvironment, Environments.Development, StringComparison.OrdinalIgnoreCase)
            || string.Equals(hostEnvironment, "Test", StringComparison.OrdinalIgnoreCase)
            || string.Equals(hostEnvironment, "Testing", StringComparison.OrdinalIgnoreCase))
        {
            configuration[ClamAvMalwareScannerConfigurationKeys.DisabledConfigurationKey] = bool.TrueString;
        }
        else
        {
            configuration[ClamAvMalwareScannerConfigurationKeys.HostConfigurationKey] = "test-clamav";
        }

        return WebApplicationTestHost.Create<CatalogApiHostEntryPoint>(
            hostEnvironment,
            services =>
            {
                services.Replace(ServiceDescriptor.Singleton<IPublicContentStore>(publicContentStore ?? new TestPublicContentStore()));
                services.Replace(ServiceDescriptor.Singleton<ICatalogTourReadModelStore>(configuredTourStore));
                services.Replace(ServiceDescriptor.Singleton<IEventStore>(configuredEventStore));
                services.Replace(ServiceDescriptor.Singleton<ICatalogTourSlugLock>(new TestCatalogTourSlugLock()));
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
