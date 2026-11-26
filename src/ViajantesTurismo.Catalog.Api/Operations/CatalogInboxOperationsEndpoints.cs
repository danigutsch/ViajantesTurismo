using Microsoft.AspNetCore.Http.HttpResults;
using ViajantesTurismo.Catalog.Application.IntegrationEvents;
using ViajantesTurismo.Catalog.Contracts.Application;

namespace ViajantesTurismo.Catalog.Api.Operations;

/// <summary>Maps the Catalog-owned read-only inbox inspection for Operations.</summary>
internal static class CatalogInboxOperationsEndpoints
{
    /// <summary>Registers the summary and recent history under the existing Admin Operations authority.</summary>
    internal static void MapCatalogInboxOperationsEndpoints(this IEndpointRouteBuilder managementCatalog)
    {
        managementCatalog.MapGet("/operations/messaging/inbox", GetCatalogInboxHealth)
            .WithName(nameof(GetCatalogInboxHealth))
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .RequireAuthorization(CatalogAuthorization.OperationsRead);
        managementCatalog.MapGet("/operations/messaging/inbox/duplicates", GetCatalogInboxDuplicates)
            .WithName(nameof(GetCatalogInboxDuplicates))
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .RequireAuthorization(CatalogAuthorization.OperationsRead);
    }

    private static async Task<Ok<GetCatalogInboxHealthDto>> GetCatalogInboxHealth(
        ICatalogInboxOperationsQuery query, CancellationToken ct) =>
        TypedResults.Ok(await query.ReadHealth(ct));

    private static async Task<Results<Ok<GetCatalogInboxDuplicatePageDto>, BadRequest>> GetCatalogInboxDuplicates(
        Guid? eventId, DateTimeOffset? afterObservedAt, Guid? afterEventId, int? limit, DateTimeOffset? observedSince,
        ICatalogInboxOperationsQuery query, CancellationToken ct)
    {
        var pageSize = limit ?? CatalogInboxDuplicateFilters.MaxPageSize;
        if (pageSize is < 1 or > CatalogInboxDuplicateFilters.MaxPageSize
            || eventId == Guid.Empty || afterEventId == Guid.Empty
            || afterObservedAt.HasValue != afterEventId.HasValue)
        {
            return TypedResults.BadRequest();
        }

        var filters = new CatalogInboxDuplicateFilters(eventId, afterObservedAt, afterEventId, pageSize, observedSince);
        return TypedResults.Ok(await query.ReadPage(filters, ct));
    }
}
