using PandaBear.AspNetCore.RateLimiting;

namespace ViajantesTurismo.Public.Web;

/// <summary>Configures rate limiting for Public Web.</summary>
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
}
