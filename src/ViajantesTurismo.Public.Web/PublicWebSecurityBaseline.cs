using Microsoft.Extensions.Http.Resilience;
using PandaBear.AspNetCore.RateLimiting;
using PandaBear.HttpClients;

namespace ViajantesTurismo.Public.Web;

/// <summary>Configures rate limiting and catalog client resilience for Public Web.</summary>
internal static class PublicWebSecurityBaseline
{
    /// <summary>Name of the rate limit policy applied to public read endpoints.</summary>
    internal const string PublicReadRateLimitPolicy = "public-web-read";

    /// <summary>Adds the per-remote-IP fixed-window rate limiter and a no-store rejection response with Retry-After.</summary>
    public static IServiceCollection AddPublicWebSecurityBaseline(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddRateLimiter(options =>
        {
            options.AddRemoteIpFixedWindowPolicy(PublicReadRateLimitPolicy, 120, TimeSpan.FromMinutes(1));
            options.UseNoStoreRetryAfterRejection();
        });

        return services;
    }

    /// <summary>Stops retries and circuit-breaker counting for public catalog quota (429) responses.</summary>
    public static void ConfigureCatalogResilience(HttpStandardResilienceOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        options.DisableRetryOnTooManyRequests(static request => request.RequestUri is { IsAbsoluteUri: true } uri
            && uri.AbsolutePath.StartsWith(PublicCatalogApiClient.RoutePrefix + "/", StringComparison.OrdinalIgnoreCase));
    }
}
