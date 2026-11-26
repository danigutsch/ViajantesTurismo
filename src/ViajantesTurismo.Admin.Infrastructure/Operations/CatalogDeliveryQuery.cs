using SharedKernel.Messaging.IntegrationEvents.EntityFrameworkCore;
using ViajantesTurismo.Admin.Application.Operations;
using ViajantesTurismo.Admin.Contracts.Application;
using ViajantesTurismo.Resources;

namespace ViajantesTurismo.Admin.Infrastructure.Operations;

/// <summary>Projects SharedKernel delivery metadata from Admin's read-only transport mapping.</summary>
internal sealed class CatalogDeliveryQuery(AdminReadDbContext dbContext) : ICatalogDeliveryQuery
{
    /// <inheritdoc />
    public async Task<GetCatalogDeliveryPageDto> ReadCatalogDelivery(CatalogDeliveryRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var filter = new IntegrationEventDeliveryFilter(request.EventId, request.EventType, request.RecordedSince);
        IntegrationEventDeliveryCursor? cursor = request.AfterRecordedAt is { } recordedAt && request.AfterStorageId is { } storageId
            ? new IntegrationEventDeliveryCursor(recordedAt, storageId)
            : null;
        var page = await IntegrationEventDeliveryInspection.ReadTransport(
            dbContext, IntegrationEventConsumerNames.Catalog, filter, request.Limit, cursor, ct);
        return new GetCatalogDeliveryPageDto
        {
            Rows = [.. page.Entries.Select(static item => new CatalogDeliveryRowDto
            {
                StorageId = item.StorageId,
                EventId = item.EventId,
                EventType = item.EventType,
                RecordedAt = item.RecordedAt,
                Attempts = item.Attempts,
                LastAttemptAt = item.LastAttemptAt,
                NextAttemptAt = item.NextAttemptAt,
                TerminalAt = item.TerminalAt,
            })],
            NextRecordedAt = page.NextCursor?.RecordedAt,
            NextStorageId = page.NextCursor?.StorageId,
        };
    }
}
