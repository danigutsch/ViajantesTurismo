using System.Collections.Concurrent;

namespace ViajantesTurismo.Public.WebTests.Infrastructure;

internal sealed class FakePublicCatalogApiClient : IPublicCatalogApiClient
{
    private readonly List<CatalogTourDto> tours = [];
    private readonly ConcurrentDictionary<string, PublicContentVariantDto> contentByCulture = new(StringComparer.OrdinalIgnoreCase);

    public bool FailListRequests { get; set; }

    public bool FailDetailsRequests { get; set; }

    public bool FailContentRequests { get; set; }

    public void AddTour(CatalogTourDto tour)
    {
        ArgumentNullException.ThrowIfNull(tour);

        tours.Add(tour);
    }

    public void AddContent(string culture, PublicContentVariantDto content)
    {
        ArgumentNullException.ThrowIfNull(content);

        contentByCulture[culture] = content;
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

    public Task<PublicContentVariantDto?> GetPublicContent(string key, string? culture, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        if (FailContentRequests)
        {
            throw new HttpRequestException("Catalog unavailable.");
        }

        var requestedCulture = string.IsNullOrWhiteSpace(culture) ? "en-US" : culture;
        contentByCulture.TryGetValue(requestedCulture, out var content);
        return Task.FromResult(content);
    }
}
