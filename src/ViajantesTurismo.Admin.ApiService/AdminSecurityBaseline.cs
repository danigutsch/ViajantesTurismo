using SharedKernel.AspNetCore;

namespace ViajantesTurismo.Admin.ApiService;

internal static class AdminSecurityBaseline
{
    public const string CorsPolicyName = "admin-api-cors";

    public const string MutationRateLimitPolicy = "admin-mutation";

    public const string ImportRateLimitPolicy = "admin-import";

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
