using Microsoft.AspNetCore.Authorization;
using PandaBear.AspNetCore;
using ViajantesTurismo.Branding.AspNetCore;
using ViajantesTurismo.Resources;

namespace ViajantesTurismo.Catalog.ApiService;

/// <summary>Registers the Catalog API security composition.</summary>
internal static class CatalogApiSecurityExtensions
{
    /// <summary>
    /// Registers Catalog and the co-hosted Branding module, which each accept only their own audience; no scheme
    /// authenticates by default.
    /// </summary>
    /// <remarks>
    /// Catalog routes that use the default or fallback policy authenticate only Catalog-audience tokens; every
    /// Branding route names its own Branding policy or is anonymous (CatalogHostedBrandingRouteTests).
    /// </remarks>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <param name="environment">The host environment.</param>
    public static void AddCatalogAndBrandingApiSecurity(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        services.AddApiSecurity(
            configuration,
            environment,
            [
                new ApiBearerScheme(CatalogAuthorization.BearerScheme, ApiAudienceNames.Catalog, CatalogAuthorization.PermissionsByRole),
                new ApiBearerScheme(BrandingModule.BearerScheme, ApiAudienceNames.Branding, BrandingModule.PermissionsByRole)
            ])
        .AddPolicy(CatalogAuthorization.CatalogRead, policy => policy.AddAuthenticationSchemes(CatalogAuthorization.BearerScheme).RequirePermission(CatalogAuthorization.CatalogRead))
        .AddPolicy(CatalogAuthorization.CatalogWrite, policy => policy.AddAuthenticationSchemes(CatalogAuthorization.BearerScheme).RequirePermission(CatalogAuthorization.CatalogWrite))
        .AddPolicy(CatalogAuthorization.CatalogPublish, policy => policy.AddAuthenticationSchemes(CatalogAuthorization.BearerScheme).RequirePermission(CatalogAuthorization.CatalogPublish))
        .AddPolicy(CatalogAuthorization.MediaAi, policy => policy.AddAuthenticationSchemes(CatalogAuthorization.BearerScheme).RequirePermission(CatalogAuthorization.MediaAi))
        .AddBrandingPolicies()
        .SetDefaultPolicy(new AuthorizationPolicyBuilder(CatalogAuthorization.BearerScheme).RequireAuthenticatedUser().Build())
        .SetFallbackPolicy(new AuthorizationPolicyBuilder(CatalogAuthorization.BearerScheme).RequireAuthenticatedUser().Build());
    }
}
