using ViajantesTurismo.Catalog.Contracts.Application;

namespace ViajantesTurismo.Catalog.Application.Gallery;

/// <summary>Reads bounded management choices from the Catalog media linked to one tour.</summary>
public interface ICatalogGalleryPhotoOptionsQuery
{
    /// <summary>Counts matches and retrieves one bounded page for the given tour.</summary>
    /// <param name="catalogTourId">The Catalog tour identifier.</param>
    /// <param name="request">The requested filter and page size.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The matching choices, or null if the tour does not exist.</returns>
    Task<CatalogGalleryPhotoOptionsDto?> Search(Guid catalogTourId, CatalogGalleryPhotoOptionsRequest request, CancellationToken ct);
}
