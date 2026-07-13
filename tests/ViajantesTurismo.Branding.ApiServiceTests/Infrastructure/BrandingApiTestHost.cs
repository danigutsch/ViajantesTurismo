using Microsoft.Extensions.Diagnostics.HealthChecks;
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
        return Create(environment, null);
    }

    public static WebApplicationFactory<BrandingApiHostEntryPoint> Create(TestBrandingSettingsStore store)
    {
        return Create(null, store);
    }

    private static WebApplicationFactory<BrandingApiHostEntryPoint> Create(
        string? environment,
        TestBrandingSettingsStore? store,
        bool authenticateClient = true)
    {
        return WebApplicationTestHost.Create<BrandingApiHostEntryPoint>(
            environment,
            services =>
            {
                services.Replace(ServiceDescriptor.Singleton<IBrandingSettingsStore>(store ?? new TestBrandingSettingsStore()));
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
        return Create(null, null, authenticateClient: false);
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
}
