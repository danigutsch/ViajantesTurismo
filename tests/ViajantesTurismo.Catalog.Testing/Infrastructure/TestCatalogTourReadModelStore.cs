using System.Collections.Concurrent;
using ViajantesTurismo.Catalog.Application.Tours;
using ViajantesTurismo.Catalog.Domain.Tours;
using SharedKernel.InputNormalization;

namespace ViajantesTurismo.Catalog.Testing.Infrastructure;

public sealed class TestCatalogTourReadModelStore : ICatalogTourReadModelStore
{
    private readonly ConcurrentDictionary<Guid, CatalogTourDraftReadModel> toursById = new();

    public bool FailNextPublicationProjection { get; set; }

    internal IReadOnlyCollection<CatalogTourDraftReadModel> GetSnapshot() => toursById.Values.ToArray();

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
                Position = Math.Max(current.Position, tour.Position),
                StreamVersion = Math.Max(current.StreamVersion, tour.StreamVersion),
                UpdatedAt = current.UpdatedAt >= tour.UpdatedAt ? current.UpdatedAt : tour.UpdatedAt
            });

        return ValueTask.CompletedTask;
    }

    public ValueTask<CatalogTourDraftReadModel?> UpdatePresentation(
        Guid catalogTourId,
        CatalogTourPresentationUpdate update,
        long streamVersion,
        long position,
        DateTimeOffset updatedAt,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(update);
        ct.ThrowIfCancellationRequested();

        if (!toursById.TryGetValue(catalogTourId, out var current))
        {
            return ValueTask.FromResult<CatalogTourDraftReadModel?>(null);
        }

        if (position < current.Position)
        {
            return ValueTask.FromResult<CatalogTourDraftReadModel?>(current);
        }

        var updated = current with
        {
            Title = StringSanitizer.Sanitize(update.Title) ?? string.Empty,
            Slug = StringSanitizer.Sanitize(update.Slug) ?? string.Empty,
            Summary = StringSanitizer.Sanitize(update.Summary) ?? string.Empty,
            Description = StringSanitizer.Sanitize(update.Description) ?? string.Empty,
            Itinerary = StringSanitizer.Sanitize(update.Itinerary) ?? string.Empty,
            SeoTitle = StringSanitizer.Sanitize(update.SeoTitle) ?? string.Empty,
            SeoDescription = StringSanitizer.Sanitize(update.SeoDescription) ?? string.Empty,
            IsPublished = current.StreamVersion != 1 && current.IsPublished,
            StreamVersion = streamVersion,
            Position = position,
            UpdatedAt = updatedAt
        };
        toursById[catalogTourId] = updated;

        return ValueTask.FromResult<CatalogTourDraftReadModel?>(updated);
    }

    public ValueTask<CatalogTourDraftReadModel?> SetPublicationStatus(
        Guid catalogTourId,
        bool isPublished,
        long streamVersion,
        long position,
        DateTimeOffset updatedAt,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        if (FailNextPublicationProjection)
        {
            FailNextPublicationProjection = false;
            throw new InvalidOperationException("Simulated publication projection failure.");
        }

        if (!toursById.TryGetValue(catalogTourId, out var current))
        {
            return ValueTask.FromResult<CatalogTourDraftReadModel?>(null);
        }

        if (position < current.Position)
        {
            return ValueTask.FromResult<CatalogTourDraftReadModel?>(current);
        }

        var updated = current with
        {
            IsPublished = isPublished,
            StreamVersion = streamVersion,
            Position = position,
            UpdatedAt = updatedAt
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
        if (!CatalogTourSlug.TryNormalize(slug, out var normalizedSlug))
        {
            return ValueTask.FromResult<CatalogTourDraftReadModel?>(null);
        }

        var tour = toursById.Values.SingleOrDefault(tour => tour.IsPublished && tour.Slug == normalizedSlug);

        return ValueTask.FromResult(tour);
    }
}
