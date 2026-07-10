using SharedKernel.ApiVersioning;
using SharedKernel.OpenApi;

namespace ViajantesTurismo.Catalog.ApiService;

/// <summary>
/// Registers the Catalog API's boundary-specific OpenAPI documents.
/// </summary>
internal static class CatalogOpenApiDocuments
{
    /// <summary>
    /// Gets the Catalog API's current HTTP contract version.
    /// </summary>
    public static ApiVersionDefinition CurrentApiVersion { get; } = new(new ApiVersion(1));

    /// <summary>
    /// Gets the Catalog API's active HTTP contract versions.
    /// </summary>
    public static IReadOnlyCollection<ApiVersionDefinition> ApiVersions { get; } =
    [
        CurrentApiVersion
    ];

    /// <summary>
    /// Gets the Catalog API's OpenAPI boundary document names.
    /// </summary>
    public static IReadOnlyCollection<string> OpenApiDocumentNames { get; } =
    [
        "catalog",
        "public-catalog",
        CurrentApiVersion.OpenApiDocumentName
    ];

    /// <summary>
    /// Adds the Catalog API's named OpenAPI documents to the service collection.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    public static void AddCatalogOpenApiDocuments(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddBoundaryOpenApiDocuments(
        [
            new OpenApiBoundaryDocument("catalog", "api/v1/catalog"),
            new OpenApiBoundaryDocument("public-catalog", "api/v1/public/catalog")
        ]);
        services.AddApiVersionOpenApiDocuments(ApiVersions);
    }
}
