using ViajantesTurismo.Public.Web;

namespace ViajantesTurismo.Public.WebTests.Infrastructure;

internal sealed class FakePublicCatalogApiClient : IPublicCatalogApiClient
{
    private readonly List<CatalogTourDto> tours = [];

    public void AddTour(CatalogTourDto tour)
    {
        ArgumentNullException.ThrowIfNull(tour);

        tours.Add(tour);
    }

    public Task<CatalogTourDto[]> GetPublishedTours(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(tours.ToArray());
    }

    public Task<CatalogTourDto?> GetPublishedTourBySlug(string slug, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var tour = tours.FirstOrDefault(tour => string.Equals(tour.Slug, slug, StringComparison.Ordinal));
        return Task.FromResult(tour);
    }
}
