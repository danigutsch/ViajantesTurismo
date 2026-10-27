using PandaBear.AspNetCore;

namespace ViajantesTurismo.Admin.ApiService;

/// <summary>Registers the CORS and rate-limiting baseline shared by Admin API endpoints.</summary>
internal static class AdminSecurityBaseline
{
    /// <summary>Name of the CORS policy built from the configured allowed origins.</summary>
    public const string CorsPolicyName = "admin-api-cors";

    /// <summary>Name of the per-IP rate-limit policy for mutating endpoints.</summary>
    public const string MutationRateLimitPolicy = "admin-mutation";

    /// <summary>Name of the stricter per-IP rate-limit policy for import endpoints.</summary>
    public const string ImportRateLimitPolicy = "admin-import";

    /// <summary>Adds the Admin CORS policy and the mutation and import rate limiters.</summary>
    public static IServiceCollection AddAdminSecurityBaseline(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddCors(options => options.AddConfiguredOriginsPolicy(
            CorsPolicyName,
            configuration.GetSection("Security:Cors:AllowedOrigins")));

        services.AddRateLimiter(options =>
            options.AddRemoteIpFixedWindowPolicies([
                new RemoteIpFixedWindowRateLimitPolicy(MutationRateLimitPolicy, 300, TimeSpan.FromMinutes(1)),
                new RemoteIpFixedWindowRateLimitPolicy(ImportRateLimitPolicy, 5, TimeSpan.FromMinutes(1))
            ]));

        return services;
    }
}
