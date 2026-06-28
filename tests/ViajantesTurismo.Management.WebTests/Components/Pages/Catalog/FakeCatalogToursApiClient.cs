using ViajantesTurismo.Management.Web;

namespace ViajantesTurismo.Management.WebTests.Components.Pages.Catalog;

internal sealed class FakeCatalogToursApiClient : ICatalogToursApiClient
{
    public CatalogTourDto[] Tours { get; set; } = [];

    public bool ThrowOnGetTours { get; set; }

    public Task<CatalogTourDto[]> GetTours(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        return ThrowOnGetTours
            ? throw new HttpRequestException("Catalog unavailable.")
            : Task.FromResult(Tours);
    }

    public Task<CatalogTourDto?> UpdatePresentation(Guid id, UpsertCatalogTourPresentationRequest request, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var tour = Tours.SingleOrDefault(tour => tour.Id == id);
        if (tour is null)
        {
            return Task.FromResult<CatalogTourDto?>(null);
        }

        var updated = tour with
        {
            Title = request.Title,
            Slug = request.Slug,
            IsPublished = request.IsPublished
        };

        Tours = Tours.Select(current => current.Id == id ? updated : current).ToArray();
        return Task.FromResult<CatalogTourDto?>(updated);
    }
}
