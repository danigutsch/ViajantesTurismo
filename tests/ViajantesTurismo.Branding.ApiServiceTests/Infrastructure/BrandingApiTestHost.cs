using Microsoft.Extensions.Diagnostics.HealthChecks;
using SharedKernel.Testing.AspNetCore;
using ViajantesTurismo.Branding.ApiService;

namespace ViajantesTurismo.Branding.ApiServiceTests.Infrastructure;

internal static class BrandingApiTestHost
{
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
        TestBrandingSettingsStore? store)
    {
        return WebApplicationTestHost.Create<BrandingApiHostEntryPoint>(
            environment,
            services =>
            {
                services.Replace(ServiceDescriptor.Singleton<IBrandingSettingsStore>(store ?? new TestBrandingSettingsStore()));
                services.Configure<HealthCheckServiceOptions>(options => options.Registrations.Clear());
            });
    }
}
