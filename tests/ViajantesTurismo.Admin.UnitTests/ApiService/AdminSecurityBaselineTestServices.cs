using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ViajantesTurismo.Admin.ApiService;

namespace ViajantesTurismo.Admin.UnitTests.ApiService;

internal static class AdminSecurityBaselineTestServices
{
    internal static CorsOptions GetCorsOptions(IConfiguration configuration)
    {
        var services = new ServiceCollection();
        services.AddAdminSecurityBaseline(configuration);

        using var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IOptions<CorsOptions>>().Value;
    }
}
