using SharedKernel.ApiVersioning;
using SharedKernel.OpenApi;

namespace ViajantesTurismo.Branding.ApiService;

internal static class BrandingOpenApiDocuments
{
    public static ApiVersionDefinition CurrentApiVersion { get; } = new(new ApiVersion(1));

    public static IReadOnlyCollection<ApiVersionDefinition> ApiVersions { get; } =
    [
        CurrentApiVersion
    ];

    public static IReadOnlyCollection<string> OpenApiDocumentNames { get; } =
    [
        "branding",
        "public-branding",
        CurrentApiVersion.OpenApiDocumentName
    ];

    public static void AddBrandingOpenApiDocuments(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddBoundaryOpenApiDocuments(
        [
            new OpenApiBoundaryDocument("branding", "api/v1/branding"),
            new OpenApiBoundaryDocument("public-branding", "api/v1/public/branding")
        ]);
        services.AddApiVersionOpenApiDocuments(ApiVersions);
    }
}
