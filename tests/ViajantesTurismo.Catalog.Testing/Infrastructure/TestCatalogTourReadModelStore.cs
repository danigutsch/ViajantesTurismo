using System.Collections.Concurrent;
using PandaBear.BuildingBlocks;
using ViajantesTurismo.Catalog.Application.Tours;
using ViajantesTurismo.Catalog.Contracts.Application;
using ViajantesTurismo.Catalog.Domain.Tours;
using PandaBear.InputNormalization;

namespace ViajantesTurismo.Catalog.Testing.Infrastructure;

public sealed class TestCatalogTourReadModelStore : ICatalogTourReadModelStore, ICatalogTourRetirementStore, ICatalogTourListReader
{
    private readonly ConcurrentDictionary<Guid, CatalogTourDraftReadModel> toursById = new();
    private readonly ConcurrentDictionary<Guid, Guid> retirements = new();

    /// <inheritdoc />
    public ValueTask Withdraw(CatalogTourRetired retirement, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(retirement);
        ct.ThrowIfCancellationRequested();
        retirements.TryAdd(retirement.AdminTourId, retirement.RetirementId);
        foreach (var tour in toursById.Values.Where(tour => tour.AdminTourId == retirement.AdminTourId))
        {
            toursById[tour.CatalogTourId] = tour with
            {
                IsPublished = false
            };
        }

        return ValueTask.CompletedTask;
    }

    /// <summary>Gets the number of distinct retired source tours.</summary>
    public int RetirementCount => retirements.Count;

    public bool FailNextPublicationProjection
    {
        get; set;
    }

    /// <summary>Returns a point-in-time copy of all stored tour drafts.</summary>
    internal IReadOnlyCollection<CatalogTourDraftReadModel> GetSnapshot() => [.. toursById.Values];

    /// <inheritdoc />
    public ValueTask<CatalogTourPageDto> ListPage(string? page, CatalogTourListSort sort, bool descending, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var snapshot = GetSnapshot();
        var window = PageWindow.Create(page, CatalogTourPageDto.ItemsPerPage, snapshot.Count);
        var ordered = (sort, descending) switch
        {
            (CatalogTourListSort.Identifier, false) => snapshot.OrderBy(tour => tour.Identifier, StringComparer.Ordinal),
            (CatalogTourListSort.Identifier, true) => snapshot.OrderByDescending(tour => tour.Identifier, StringComparer.Ordinal),
            (CatalogTourListSort.Slug, false) => snapshot.OrderBy(tour => tour.Slug, StringComparer.Ordinal),
            (CatalogTourListSort.Slug, true) => snapshot.OrderByDescending(tour => tour.Slug, StringComparer.Ordinal),
            (CatalogTourListSort.IsPublished, false) => snapshot.OrderBy(tour => tour.IsPublished),
            (CatalogTourListSort.IsPublished, true) => snapshot.OrderByDescending(tour => tour.IsPublished),
            (_, true) => snapshot.OrderByDescending(tour => tour.Title, StringComparer.Ordinal),
            _ => snapshot.OrderBy(tour => tour.Title, StringComparer.Ordinal),
        };
        var items = ordered.ThenBy(tour => tour.CatalogTourId)
            .Skip(window.Skip).Take(window.Take)
            .Select(tour => new CatalogTourListItemDto(
                tour.CatalogTourId, tour.Identifier, tour.Title, tour.Slug, tour.IsPublished, tour.UpdatedAt))
            .ToArray();
        return ValueTask.FromResult(new CatalogTourPageDto(items, window.Total));
    }

    /// <inheritdoc />
    public ValueTask<bool> AdvanceStreamVersion(Guid catalogTourId, long streamVersion, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (!toursById.TryGetValue(catalogTourId, out var current))
        {
            return ValueTask.FromResult(false);
        }

        toursById[catalogTourId] = current with
        {
            StreamVersion = Math.Max(current.StreamVersion, streamVersion)
        };
        return ValueTask.FromResult(true);
    }

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
        IReadOnlyList<CatalogTourDraftReadModel> tours = [.. toursById.Values
            .OrderBy(tour => tour.Title)
            .ThenBy(tour => tour.CatalogTourId)];

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

    /// <inheritdoc />
    public ValueTask<CatalogTourDraftReadModel?> UpdateGallery(
        CatalogTourGalleryChanged gallery, long streamVersion, long position, DateTimeOffset updatedAt, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(gallery);
        ct.ThrowIfCancellationRequested();

        while (toursById.TryGetValue(gallery.CatalogTourId, out var current))
        {
            if (position <= current.GalleryPosition)
            {
                return ValueTask.FromResult<CatalogTourDraftReadModel?>(current);
            }

            var updated = current with
            {
                GalleryPhotoIds = gallery.PhotoIds,
                GalleryCoverId = gallery.CoverId,
                GalleryPosition = position,
                Position = Math.Max(current.Position, position),
                StreamVersion = Math.Max(current.StreamVersion, streamVersion),
                UpdatedAt = current.UpdatedAt >= updatedAt ? current.UpdatedAt : updatedAt
            };
            if (toursById.TryUpdate(gallery.CatalogTourId, updated, current))
            {
                return ValueTask.FromResult<CatalogTourDraftReadModel?>(updated);
            }
        }

        return ValueTask.FromResult<CatalogTourDraftReadModel?>(null);
    }
}
