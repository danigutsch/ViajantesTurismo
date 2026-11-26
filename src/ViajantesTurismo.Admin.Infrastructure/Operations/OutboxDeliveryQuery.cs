using SharedKernel.Messaging.IntegrationEvents.EntityFrameworkCore;
using ViajantesTurismo.Admin.Application.Operations;
using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.Infrastructure.Operations;

/// <summary>Projects SharedKernel publication metadata from the Admin read-only outbox mapping.</summary>
internal sealed class OutboxDeliveryQuery(AdminReadDbContext dbContext) : IOutboxDeliveryQuery
{
    /// <inheritdoc />
    public async Task<GetOutboxDeliveryPageDto> ReadOutboxDelivery(OutboxDeliveryRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.IncludePublished)
        {
            return await ReadExactDelivery(request, ct).ConfigureAwait(false);
        }

        var filter = new IntegrationEventDeliveryFilter(request.EventId, request.EventType, request.RecordedSince);
        IntegrationEventDeliveryCursor? cursor = request.AfterRecordedAt is { } recordedAt && request.AfterStorageId is { } storageId
            ? new IntegrationEventDeliveryCursor(recordedAt, storageId)
            : null;
        var page = await IntegrationEventDeliveryInspection.ReadOutbox(
            dbContext, filter, request.Limit, cursor, ct);
        return new GetOutboxDeliveryPageDto
        {
            Rows = [.. page.Entries.Select(static item => new OutboxDeliveryRowDto
            {
                StorageId = item.StorageId,
                EventId = item.EventId,
                EventType = item.EventType,
                RecordedAt = item.RecordedAt,
                Attempts = item.Attempts,
                LastAttemptAt = item.LastAttemptAt,
                NextAttemptAt = item.NextAttemptAt,
            })],
            NextRecordedAt = page.NextCursor?.RecordedAt,
            NextStorageId = page.NextCursor?.StorageId,
        };
    }

    private async Task<GetOutboxDeliveryPageDto> ReadExactDelivery(OutboxDeliveryRequest request, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.EventId);
        var delivery = await IntegrationEventDeliveryInspection.ReadRetainedOutbox(dbContext, request.EventId, ct).ConfigureAwait(false);
        OutboxDeliveryRowDto[] rows = [];
        if (delivery is not null
            && (request.EventType is null || string.Equals(delivery.EventType, request.EventType, StringComparison.Ordinal))
            && (request.RecordedSince is not { } recordedSince || delivery.RecordedAt >= recordedSince))
        {
            rows =
            [
                new OutboxDeliveryRowDto
                {
                    StorageId = delivery.StorageId,
                    EventId = delivery.EventId,
                    EventType = delivery.EventType,
                    RecordedAt = delivery.RecordedAt,
                    Attempts = delivery.FailedAttempts,
                    LastAttemptAt = delivery.LastAttemptAt,
                    NextAttemptAt = delivery.NextAttemptAt,
                    PublishedAt = delivery.PublishedAt,
                },
            ];
        }

        return new GetOutboxDeliveryPageDto
        {
            Rows = rows,
            NextRecordedAt = null,
            NextStorageId = null,
            IncludesPublished = true,
        };
    }
}
