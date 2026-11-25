using SharedKernel.BuildingBlocks;
using ViajantesTurismo.Catalog.Application.Gallery;
using ViajantesTurismo.Catalog.Application.Media;
using ViajantesTurismo.Catalog.Application.Tours;
using ViajantesTurismo.Catalog.Contracts.Application;
using ViajantesTurismo.Catalog.Domain.Media;

namespace ViajantesTurismo.Catalog.Testing.Infrastructure;

/// <summary>Projects bounded tour responses from a test host's seeded stores.</summary>
public sealed class TestCatalogTourListReader(
    ICatalogTourReadModelStore tours,
    IPublicMediaImageStore media) : ICatalogTourListReader
{
    /// <inheritdoc />
    public async ValueTask<CatalogTourPageDto> ListPage(string? page, CatalogTourListSort sort, bool descending, CancellationToken ct)
    {
        var snapshot = await tours.ListTours(ct).ConfigureAwait(false);
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
        return new CatalogTourPageDto(items, window.Total);
    }

    /// <inheritdoc />
    public async ValueTask<PublishedTourPageDto?> ListPublishedPage(string? page, int pageSize, CancellationToken ct)
    {
        var published = (await tours.ListTours(ct).ConfigureAwait(false)).Where(tour => tour.IsPubliclyVisible).ToArray();
        var window = PageWindow.Create(page, pageSize, published.Length);
        if (window.IsOutOfRange)
        {
            return null;
        }

        var cards = new List<TourSummaryDto>();
        foreach (var tour in published.OrderBy(tour => tour.Title, StringComparer.Ordinal).ThenBy(tour => tour.CatalogTourId)
            .Skip(window.Skip).Take(window.Take))
        {
            var photos = await GetEligiblePhotos(tour, ct).ConfigureAwait(false);
            cards.Add(new TourSummaryDto
            {
                Title = tour.Title,
                Slug = tour.Slug,
                Summary = tour.Summary,
                UpdatedAt = tour.UpdatedAt,
                Images = photos.FirstOrDefault() is { } cover ? [MapPhoto(cover, 0, isCover: true)] : []
            });
        }

        return new PublishedTourPageDto(window.Page, window.PageSize, window.Total, cards);
    }

    /// <inheritdoc />
    public async ValueTask<PublishedTourSitemapEntryDto[]> ListPublishedSitemapEntries(int limit, CancellationToken ct)
    {
        return [.. (await tours.ListTours(ct).ConfigureAwait(false)).Where(tour => tour.IsPubliclyVisible)
            .OrderBy(tour => tour.Title, StringComparer.Ordinal).ThenBy(tour => tour.CatalogTourId)
            .Take(limit)
            .Select(tour => new PublishedTourSitemapEntryDto(tour.Slug, tour.UpdatedAt))];
    }

    /// <inheritdoc />
    public async ValueTask<(PageWindow Window, IReadOnlyList<CatalogTourImageDto> Images)?> ListPublishedPhotos(
        CatalogTourDraftReadModel tour, string? page, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(tour);
        var eligible = await GetEligiblePhotos(tour, ct).ConfigureAwait(false);
        var window = PageWindow.Create(page, PublishedTourDetailsPageDto.PhotosPerPage, Math.Max(0, eligible.Length - 1));
        if (window.IsOutOfRange)
        {
            return null;
        }

        if (eligible.Length == 0)
        {
            return (window, []);
        }

        var thumbnails = eligible.Skip(window.Skip + 1).Take(window.Take)
            .Select((image, index) => MapPhoto(image, window.Skip + index + 1, isCover: false));
        return (window, [MapPhoto(eligible[0], 0, isCover: true), .. thumbnails]);
    }

    private async Task<PublicMediaImage[]> GetEligiblePhotos(CatalogTourDraftReadModel tour, CancellationToken ct)
    {
        var tourId = tour.CatalogTourId;
        var eligible = (await media.ListByTour(tourId, ct).ConfigureAwait(false)).Where(image => image.HasPublicVariants);
        return tour.GalleryPhotoIds?.ToArray() is { } authored
            ? [.. eligible.Where(image => authored.Contains(image.Id))
                .OrderByDescending(image => image.Id == tour.GalleryCoverId)
                .ThenBy(image => Array.IndexOf(authored, image.Id))]
            : [.. eligible.OrderByDescending(image => image.IsCoverForTour(tourId))
                .ThenBy(image => image.GetDisplayOrderForTour(tourId))
                .ThenBy(image => image.Id)];
    }

    private static CatalogTourImageDto MapPhoto(PublicMediaImage image, int order, bool isCover) =>
        CatalogGalleryProjection.MapImage(image, order, isCover, image.AltText, image.IsDecorative, image.Caption);
}
