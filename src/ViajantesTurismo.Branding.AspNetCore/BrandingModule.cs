using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Hosting;
using PandaBear.AspNetCore;
using ViajantesTurismo.Branding.Infrastructure;

namespace ViajantesTurismo.Branding.AspNetCore;

/// <summary>
/// Composes the Branding HTTP module into a host: the standalone Branding API or Catalog.ApiService. Both hosts use
/// the same routes, validation, revision-aware writer, cache, rate limits, CORS, and permissions.
/// </summary>
public static class BrandingModule
{
    /// <summary>The bearer scheme a co-hosting API registers for the Branding audience.</summary>
    public const string BearerScheme = "BrandingBearer";

    /// <summary>The Branding role-to-permission table.</summary>
    public static IReadOnlyDictionary<string, IReadOnlyCollection<string>> PermissionsByRole => BrandingAuthorization.PermissionsByRole;

    /// <summary>Adds Branding persistence, the outbox, CORS, and rate-limit policies.</summary>
    /// <param name="builder">The host builder.</param>
    /// <param name="addOutboxRelay">Whether this host relays the Branding outbox.</param>
    /// <typeparam name="TBuilder">The host builder type.</typeparam>
    /// <returns>The host builder.</returns>
    public static TBuilder AddBrandingModule<TBuilder>(this TBuilder builder, bool addOutboxRelay)
        where TBuilder : IHostApplicationBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddBrandingSecurityBaseline(builder.Configuration);
        builder.AddBrandingInfrastructure(addOutboxRelay);
        return builder;
    }

    /// <summary>
    /// Adds the Branding read and write policies. A co-hosting API passes <see cref="BearerScheme" /> so only
    /// Branding-audience tokens satisfy them; the standalone host uses its default scheme.
    /// </summary>
    /// <param name="authorization">The authorization builder.</param>
    /// <param name="authenticationScheme">The scheme the policies authenticate with, or <see langword="null" /> for the default.</param>
    /// <returns>The authorization builder.</returns>
    public static AuthorizationBuilder AddBrandingPolicies(this AuthorizationBuilder authorization, string? authenticationScheme)
    {
        ArgumentNullException.ThrowIfNull(authorization);

        return authorization
            .AddPolicy(BrandingAuthorization.BrandingRead, policy => Configure(policy, authenticationScheme, BrandingAuthorization.BrandingRead))
            .AddPolicy(BrandingAuthorization.BrandingWrite, policy => Configure(policy, authenticationScheme, BrandingAuthorization.BrandingWrite));
    }

    /// <summary>Maps the public and management Branding routes.</summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <returns>The endpoint route builder.</returns>
    public static IEndpointRouteBuilder MapBrandingModule(this IEndpointRouteBuilder endpoints) => endpoints.MapBrandingEndpoints();

    private static void Configure(AuthorizationPolicyBuilder policy, string? authenticationScheme, string permission)
    {
        if (authenticationScheme is not null)
        {
            policy.AddAuthenticationSchemes(authenticationScheme);
        }

        policy.RequirePermission(permission);
    }
}
