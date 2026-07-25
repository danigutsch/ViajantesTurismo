using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SharedKernel.AspNetCore;
using SharedKernel.Testing.AspNetCore;
using ViajantesTurismo.Branding.ApiService;

namespace ViajantesTurismo.Branding.ApiServiceTests.Infrastructure;

internal static class BrandingApiTestHost
{
    private const string Audience = "branding-api";
    private const string AdministratorRole = "Admin";
    private const string UntrustedIssuer = "https://untrusted.test";
    private const string WrongAudience = "wrong-audience";

    public static WebApplicationFactory<BrandingApiHostEntryPoint> Create(string? environment = null)
    {
        return Create(environment, null, null, null);
    }

    public static WebApplicationFactory<BrandingApiHostEntryPoint> Create(TestBrandingSettingsStore store)
    {
        return Create(null, store, null, null);
    }

    public static WebApplicationFactory<BrandingApiHostEntryPoint> Create(
        TestBrandingSettingsStore store,
        RecordingOutputCacheStore outputCacheStore,
        CollectingLogger<BrandingApiHostEntryPoint>? logger = null)
    {
        return Create(null, store, outputCacheStore, logger);
    }

    public static WebApplicationFactory<BrandingApiHostEntryPoint> CreateProductionComposition()
    {
        return WebApplicationTestHost.Create<BrandingApiHostEntryPoint>(
            Environments.Development,
            services =>
            {
                services.Configure<HealthCheckServiceOptions>(options => options.Registrations.Clear());
                ApiTestAuthentication.ConfigureJwtBearer(services, Audience);
            },
            null,
            new Dictionary<string, string?>
            {
                [ApiAuthenticationDefaults.AuthorityConfigurationKey] = ApiTestAuthentication.Authority,
                [ApiAuthenticationDefaults.IssuerConfigurationKey] = ApiTestAuthentication.Authority,
                ["ConnectionStrings:catalog-database"] = "Host=localhost;Database=viajantes-branding"
            });
    }

    private static WebApplicationFactory<BrandingApiHostEntryPoint> Create(
        string? environment,
        TestBrandingSettingsStore? store,
        RecordingOutputCacheStore? outputCacheStore,
        CollectingLogger<BrandingApiHostEntryPoint>? logger,
        bool authenticateClient = true)
    {
        return WebApplicationTestHost.Create<BrandingApiHostEntryPoint>(
            environment,
            services =>
            {
                services.Replace(ServiceDescriptor.Singleton<IBrandingSettingsStore>(store ?? new TestBrandingSettingsStore()));
                if (outputCacheStore is not null)
                {
                    services.Replace(ServiceDescriptor.Singleton<IOutputCacheStore>(_ => outputCacheStore));
                }

                if (logger is not null)
                {
                    services.Replace(ServiceDescriptor.Singleton<ILogger<BrandingApiHostEntryPoint>>(_ => logger));
                }

                services.Configure<HealthCheckServiceOptions>(options => options.Registrations.Clear());
                ApiTestAuthentication.ConfigureJwtBearer(services, Audience);
            },
            authenticateClient
                ? client => ApiTestAuthentication.ConfigureAuthenticatedClient(client, Audience, AdministratorRole)
                : null,
            new Dictionary<string, string?>
            {
                [ApiAuthenticationDefaults.AuthorityConfigurationKey] = ApiTestAuthentication.Authority,
                [ApiAuthenticationDefaults.IssuerConfigurationKey] = ApiTestAuthentication.Authority
            });
    }

    public static WebApplicationFactory<BrandingApiHostEntryPoint> CreateAnonymous()
    {
        return Create(null, null, null, null, authenticateClient: false);
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

    public static void VerifyMappedMutationDependencies(WebApplicationFactory<BrandingApiHostEntryPoint> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);

        using var scope = factory.Services.CreateScope();
        _ = scope.ServiceProvider.GetRequiredService<IBrandingSettingsStore>();
        _ = scope.ServiceProvider.GetRequiredService<IOutputCacheStore>();
    }
}
