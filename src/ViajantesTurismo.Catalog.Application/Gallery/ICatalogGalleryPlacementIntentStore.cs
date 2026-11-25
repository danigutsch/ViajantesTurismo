namespace ViajantesTurismo.Catalog.Application.Gallery;

/// <summary>Stores accepted gallery placement intents and their local wake-up messages.</summary>
public interface ICatalogGalleryPlacementIntentStore
{
    /// <summary>Commits a snapshot and its outbox message together while the caller holds the tour mutation lease.</summary>
    ValueTask<long> Enqueue(Guid catalogTourId, IReadOnlyList<Guid> photoIds, Guid? coverId, bool canPublish, CancellationToken ct);

    /// <summary>Loads the committed intent at a per-tour sequence.</summary>
    ValueTask<CatalogGalleryPlacementIntent?> GetIntent(Guid catalogTourId, long sequence, CancellationToken ct);

    /// <summary>Gets the latest accepted sequence, or zero for an empty tour journal.</summary>
    ValueTask<long> GetHighWatermark(Guid catalogTourId, CancellationToken ct);

    /// <summary>Records the bootstrap wake receipt and its outbox row together for the captured sequence.</summary>
    ValueTask ResumeBootstrap(Guid catalogTourId, long throughSequence, Guid bootstrapEventId, CancellationToken ct);
}
