namespace ViajantesTurismo.Catalog.Contracts.Application;

/// <summary>Requests one bounded page of management photo choices for a Catalog tour.</summary>
/// <param name="Query">Optional search text or a full photo ID.</param>
/// <param name="Limit">The maximum number of choices to return.</param>
/// <param name="IncludedPhotoIds">An optional restriction to a chosen set of photos.</param>
public sealed record CatalogGalleryPhotoOptionsRequest(
    string? Query,
    int Limit = CatalogGalleryPhotoOptionsRequest.MaxPageSize,
    IReadOnlyList<Guid>? IncludedPhotoIds = null)
{
    /// <summary>The maximum number of choices or thumbnails on a picker page.</summary>
    public const int MaxPageSize = 24;

    /// <summary>The maximum length of a search term.</summary>
    public const int MaxQueryLength = 200;
}
