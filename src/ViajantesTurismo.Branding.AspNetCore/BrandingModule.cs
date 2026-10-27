using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Hosting;
using PandaBear.AspNetCore;
using ViajantesTurismo.Branding.Infrastructure;

namespace ViajantesTurismo.Branding.AspNetCore;

/// <summary>
/// Composes the Branding HTTP module into Catalog.ApiService: routes, validation, the revision-aware writer, cache,
/// rate limits, CORS, permissions, and the Branding OpenAPI boundary documents.
/// </summary>
public static class BrandingModule
{
    /// <summary>The bearer scheme a co-hosting API registers for the Branding audience.</summary>
    public const string BearerScheme = "BrandingBearer";

    /// <summary>The Branding role-to-permission table.</summary>
    public static IReadOnlyDictionary<string, IReadOnlyCollection<string>> PermissionsByRole => BrandingAuthorization.PermissionsByRole;

    /// <summary>Adds Branding persistence, the outbox, CORS, rate-limit policies, and OpenAPI boundary documents.</summary>
    /// <param name="builder">The host builder.</param>
    /// <param name="addOutboxRelay">Whether this host relays the Branding outbox.</param>
    /// <typeparam name="TBuilder">The host builder type.</typeparam>
    /// <returns>The host builder.</returns>
    public static TBuilder AddBrandingModule<TBuilder>(this TBuilder builder, bool addOutboxRelay)
        where TBuilder : IHostApplicationBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddSingleton<BrandingTelemetry>();
        builder.Services.AddBrandingSecurityBaseline(builder.Configuration);
        builder.Services.AddBrandingOpenApiDocuments();
        builder.AddBrandingInfrastructure(addOutboxRelay);
        return builder;
    }

    /// <summary>
    /// Adds the Branding read and write policies, which only <see cref="BearerScheme" /> (Branding-audience) tokens
    /// satisfy.
    /// </summary>
    /// <param name="authorization">The authorization builder.</param>
    /// <returns>The authorization builder.</returns>
    public static AuthorizationBuilder AddBrandingPolicies(this AuthorizationBuilder authorization)
    {
        ArgumentNullException.ThrowIfNull(authorization);

        return authorization
            .AddPolicy(BrandingAuthorization.BrandingRead, policy => policy.AddAuthenticationSchemes(BearerScheme).RequirePermission(BrandingAuthorization.BrandingRead))
            .AddPolicy(BrandingAuthorization.BrandingWrite, policy => policy.AddAuthenticationSchemes(BearerScheme).RequirePermission(BrandingAuthorization.BrandingWrite));
    }

    /// <summary>Maps the public and management Branding routes.</summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <returns>The endpoint route builder.</returns>
    public static IEndpointRouteBuilder MapBrandingModule(this IEndpointRouteBuilder endpoints) => endpoints.MapBrandingEndpoints();
}
