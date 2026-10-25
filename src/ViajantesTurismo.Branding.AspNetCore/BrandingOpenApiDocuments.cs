using PandaBear.ApiVersioning;
using PandaBear.OpenApi;

namespace ViajantesTurismo.Branding.AspNetCore;

internal static class BrandingOpenApiDocuments
{
    public static ApiVersionDefinition CurrentApiVersion { get; } = new(new ApiVersion(1));

    public static void AddBrandingOpenApiDocuments(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // The host's own version document already covers these routes; Branding adds only its boundary documents.
        services.AddBoundaryOpenApiDocuments(
        [
            new OpenApiBoundaryDocument("branding", "api/v1/branding"),
            new OpenApiBoundaryDocument("public-branding", "api/v1/public/branding")
        ]);
    }
}
