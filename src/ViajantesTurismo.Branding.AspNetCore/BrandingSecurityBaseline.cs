using PandaBear.AspNetCore;

namespace ViajantesTurismo.Branding.AspNetCore;

/// <summary>Registers CORS and rate limiting for the Branding API.</summary>
internal static class BrandingSecurityBaseline
{
    /// <summary>Name of the Branding CORS policy.</summary>
    public const string CorsPolicyName = "branding-api-cors";

    /// <summary>Rate limit policy name for public reads.</summary>
    public const string PublicReadRateLimitPolicy = "branding-public-read";

    /// <summary>Rate limit policy name for management mutations.</summary>
    public const string MutationRateLimitPolicy = "branding-mutation";

    /// <summary>Adds the Branding CORS and per-IP rate limit policies.</summary>
    public static IServiceCollection AddBrandingSecurityBaseline(this IServiceCollection services, IConfiguration configuration)
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
