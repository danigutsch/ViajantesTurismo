using Microsoft.EntityFrameworkCore;
using SharedKernel.Messaging.IntegrationEvents;
using ViajantesTurismo.Catalog.Application.Gallery;
using ViajantesTurismo.Catalog.Contracts.IntegrationEvents.Tours;

namespace ViajantesTurismo.Catalog.Infrastructure.Tours;

/// <summary>Writes placement journal and wake-up rows through the same Catalog context.</summary>
internal sealed class EfCatalogGalleryPlacementIntentStore(
    CatalogDbContext dbContext, IIntegrationEventOutbox outbox, TimeProvider timeProvider) : ICatalogGalleryPlacementIntentStore
{
    /// <inheritdoc />
    public async ValueTask<long> Enqueue(
        Guid catalogTourId, IReadOnlyList<Guid> photoIds, Guid? coverId, bool canPublish, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(photoIds);

        var sequence = await Stage(catalogTourId, photoIds, coverId, canPublish, appendsUploadedPhoto: false, ct).ConfigureAwait(false);
        await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
        return sequence;
    }

    /// <summary>Stages a photo and its wake-up in the open upload transaction under the tour mutation lease.</summary>
    internal async ValueTask StageUpload(Guid catalogTourId, Guid photoId, bool canPublish, CancellationToken ct)
    {
        _ = await Stage(catalogTourId, [photoId], coverId: null, canPublish, appendsUploadedPhoto: true, ct).ConfigureAwait(false);
    }

    private async ValueTask<long> Stage(Guid catalogTourId, IReadOnlyList<Guid> photoIds, Guid? coverId, bool canPublish,
        bool appendsUploadedPhoto, CancellationToken ct)
    {
        var previous = await GetHighWatermark(catalogTourId, ct).ConfigureAwait(false);
        var id = Guid.CreateVersion7();
        var sequence = checked(previous + 1);
        var acceptedAt = timeProvider.GetUtcNow();
        dbContext.Set<CatalogGalleryPlacementIntentEntity>().Add(new CatalogGalleryPlacementIntentEntity
        {
            Id = id,
            CatalogTourId = catalogTourId,
            Sequence = sequence,
            AcceptedAtUtc = acceptedAt,
            PhotoIds = [.. photoIds],
            CoverId = coverId,
            CanPublish = canPublish,
            AppendsUploadedPhoto = appendsUploadedPhoto
        });
        await outbox.Enqueue(new CatalogGalleryPlacementQueuedIntegrationEvent(id, acceptedAt, catalogTourId, sequence), ct)
            .ConfigureAwait(false);
        return sequence;
    }

    /// <inheritdoc />
    public async ValueTask<CatalogGalleryPlacementIntent?> GetIntent(Guid catalogTourId, long sequence, CancellationToken ct)
    {
        var row = await dbContext.Set<CatalogGalleryPlacementIntentEntity>().AsNoTracking()
            .SingleOrDefaultAsync(intent => intent.CatalogTourId == catalogTourId && intent.Sequence == sequence, ct)
            .ConfigureAwait(false);
        return row is null
            ? null
            : new CatalogGalleryPlacementIntent(
                row.Id, row.Sequence, Array.AsReadOnly(row.PhotoIds), row.CoverId, row.CanPublish, row.AppendsUploadedPhoto);
    }

    /// <inheritdoc />
    public async ValueTask<long> GetHighWatermark(Guid catalogTourId, CancellationToken ct) =>
        await dbContext.Set<CatalogGalleryPlacementIntentEntity>().Where(intent => intent.CatalogTourId == catalogTourId)
            .MaxAsync(intent => (long?)intent.Sequence, ct).ConfigureAwait(false) ?? 0;

    /// <inheritdoc />
    public async ValueTask ResumeBootstrap(Guid catalogTourId, long throughSequence, Guid bootstrapEventId, CancellationToken ct)
    {
        var transaction = await dbContext.Database.BeginTransactionAsync(ct).ConfigureAwait(false);
        await using var ownedTransaction = transaction.ConfigureAwait(false);
        var watermark = dbContext.Set<CatalogGalleryPlacementIntentEntity>()
            .Where(intent => intent.CatalogTourId == catalogTourId && intent.Sequence == throughSequence);
        var changed = await watermark.Where(intent => intent.BootstrapWakeId == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(intent => intent.BootstrapWakeId, bootstrapEventId), ct)
            .ConfigureAwait(false);
        if (changed == 0)
        {
            if (!await watermark.AnyAsync(intent => intent.BootstrapWakeId == bootstrapEventId, ct).ConfigureAwait(false))
            {
                throw new InvalidOperationException("The captured Catalog gallery watermark could not be resumed.");
            }

            return;
        }

        await outbox.Enqueue(new CatalogGalleryPlacementQueuedIntegrationEvent(
            bootstrapEventId, timeProvider.GetUtcNow(), catalogTourId, throughSequence), ct).ConfigureAwait(false);
        await dbContext.SaveChangesAsync(ct).ConfigureAwait(false);
        await transaction.CommitAsync(ct).ConfigureAwait(false);
    }
}
