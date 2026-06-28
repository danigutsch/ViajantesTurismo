using ViajantesTurismo.Catalog.Contracts;

namespace ViajantesTurismo.Management.Web;

internal interface ICatalogToursApiClient
{
    Task<CatalogTourDto[]> GetTours(CancellationToken ct);

    Task<CatalogTourDto?> UpdatePresentation(Guid id, UpsertCatalogTourPresentationRequest request, CancellationToken ct);
}
