using SharedKernel.BuildingBlocks;
using ViajantesTurismo.Catalog.Contracts.Application;

namespace ViajantesTurismo.Catalog.Application.Gallery;

/// <summary>Reads public albums and renditions from Catalog tour visibility and photo placement.</summary>
public interface ICatalogGalleryReadStore
{
    /// <summary>Lists the requested album page, or returns null if the page is out of range.</summary>
    ValueTask<CatalogGalleryIndexDto?> ListAlbums(ContentCulture culture, string? page, CancellationToken ct);

    /// <summary>Gets one page of a visible and eligible album, or returns null if that page is unavailable.</summary>
    ValueTask<CatalogGalleryAlbumDto?> GetAlbum(string slug, ContentCulture culture, string? page, CancellationToken ct);

    /// <summary>Finds the object key of a deliverable rendition and reports any accessibility blocker.</summary>
    ValueTask<(string? ObjectKey, bool HasAccessibilityBlocker, bool IsMissingAltText)> FindPublicRendition(
        Guid imageId, int width, string contentType, CancellationToken ct);
}
