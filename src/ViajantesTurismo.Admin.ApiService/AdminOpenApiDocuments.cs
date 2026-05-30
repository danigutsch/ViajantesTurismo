using SharedKernel.OpenApi;

namespace ViajantesTurismo.Admin.ApiService;

/// <summary>
/// Registers the Admin API's boundary-specific OpenAPI documents.
/// </summary>
internal static class AdminOpenApiDocuments
{
    /// <summary>
    /// Adds the Admin API's named OpenAPI documents to the service collection.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    public static void AddAdminOpenApiDocuments(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddBoundaryOpenApiDocuments(AdminRouteGroupExtensions.OpenApiDocumentNames);
    }
}
