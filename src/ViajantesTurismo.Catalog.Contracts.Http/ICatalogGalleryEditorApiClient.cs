using SharedKernel.HttpClients;
using ViajantesTurismo.Catalog.Contracts.Application;

namespace ViajantesTurismo.Catalog.Contracts.Http;

/// <summary>Accesses the Catalog tour gallery editor and its bounded photo choices.</summary>
public interface ICatalogGalleryEditorApiClient
{
    /// <summary>Reads the current editor state and, when supplied, the outcome of a specific placement.</summary>
    Task<HttpOutcome<CatalogGalleryEditorStateDto>> GetState(Guid tourId, long? placementSequence, CancellationToken ct);

    /// <summary>Searches or hydrates a bounded page of the tour's linked photos.</summary>
    Task<HttpOutcome<CatalogGalleryPhotoOptionsDto>> SearchPhotos(Guid tourId, CatalogGalleryPhotoOptionsRequest request, CancellationToken ct);

    /// <summary>Initializes the authored album from the tour's current legacy placements.</summary>
    Task<HttpOutcome> Initialize(Guid tourId, CancellationToken ct);

    /// <summary>Queues a complete authored gallery order and cover based on the observed revision pair.</summary>
    Task<HttpOutcome> QueuePlacement(Guid tourId, QueueCatalogGalleryPlacementRequest request, CancellationToken ct);
}
