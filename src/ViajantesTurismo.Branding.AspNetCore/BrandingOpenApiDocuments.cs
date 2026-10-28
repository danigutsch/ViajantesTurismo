using PandaBear.ApiVersioning;
using PandaBear.OpenApi;

namespace ViajantesTurismo.Branding.AspNetCore;

/// <summary>Registers Branding OpenAPI documents.</summary>
internal static class BrandingOpenApiDocuments
{
    /// <summary>API version served by the Branding endpoints.</summary>
    public static ApiVersionDefinition CurrentApiVersion { get; } = new(new ApiVersion(1));

    /// <summary>Adds the Branding boundary OpenAPI documents.</summary>
    /// <remarks>The host's own version document already covers these routes; Branding adds only its boundary documents.</remarks>
    public static void AddBrandingOpenApiDocuments(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddBoundaryOpenApiDocuments(
        [
            new OpenApiBoundaryDocument("branding", "api/v1/branding"),
            new OpenApiBoundaryDocument("public-branding", "api/v1/public/branding")
        ]);
    }
}
