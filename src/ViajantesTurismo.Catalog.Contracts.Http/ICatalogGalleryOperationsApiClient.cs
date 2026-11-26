using SharedKernel.HttpClients;
using ViajantesTurismo.Catalog.Contracts.Application;

namespace ViajantesTurismo.Catalog.Contracts.Http;

/// <summary>Reads retained Gallery operations metadata from the Catalog API.</summary>
public interface ICatalogGalleryOperationsApiClient
{
    /// <summary>Looks up one placement by Catalog tour and sequence.</summary>
    /// <param name="catalogTourId">The Catalog tour that owns the placement.</param>
    /// <param name="sequence">The positive placement sequence.</param>
    /// <param name="ct">Cancellation token for the request.</param>
    /// <returns>The HTTP outcome of the lookup. A successful value identifies the requested placement.</returns>
    Task<HttpOutcome<GetCatalogGalleryPlacementStatusDto>> Read(Guid catalogTourId, long sequence, CancellationToken ct);

    /// <summary>Looks up one placement by the event ID of its original queued wake.</summary>
    /// <param name="eventId">The event ID assigned to the original Gallery wake.</param>
    /// <param name="ct">Cancellation token for the request.</param>
    /// <returns>The HTTP outcome of the lookup. A successful value identifies the placement belonging to that wake.</returns>
    Task<HttpOutcome<GetCatalogGalleryPlacementStatusDto>> ReadByOriginalWake(Guid eventId, CancellationToken ct);

    /// <summary>Reads the count and oldest due retry for unresolved Gallery placements.</summary>
    /// <param name="ct">Cancellation token for the request.</param>
    /// <returns>The HTTP outcome of the retry summary read.</returns>
    Task<HttpOutcome<GetCatalogGalleryRetryHealthDto>> GetRetryHealth(CancellationToken ct);
}
