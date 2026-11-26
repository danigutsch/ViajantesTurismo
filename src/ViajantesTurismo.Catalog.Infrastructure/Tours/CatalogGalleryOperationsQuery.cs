using Microsoft.EntityFrameworkCore;
using SharedKernel.Messaging.IntegrationEvents.EntityFrameworkCore;
using ViajantesTurismo.Catalog.Application.Gallery;
using ViajantesTurismo.Catalog.Contracts.Application;
using ViajantesTurismo.Catalog.Contracts.IntegrationEvents.Tours;

namespace ViajantesTurismo.Catalog.Infrastructure.Tours;

/// <summary>Projects the bounded Gallery placement and associated wake deliveries to the operations contract.</summary>
internal sealed class CatalogGalleryOperationsQuery(CatalogGalleryProcessStatusReader reader, CatalogDbContext database) : ICatalogGalleryOperationsQuery
{
    private const int MaximumRelatedPendingWakes = 2;

    /// <inheritdoc />
    public ValueTask<GetCatalogGalleryRetryHealthDto> ReadRetryHealth(DateTimeOffset observedAt, CancellationToken ct)
    {
        return reader.ReadRetryHealth(observedAt, ct);
    }

    /// <inheritdoc />
    public async ValueTask<GetCatalogGalleryPlacementStatusDto?> ReadByOriginalWake(Guid eventId, CancellationToken ct)
    {
        var key = await database.Set<CatalogGalleryPlacementIntentEntity>().AsNoTracking()
            .Where(row => row.Id == eventId)
            .Select(static row => new { row.CatalogTourId, row.Sequence })
            .SingleOrDefaultAsync(ct).ConfigureAwait(false);
        return key is null ? null : await Read(key.CatalogTourId, key.Sequence, ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask<GetCatalogGalleryPlacementStatusDto?> Read(Guid catalogTourId, long sequence, CancellationToken ct)
    {
        var status = await reader.Read(catalogTourId, sequence, ct).ConfigureAwait(false);
        if (status is null)
        {
            return null;
        }

        var wake = await IntegrationEventDeliveryInspection.ReadRetainedOutbox(database, status.IntentId.ToString("D"), ct).ConfigureAwait(false);
        var tourIntents = database.Set<CatalogGalleryPlacementIntentEntity>().AsNoTracking()
            .Where(row => row.CatalogTourId == catalogTourId);
        Guid? triggeringWakeEventId = null;
        IntegrationEventOutboxDelivery? triggeringWake = null;
        if (status.Status != CatalogGalleryPlacementStatusDto.ClosedByRetirement && status.CausationEventId is { } cause
            && (cause == status.IntentId
                || await tourIntents.AnyAsync(row => row.Id == cause || row.BootstrapWakeId == cause, ct).ConfigureAwait(false)))
        {
            var retained = cause == status.IntentId
                ? wake
                : await IntegrationEventDeliveryInspection.ReadRetainedOutbox(database, cause.ToString("D"), ct).ConfigureAwait(false);
            if (retained is null || retained.EventType == CatalogGalleryPlacementQueuedIntegrationEvent.EventType)
            {
                triggeringWakeEventId = cause;
                triggeringWake = retained;
            }
        }

        var placement = tourIntents.Where(row => row.Sequence == sequence);
        var eventIds = placement.Select(row => row.Id.ToString())
            .Concat(placement.Where(row => row.BootstrapWakeId.HasValue).Select(row => row.BootstrapWakeId!.Value.ToString()));
        var pending = await IntegrationEventDeliveryInspection.ReadMatchingOutbox(database, eventIds,
            new IntegrationEventDeliveryFilter(null, CatalogGalleryPlacementQueuedIntegrationEvent.EventType, null),
            MaximumRelatedPendingWakes, cursor: null, ct).ConfigureAwait(false);

        return new GetCatalogGalleryPlacementStatusDto(
            catalogTourId, sequence, status.IntentId, status.Status, status.Revision, status.AcceptedAt, status.RecordedAt)
        {
            TerminalEventId = status.TerminalEventId,
            CausationEventId = status.CausationEventId,
            TriggeringWakeEventId = triggeringWakeEventId,
            Retirement = status.Retirement,
            OriginalWake = ToDeliveryMetadata(wake),
            TriggeringWake = ToDeliveryMetadata(triggeringWake),
            RelatedPendingWakes = pending.Entries.Count == 0 ? null : pending.Entries.Select(static delivery =>
                new CatalogGalleryPendingWakeDeliveryDto(Guid.ParseExact(delivery.EventId, "D"), delivery.RecordedAt,
                    delivery.Attempts, delivery.LastAttemptAt, delivery.NextAttemptAt)).ToArray(),
        };
    }

    private static CatalogGalleryOriginalWakeDeliveryDto? ToDeliveryMetadata(IntegrationEventOutboxDelivery? wake) =>
        wake is null ? null : new(wake.RecordedAt, wake.PublishedAt, wake.FailedAttempts, wake.LastAttemptAt, wake.NextAttemptAt);
}
