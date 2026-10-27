using PandaBear.ObjectStorage;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using PandaBear.AI;
using PandaBear.AspNetCore;
using PandaBear.EventSourcing;
using PandaBear.Messaging.IntegrationEvents;
using PandaBear.MalwareScanning.ClamAv;
using PandaBear.Testing.AspNetCore;
using ViajantesTurismo.Branding.AspNetCore;
using ViajantesTurismo.Catalog.ApiService;
using ViajantesTurismo.Catalog.Application.Media;
using ViajantesTurismo.Catalog.Application.PublicContent;
using ViajantesTurismo.Catalog.Application.Tours;

namespace ViajantesTurismo.Catalog.Testing.Infrastructure;

/// <summary>Builds in-memory Catalog API test hosts with fake stores and test JWT authentication.</summary>
internal static class CatalogApiTestHost
{
    private const string Audience = "catalog-api";
    private const string BrandingAudience = "branding-api";
    private const string AdministratorRole = "Admin";
    private const string UntrustedIssuer = "https://untrusted.test";
    private const string WrongAudience = "wrong-audience";
    private const string MediaObjectStorageSectionName = "Catalog:MediaObjectStorage:SeaweedFs";

    /// <summary>Creates an authenticated host for the given environment (Development by default) with empty fake stores.</summary>
    public static WebApplicationFactory<CatalogApiHostEntryPoint> Create(string? environment = null)
    {
        return Create(environment, null, null, null, null, null);
    }

    /// <summary>Creates an authenticated host backed by the given tour and public content stores.</summary>
    public static WebApplicationFactory<CatalogApiHostEntryPoint> Create(
        TestCatalogTourReadModelStore tourStore,
        TestPublicContentStore publicContentStore)
    {
        return Create(null, tourStore, publicContentStore, null, null, null);
    }

    /// <summary>Creates an authenticated host backed by the given tour, public content and media image stores.</summary>
    public static WebApplicationFactory<CatalogApiHostEntryPoint> Create(
        TestCatalogTourReadModelStore tourStore,
        TestPublicContentStore publicContentStore,
        TestPublicMediaImageStore mediaStore)
    {
        return Create(null, tourStore, publicContentStore, mediaStore, null, null);
    }

    /// <summary>Creates an authenticated host backed by the given tour and public content stores and event store.</summary>
    public static WebApplicationFactory<CatalogApiHostEntryPoint> Create(
        TestCatalogTourReadModelStore tourStore,
        TestPublicContentStore publicContentStore,
        TestEventStore eventStore)
    {
        return Create(null, tourStore, publicContentStore, null, null, null, eventStore: eventStore);
    }

    /// <summary>Creates an authenticated host backed by the given tour and media image stores and media object store.</summary>
    public static WebApplicationFactory<CatalogApiHostEntryPoint> Create(
        TestCatalogTourReadModelStore tourStore,
        TestPublicMediaImageStore mediaStore,
        TestMediaObjectStore objectStore)
    {
        return Create(null, tourStore, null, mediaStore, objectStore, null);
    }

    /// <summary>Creates an authenticated host backed by the given media stores and a replacement image text generator.</summary>
    public static WebApplicationFactory<CatalogApiHostEntryPoint> Create(
        TestPublicMediaImageStore mediaStore,
        TestMediaObjectStore objectStore,
        IImageTextGenerator imageTextGenerator)
    {
        return Create(null, null, null, mediaStore, objectStore, imageTextGenerator);
    }

    /// <summary>Creates an authenticated host backed by the given tour and media image stores and event store.</summary>
    public static WebApplicationFactory<CatalogApiHostEntryPoint> Create(
        TestCatalogTourReadModelStore tourStore,
        TestPublicMediaImageStore mediaStore,
        TestEventStore eventStore)
    {
        return Create(null, tourStore, null, mediaStore, null, null, eventStore: eventStore);
    }

    /// <summary>Creates a host whose clients carry no authentication token.</summary>
    public static WebApplicationFactory<CatalogApiHostEntryPoint> CreateAnonymous()
    {
        return Create(null, null, null, null, null, null, authenticateClient: false);
    }

    /// <summary>Creates a host whose clients carry an Administrator token for the co-hosted Branding routes.</summary>
    public static WebApplicationFactory<CatalogApiHostEntryPoint> CreateForBranding(string? environment = null)
    {
        return Create(environment, null, null, null, null, null, authenticateClient: false, configureClient: client => ConfigureBrandingClient(client, AdministratorRole));
    }

