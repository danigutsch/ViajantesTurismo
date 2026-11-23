using ViajantesTurismo.Catalog.Application.Gallery;

namespace ViajantesTurismo.Catalog.Testing.Infrastructure;

/// <summary>Guards API surfaces that still use legacy placement and have no intent writer route.</summary>
internal sealed class UnsupportedCatalogGalleryPlacementIntentStore : ICatalogGalleryPlacementIntentStore
{
    /// <inheritdoc />
    public ValueTask<long> Enqueue(
        Guid catalogTourId, IReadOnlyList<Guid> photoIds, Guid? coverId, bool canPublish, CancellationToken ct) =>
        throw new NotSupportedException("The API test host does not queue placement intents.");

    /// <inheritdoc />
    public ValueTask<CatalogGalleryPlacementIntent?> GetIntent(Guid catalogTourId, long sequence, CancellationToken ct) =>
        ValueTask.FromResult<CatalogGalleryPlacementIntent?>(null);

    /// <inheritdoc />
    public ValueTask<long> GetHighWatermark(Guid catalogTourId, CancellationToken ct) =>
        ValueTask.FromResult(0L);

    /// <inheritdoc />
    public ValueTask ResumeBootstrap(Guid catalogTourId, long throughSequence, Guid bootstrapEventId, CancellationToken ct) =>
        throw new NotSupportedException("The API test host does not bootstrap galleries.");
}
