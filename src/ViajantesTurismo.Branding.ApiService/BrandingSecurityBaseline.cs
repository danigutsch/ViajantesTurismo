using SharedKernel.AspNetCore;

namespace ViajantesTurismo.Branding.ApiService;

internal static class BrandingSecurityBaseline
{
    public const string CorsPolicyName = "branding-api-cors";

    public const string PublicReadRateLimitPolicy = "branding-public-read";

    public const string MutationRateLimitPolicy = "branding-mutation";

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