    /// <summary>Creates a Development host that keeps the production service composition, with only health checks, hosted services and authentication replaced.</summary>
    public static WebApplicationFactory<CatalogApiHostEntryPoint> CreateProductionComposition()
    {
        return WebApplicationTestHost.Create<CatalogApiHostEntryPoint>(
            Environments.Development,
            services =>
            {
                services.Configure<HealthCheckServiceOptions>(options => options.Registrations.Clear());
                ApiTestAuthentication.ConfigureJwtBearer(services, Audience);
                // The co-hosted Branding scheme keeps its own audience after the test signing key is applied.
                services.PostConfigure<JwtBearerOptions>(
                    BrandingModule.BearerScheme,
                    options => options.TokenValidationParameters.ValidAudience = BrandingAudience);
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

    /// <summary>Gives the client an Administrator token for the Catalog audience.</summary>
    public static void ConfigureAuthenticatedClient(HttpClient client)
    {
        ApiTestAuthentication.ConfigureAuthenticatedClient(client, Audience, AdministratorRole);
    }

    /// <summary>Gives the client a token for the Catalog audience with the given role.</summary>
    public static void ConfigureAuthenticatedClient(HttpClient client, string role)
    {
        ApiTestAuthentication.ConfigureAuthenticatedClient(client, Audience, role);
    }

    /// <summary>Gives the client a token for the Branding audience with the given role.</summary>
    public static void ConfigureBrandingClient(HttpClient client, string role)
    {
        ApiTestAuthentication.ConfigureAuthenticatedClient(client, BrandingAudience, role);
    }

    /// <summary>Gives the client an Administrator token from an issuer the host does not trust.</summary>
    public static void ConfigureClientWithUntrustedIssuer(HttpClient client)
    {
        ApiTestAuthentication.ConfigureClient(client, Audience, UntrustedIssuer, AdministratorRole);
    }

    /// <summary>Gives the client an Administrator token for an audience the host does not accept.</summary>
    public static void ConfigureClientWithWrongAudience(HttpClient client)
    {
        ApiTestAuthentication.ConfigureAuthenticatedClient(client, WrongAudience, AdministratorRole);
    }

    /// <summary>Resolves the services required by the mapped mutation endpoints, failing if any is not registered.</summary>
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
        TestEventStore? eventStore = null,
        Action<HttpClient>? configureClient = null)
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

        if (!string.Equals(hostEnvironment, Environments.Development, StringComparison.OrdinalIgnoreCase))
        {
            configuration[$"{MediaObjectStorageSectionName}:Endpoint"] = "https://seaweedfs.example";
            configuration[$"{MediaObjectStorageSectionName}:Bucket"] = "media";
            configuration[$"{MediaObjectStorageSectionName}:AccessKey"] = "access";
            configuration[$"{MediaObjectStorageSectionName}:SecretKey"] = "secret";
        }

        return WebApplicationTestHost.Create<CatalogApiHostEntryPoint>(
            hostEnvironment,
            services =>
            {
                services.Replace(ServiceDescriptor.Singleton<IPublicContentStore>(publicContentStore ?? new TestPublicContentStore()));
                services.Replace(ServiceDescriptor.Singleton<ICatalogTourReadModelStore>(configuredTourStore));
                services.Replace(ServiceDescriptor.Singleton<IEventStore>(configuredEventStore));
                services.Replace(ServiceDescriptor.Singleton<ICatalogTourSlugLock>(new TestCatalogTourSlugLock()));
                services.Replace(ServiceDescriptor.Singleton<ICatalogTourMutationLock>(new TestCatalogTourMutationLock()));
                services.Replace(ServiceDescriptor.Singleton<IPublicMediaImageStore>(mediaStore ?? new TestPublicMediaImageStore()));
                services.Replace(ServiceDescriptor.Singleton<IObjectStore>(objectStore ?? new TestMediaObjectStore()));
                services.Replace(ServiceDescriptor.Singleton<IIntegrationEventOutbox, TestIntegrationEventOutbox>());
                if (imageTextGenerator is not null)
                {
                    services.Replace(ServiceDescriptor.Singleton(imageTextGenerator));
                }

                services.Configure<HealthCheckServiceOptions>(options => options.Registrations.Clear());
                ApiTestAuthentication.ConfigureJwtBearer(services, Audience);
                // The co-hosted Branding scheme keeps its own audience after the test signing key is applied.
                services.PostConfigure<JwtBearerOptions>(
                    BrandingModule.BearerScheme,
                    options => options.TokenValidationParameters.ValidAudience = BrandingAudience);
            },
            configureClient ?? (authenticateClient
                ? ConfigureAuthenticatedClient
                : null),
            configuration);
    }
}
