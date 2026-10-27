using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ViajantesTurismo.Admin.ApiService;

namespace ViajantesTurismo.Admin.UnitTests.ApiService;

/// <summary>Builds service providers for admin security baseline tests.</summary>
internal static class AdminSecurityBaselineTestServices
{
    /// <summary>Registers the admin security baseline for the configuration and returns the resulting CORS options.</summary>
    internal static CorsOptions GetCorsOptions(IConfiguration configuration)
    {
        var services = new ServiceCollection();
        services.AddAdminSecurityBaseline(configuration);

        using var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IOptions<CorsOptions>>().Value;
    }
}
