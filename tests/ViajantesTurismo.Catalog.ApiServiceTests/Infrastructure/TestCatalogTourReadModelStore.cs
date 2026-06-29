using System.Collections.Concurrent;
using ViajantesTurismo.Catalog.Application.Tours;
using ViajantesTurismo.Common.Sanitizers;

namespace ViajantesTurismo.Catalog.ApiServiceTests.Infrastructure;

internal sealed class TestCatalogTourReadModelStore : ICatalogTourReadModelStore
{
    private readonly ConcurrentDictionary<Guid, CatalogTourDraftReadModel> toursById = new();

    public ValueTask UpsertDraft(CatalogTourDraftReadModel tour, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(tour);
        ct.ThrowIfCancellationRequested();

        toursById.AddOrUpdate(
            tour.CatalogTourId,
            tour,
            (_, current) => current with
            {
                AdminTourId = tour.AdminTourId,
                Identifier = tour.Identifier,
                Position = tour.Position,
                UpdatedAt = tour.UpdatedAt
            });

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
            Title = StringSanitizer.Sanitize(update.Title) ?? string.Empty,
            Slug = StringSanitizer.Sanitize(update.Slug) ?? string.Empty,
            IsPublished = update.IsPublished,
            UpdatedAt = DateTimeOffset.UtcNow
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
        IReadOnlyList<CatalogTourDraftReadModel> tours = toursById.Values
            .OrderBy(tour => tour.Title)
            .ThenBy(tour => tour.CatalogTourId)
            .ToArray();

        return ValueTask.FromResult(tours);
    }

    public ValueTask<CatalogTourDraftReadModel?> GetPublishedTourBySlug(string slug, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(slug);
        ct.ThrowIfCancellationRequested();
        var sanitizedSlug = StringSanitizer.Sanitize(slug) ?? string.Empty;
        var tour = toursById.Values.SingleOrDefault(tour => tour.IsPublished && tour.Slug == sanitizedSlug);

        return ValueTask.FromResult(tour);
    }
}
