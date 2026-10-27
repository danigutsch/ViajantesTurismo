using PandaBear.AspNetCore;

namespace ViajantesTurismo.Catalog.ApiService;

/// <summary>Registers CORS and rate limiting for the Catalog API.</summary>
internal static class CatalogSecurityBaseline
{
    /// <summary>Name of the CORS policy.</summary>
    public const string CorsPolicyName = "catalog-api-cors";

    /// <summary>Name of the rate limit policy for public reads.</summary>
    public const string PublicReadRateLimitPolicy = "catalog-public-read";

    /// <summary>Name of the rate limit policy for mutations.</summary>
    public const string MutationRateLimitPolicy = "catalog-mutation";

    /// <summary>Requests allowed per remote IP per minute on public reads.</summary>
    public const int PublicReadPermitLimit = 240;

    /// <summary>Adds the Catalog CORS policy and remote-IP fixed-window rate limits.</summary>
    public static IServiceCollection AddCatalogSecurityBaseline(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddCors(options => options.AddConfiguredOriginsPolicy(
            CorsPolicyName,
            configuration.GetSection("Security:Cors:AllowedOrigins")));

        services.AddRateLimiter(options =>
            options.AddRemoteIpFixedWindowPolicies([
                new RemoteIpFixedWindowRateLimitPolicy(PublicReadRateLimitPolicy, PublicReadPermitLimit, TimeSpan.FromMinutes(1)),
                new RemoteIpFixedWindowRateLimitPolicy(MutationRateLimitPolicy, 20, TimeSpan.FromMinutes(1))
            ]));

        return services;
    }
}
