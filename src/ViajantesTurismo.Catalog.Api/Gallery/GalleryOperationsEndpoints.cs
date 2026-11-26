using Microsoft.AspNetCore.Http.HttpResults;
using ViajantesTurismo.Catalog.Application.Gallery;
using ViajantesTurismo.Catalog.Contracts.Application;

namespace ViajantesTurismo.Catalog.Api.Gallery;

/// <summary>Maps the Admin-only Gallery operations reads on the management Catalog group.</summary>
internal static class GalleryOperationsEndpoints
{
    /// <summary>Registers the bounded placement lookups and retry summary.</summary>
    /// <param name="managementCatalog">The Catalog management group.</param>
    internal static void MapGalleryOperationsEndpoints(this IEndpointRouteBuilder managementCatalog)
    {
        managementCatalog.MapGet("/operations/gallery/{catalogTourId:guid}/placements/{sequence:long}", GetGalleryPlacementStatus)
            .WithName(nameof(GetGalleryPlacementStatus))
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .RequireAuthorization(CatalogAuthorization.OperationsRead);
        managementCatalog.MapGet("/operations/gallery/original-wakes/{eventId:guid}/placement", GetGalleryPlacementByOriginalWake)
            .WithName(nameof(GetGalleryPlacementByOriginalWake))
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .RequireAuthorization(CatalogAuthorization.OperationsRead);
        managementCatalog.MapGet("/operations/gallery/retry-health", GetCatalogGalleryRetryHealth)
            .WithName(nameof(GetCatalogGalleryRetryHealth))
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .RequireAuthorization(CatalogAuthorization.OperationsRead);
    }

    private static async Task<Results<Ok<GetCatalogGalleryPlacementStatusDto>, BadRequest, NotFound>> GetGalleryPlacementStatus(
        Guid catalogTourId, long sequence, ICatalogGalleryOperationsQuery query, CancellationToken ct)
    {
        if (catalogTourId == Guid.Empty || sequence <= 0)
        {
            return TypedResults.BadRequest();
        }

        var placement = await query.Read(catalogTourId, sequence, ct);
        return placement is null ? TypedResults.NotFound() : TypedResults.Ok(placement);
    }

    private static async Task<Results<Ok<GetCatalogGalleryPlacementStatusDto>, BadRequest, NotFound>> GetGalleryPlacementByOriginalWake(
        Guid eventId, ICatalogGalleryOperationsQuery query, CancellationToken ct)
    {
        if (eventId == Guid.Empty)
        {
            return TypedResults.BadRequest();
        }

        var placement = await query.ReadByOriginalWake(eventId, ct);
        return placement is null ? TypedResults.NotFound() : TypedResults.Ok(placement);
    }

    private static async Task<Ok<GetCatalogGalleryRetryHealthDto>> GetCatalogGalleryRetryHealth(
        ICatalogGalleryOperationsQuery query, TimeProvider clock, CancellationToken ct) =>
        TypedResults.Ok(await query.ReadRetryHealth(clock.GetUtcNow(), ct));
}
