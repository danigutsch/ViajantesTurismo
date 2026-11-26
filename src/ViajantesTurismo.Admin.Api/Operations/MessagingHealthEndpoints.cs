using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using ViajantesTurismo.Admin.Application.Operations;
using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.Api.Operations;

/// <summary>Provides read-only endpoints for messaging health.</summary>
internal static class MessagingHealthEndpoints
{
    /// <summary>Maps the Admin-only messaging-health query.</summary>
    public static void MapMessagingHealthEndpoints(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var operations = app.MapOperationsGroup(AdminAuthorization.OperationsRead);
        operations.MapGet("/messaging", Get)
            .WithAdminMetadata("GetMessagingHealth", "Reads retry and terminal aggregates for Admin-to-Catalog delivery.",
                "Gets messaging health.");
        operations.MapGet("/messaging/catalog-delivery", GetCatalogDelivery)
            .WithAdminMetadata("GetCatalogDelivery", "Reads a bounded metadata page for unfinished Admin-to-Catalog deliveries.",
                "Gets Catalog delivery metadata.");
        operations.MapGet("/messaging/outbox", GetOutboxDelivery)
            .WithAdminMetadata("GetOutboxDelivery",
                "Reads bounded pending publication metadata, or retained publication metadata by exact event ID.",
                "Gets Admin outbox metadata.");
    }

    private static async Task<Ok<GetMessagingHealthDto>> Get([FromServices] IMessagingHealthQuery query, CancellationToken ct)
    {
        var health = await query.ReadMessagingHealth(ct);
        return TypedResults.Ok(health);
    }

    private static async Task<Results<Ok<GetCatalogDeliveryPageDto>, BadRequest>> GetCatalogDelivery(
        [FromServices] ICatalogDeliveryQuery query, string? eventId, string? eventType,
        DateTimeOffset? recordedSince, DateTimeOffset? afterRecordedAt, Guid? afterStorageId, int? limit,
        CancellationToken ct)
    {
        var pageSize = limit ?? ContractConstants.DefaultCatalogDeliveryPageSize;
        if (pageSize is < 1 or > ContractConstants.MaxCatalogDeliveryPageSize
            || afterRecordedAt.HasValue != afterStorageId.HasValue)
        {
            return TypedResults.BadRequest();
        }

        var request = new CatalogDeliveryRequest(eventId, eventType, recordedSince, afterRecordedAt, afterStorageId, pageSize);
        return TypedResults.Ok(await query.ReadCatalogDelivery(request, ct));
    }

    private static async Task<Results<Ok<GetOutboxDeliveryPageDto>, BadRequest>> GetOutboxDelivery(
        [FromServices] IOutboxDeliveryQuery query, string? eventId, string? eventType,
        DateTimeOffset? recordedSince, DateTimeOffset? afterRecordedAt, Guid? afterStorageId, int? limit, bool? includePublished,
        CancellationToken ct)
    {
        var pageSize = limit ?? ContractConstants.DefaultOutboxDeliveryPageSize;
        if (pageSize is < 1 or > ContractConstants.MaxOutboxDeliveryPageSize
            || afterRecordedAt.HasValue != afterStorageId.HasValue
            || (includePublished is true && (string.IsNullOrWhiteSpace(eventId) || afterRecordedAt.HasValue)))
        {
            return TypedResults.BadRequest();
        }

        var request = new OutboxDeliveryRequest(eventId, eventType, recordedSince, afterRecordedAt, afterStorageId, pageSize)
        {
            IncludePublished = includePublished is true
        };
        return TypedResults.Ok(await query.ReadOutboxDelivery(request, ct));
    }
}
