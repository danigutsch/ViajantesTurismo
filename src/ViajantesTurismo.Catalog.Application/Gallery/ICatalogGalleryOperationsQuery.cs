using ViajantesTurismo.Catalog.Contracts.Application;

namespace ViajantesTurismo.Catalog.Application.Gallery;

/// <summary>Reads committed Gallery operations metadata.</summary>
public interface ICatalogGalleryOperationsQuery
{
    /// <summary>Reads a placement or returns null when the tour and sequence identify no retained intent.</summary>
    /// <param name="catalogTourId">The Catalog tour that owns the placement.</param>
    /// <param name="sequence">The positive per-tour placement sequence.</param>
    /// <param name="ct">Cancellation token for the read.</param>
    /// <returns>The retained placement metadata, if present.</returns>
    ValueTask<GetCatalogGalleryPlacementStatusDto?> Read(Guid catalogTourId, long sequence, CancellationToken ct);

    /// <summary>Reads the placement whose original queued wake has the exact event identifier.</summary>
    /// <param name="eventId">The event ID assigned to the original Gallery wake.</param>
    /// <param name="ct">Cancellation token for the read.</param>
    /// <returns>The retained placement metadata, if present.</returns>
    ValueTask<GetCatalogGalleryPlacementStatusDto?> ReadByOriginalWake(Guid eventId, CancellationToken ct);

    /// <summary>Summarizes unresolved placements with repeated wake attempts and retries due at the observation time.</summary>
    /// <param name="observedAt">The UTC snapshot time against which retry due times are compared.</param>
    /// <param name="ct">Cancellation token for the read.</param>
    /// <returns>The count of due placements and their oldest due retry time.</returns>
    ValueTask<GetCatalogGalleryRetryHealthDto> ReadRetryHealth(DateTimeOffset observedAt, CancellationToken ct);
}
