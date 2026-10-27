using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Logging;
using PandaBear.Testing.AspNetCore;
using ViajantesTurismo.Branding.AspNetCore;
using ViajantesTurismo.Branding.Infrastructure;
using ViajantesTurismo.Catalog.ApiService;

namespace ViajantesTurismo.Catalog.ApiServiceTests.Infrastructure;

/// <summary>Hosts the Branding routes in Catalog.ApiService with in-memory Branding settings.</summary>
internal static class BrandingApiTestHost
{
    private const string Audience = "branding-api";
    private const string AdministratorRole = "Admin";
    private const string UntrustedIssuer = "https://untrusted.test";
    private const string WrongAudience = "wrong-audience";

    public static WebApplicationFactory<CatalogApiHostEntryPoint> Create(string? environment = null) =>
        Create(environment, null, null, null, authenticateClient: true);

    public static WebApplicationFactory<CatalogApiHostEntryPoint> Create(TestBrandingSettingsStore store) =>
        Create(null, store, null, null, authenticateClient: true);

    public static WebApplicationFactory<CatalogApiHostEntryPoint> Create(
        TestBrandingSettingsStore store,
        RecordingOutputCacheStore outputCacheStore,
        CollectingLogger<BrandingAspNetCoreMarker>? logger = null) =>
        Create(null, store, outputCacheStore, logger, authenticateClient: true);

    public static WebApplicationFactory<CatalogApiHostEntryPoint> CreateAnonymous(TestBrandingSettingsStore store) =>
        Create(null, store, null, null, authenticateClient: false);

    public static WebApplicationFactory<CatalogApiHostEntryPoint> CreateAnonymous() =>
        Create(null, TestBrandingSettingsStore.Seeded(), null, null, authenticateClient: false);

    /// <summary>Gives an existing Catalog test host in-memory Branding settings.</summary>
    public static WebApplicationFactory<CatalogApiHostEntryPoint> WithBranding(
        WebApplicationFactory<CatalogApiHostEntryPoint> catalog,
        TestBrandingSettingsStore store,
        RecordingOutputCacheStore? outputCacheStore = null,
        CollectingLogger<BrandingAspNetCoreMarker>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(store);

        return catalog.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.Replace(ServiceDescriptor.Singleton<IBrandingSettingsStore>(store));
            services.Replace(ServiceDescriptor.Singleton<IBrandingSettingsRevisionStore>(store));
            if (outputCacheStore is not null)
            {
                services.Replace(ServiceDescriptor.Singleton<IOutputCacheStore>(_ => outputCacheStore));
            }

            if (logger is not null)
            {
                services.Replace(ServiceDescriptor.Singleton<ILogger<BrandingAspNetCoreMarker>>(_ => logger));
            }
        }));
    }

    public static void ConfigureAuthenticatedClient(HttpClient client, string role) =>
        ApiTestAuthentication.ConfigureAuthenticatedClient(client, Audience, role);

    public static void ConfigureClientWithUntrustedIssuer(HttpClient client) =>
        ApiTestAuthentication.ConfigureClient(client, Audience, UntrustedIssuer, AdministratorRole);

    public static void ConfigureClientWithWrongAudience(HttpClient client) =>
        ApiTestAuthentication.ConfigureAuthenticatedClient(client, WrongAudience, AdministratorRole);

    public static void VerifyMappedMutationDependencies(WebApplicationFactory<CatalogApiHostEntryPoint> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);

        using var scope = factory.Services.CreateScope();
        _ = scope.ServiceProvider.GetRequiredService<IBrandingSettingsStore>();
        _ = scope.ServiceProvider.GetRequiredService<IOutputCacheStore>();
    }

    private static WebApplicationFactory<CatalogApiHostEntryPoint> Create(
        string? environment,
        TestBrandingSettingsStore? store,
        RecordingOutputCacheStore? outputCacheStore,
        CollectingLogger<BrandingAspNetCoreMarker>? logger,
        bool authenticateClient) =>
        WithBranding(
            authenticateClient ? CatalogApiTestHost.CreateForBranding(environment) : CatalogApiTestHost.CreateAnonymous(),
            store ?? new TestBrandingSettingsStore(),
            outputCacheStore,
            logger);
}
