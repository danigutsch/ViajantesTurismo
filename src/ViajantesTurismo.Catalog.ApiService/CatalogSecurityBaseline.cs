using SharedKernel.AspNetCore;

namespace ViajantesTurismo.Catalog.ApiService;

internal static class CatalogSecurityBaseline
{
    public const string CorsPolicyName = "catalog-api-cors";

    public const string PublicReadRateLimitPolicy = "catalog-public-read";

    public const string MutationRateLimitPolicy = "catalog-mutation";

    public static IServiceCollection AddCatalogSecurityBaseline(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddCors(options => options.AddConfiguredOriginsPolicy(
            CorsPolicyName,
            configuration.GetSection("Security:Cors:AllowedOrigins")));

        services.AddRateLimiter(options =>
            options.AddRemoteIpFixedWindowPolicies([
                new RemoteIpFixedWindowRateLimitPolicy(PublicReadRateLimitPolicy, 60, TimeSpan.FromMinutes(1)),
                new RemoteIpFixedWindowRateLimitPolicy(MutationRateLimitPolicy, 20, TimeSpan.FromMinutes(1))
            ]));

        return services;
    }
}
