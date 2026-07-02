namespace ViajantesTurismo.Catalog.Contracts;

/// <summary>
/// HTTP client contract for catalog tour management endpoints.
/// </summary>
public interface ICatalogToursApiClient
{
    /// <summary>
    /// Gets catalog tours.
    /// </summary>
    Task<CatalogTourDto[]> GetTours(CancellationToken ct);

    /// <summary>
    /// Gets a catalog tour by identifier.
    /// </summary>
    Task<CatalogTourDto?> GetTour(Guid id, CancellationToken ct);

    /// <summary>
    /// Updates catalog tour presentation data.
    /// </summary>
    Task<CatalogTourDto?> UpdatePresentation(Guid id, UpsertCatalogTourPresentationRequest request, CancellationToken ct);
}
