using ViajantesTurismo.Catalog.Contracts;

namespace ViajantesTurismo.Management.Web;

internal interface ICatalogToursApiClient
{
    Task<CatalogTourDto[]> GetTours(CancellationToken cancellationToken);
}
