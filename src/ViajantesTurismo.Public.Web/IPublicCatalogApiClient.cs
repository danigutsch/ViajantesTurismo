using ViajantesTurismo.Catalog.Contracts;

namespace ViajantesTurismo.Public.Web;

internal interface IPublicCatalogApiClient
{
    Task<CatalogTourDto[]> GetPublishedTours(CancellationToken cancellationToken);

    Task<CatalogTourDto?> GetPublishedTourBySlug(string slug, CancellationToken cancellationToken);
}
