using SharedKernel.OpenApi;

namespace ViajantesTurismo.Catalog.ApiService;

/// <summary>
/// Registers the Catalog API's boundary-specific OpenAPI documents.
/// </summary>
internal static class CatalogOpenApiDocuments
{
    /// <summary>
    /// Gets the Catalog API's OpenAPI boundary document names.
    /// </summary>
    public static IReadOnlyCollection<string> OpenApiDocumentNames { get; } =
    [
        "catalog",
        "public-catalog"
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
            new OpenApiBoundaryDocument("catalog", "catalog"),
            new OpenApiBoundaryDocument("public-catalog", "public/catalog")
        ]);
    }
}
