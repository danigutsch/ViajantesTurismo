using System.Collections.Concurrent;
using ViajantesTurismo.Catalog.Application.Tours;

namespace ViajantesTurismo.Catalog.ApiServiceTests.Infrastructure;

internal sealed class TestCatalogTourReadModelStore : ICatalogTourReadModelStore
{
    private readonly ConcurrentDictionary<Guid, CatalogTourDraftReadModel> toursById = new();

    public ValueTask UpsertDraft(CatalogTourDraftReadModel tour, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(tour);
        ct.ThrowIfCancellationRequested();

        toursById[tour.CatalogTourId] = tour;

        return ValueTask.CompletedTask;
    }

    public ValueTask<CatalogTourDraftReadModel?> UpdatePresentation(Guid catalogTourId, CatalogTourPresentationUpdate update, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(update);
        ct.ThrowIfCancellationRequested();

        if (!toursById.TryGetValue(catalogTourId, out var current))
        {
            return ValueTask.FromResult<CatalogTourDraftReadModel?>(null);
        }

        var updated = current with
        {
            Title = update.Title,
            Slug = update.Slug,
            IsPublished = update.IsPublished,
            UpdatedAt = current.UpdatedAt
        };
        toursById[catalogTourId] = updated;

        return ValueTask.FromResult<CatalogTourDraftReadModel?>(updated);
    }

    public ValueTask<CatalogTourDraftReadModel?> GetTour(Guid catalogTourId, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        toursById.TryGetValue(catalogTourId, out var tour);

        return ValueTask.FromResult(tour);
    }

    public ValueTask<IReadOnlyList<CatalogTourDraftReadModel>> ListTours(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        IReadOnlyList<CatalogTourDraftReadModel> tours = toursById.Values.OrderBy(tour => tour.Identifier).ToArray();

        return ValueTask.FromResult(tours);
    }

    public ValueTask<CatalogTourDraftReadModel?> GetPublishedTourBySlug(string slug, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var tour = toursById.Values.SingleOrDefault(tour => tour.IsPublished && string.Equals(tour.Slug, slug, StringComparison.OrdinalIgnoreCase));

        return ValueTask.FromResult(tour);
    }
}
