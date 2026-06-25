namespace ViajantesTurismo.Public.WebTests.Infrastructure;

internal sealed class FakePublicCatalogApiClient : IPublicCatalogApiClient
{
    private readonly List<CatalogTourDto> tours = [];

    public bool FailListRequests { get; set; }

    public bool FailDetailsRequests { get; set; }

    public void AddTour(CatalogTourDto tour)
    {
        ArgumentNullException.ThrowIfNull(tour);

        tours.Add(tour);
    }

    public Task<CatalogTourDto[]> GetPublishedTours(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        return FailListRequests
            ? throw new HttpRequestException("Catalog unavailable.")
            : Task.FromResult(tours.Where(tour => tour.IsPublished).ToArray());
    }

    public Task<CatalogTourDto?> GetPublishedTourBySlug(string slug, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        if (FailDetailsRequests)
        {
            throw new HttpRequestException("Catalog unavailable.");
        }

        var tour = tours.FirstOrDefault(tour =>
            tour.IsPublished && string.Equals(tour.Slug, slug, StringComparison.Ordinal));
        return Task.FromResult(tour);
    }
}
