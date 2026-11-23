using PandaBear.ApiVersioning.AspNetCore;
using PandaBear.HttpCaching.AspNetCore;
using ViajantesTurismo.Catalog.Api.Gallery;
using ViajantesTurismo.Catalog.Api.Media;
using ViajantesTurismo.Catalog.Api.PublicContent;
using ViajantesTurismo.Catalog.Api.Tours;

namespace ViajantesTurismo.Catalog.Api;

/// <summary>Composes the Catalog management and public route groups.</summary>
internal static class CatalogEndpoints
{
    /// <summary>Maps all Catalog endpoints on the versioned API group.</summary>
    internal static IEndpointRouteBuilder MapCatalogEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var versionedApi = app.MapApiVersionGroup(CatalogOpenApiDocuments.CurrentApiVersion)
            .WithTags(nameof(CatalogEndpoints));
        var managementCatalog = versionedApi.MapGroup("/catalog")
            .WithNoStoreResponses($"/api/{CatalogOpenApiDocuments.CurrentApiVersion.RouteSegment}/catalog");

        versionedApi.MapCatalogTourEndpoints(managementCatalog);
        versionedApi.MapCatalogGalleryEndpoints();
        versionedApi.MapCatalogMediaEndpoints(managementCatalog);
        versionedApi.MapCatalogPublicContentEndpoints(managementCatalog);

        return app;
    }
}
