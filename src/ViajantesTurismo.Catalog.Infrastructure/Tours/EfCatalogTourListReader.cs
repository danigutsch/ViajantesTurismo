using Microsoft.EntityFrameworkCore;
using PandaBear.BuildingBlocks;
using ViajantesTurismo.Catalog.Application.Gallery;
using ViajantesTurismo.Catalog.Application.Tours;
using ViajantesTurismo.Catalog.Contracts.Application;
using ViajantesTurismo.Catalog.Infrastructure.Media;

namespace ViajantesTurismo.Catalog.Infrastructure.Tours;

/// <summary>Reads summary pages from the Catalog tour projection.</summary>
internal sealed class EfCatalogTourListReader(CatalogDbContext dbContext) : ICatalogTourListReader
{
    /// <inheritdoc />
    public async ValueTask<CatalogTourPageDto> ListPage(
        string? page, CatalogTourListSort sort, bool descending, CancellationToken ct)
    {
        var tours = dbContext.CatalogTourReadModels.AsNoTracking();
        var total = await tours.CountAsync(ct).ConfigureAwait(false);
        var window = PageWindow.Create(page, CatalogTourPageDto.ItemsPerPage, total);
        var orderedTours = (sort, descending) switch
        {
            (CatalogTourListSort.Identifier, false) => tours.OrderBy(tour => tour.Identifier),
            (CatalogTourListSort.Identifier, true) => tours.OrderByDescending(tour => tour.Identifier),
            (CatalogTourListSort.Slug, false) => tours.OrderBy(tour => tour.Slug),
            (CatalogTourListSort.Slug, true) => tours.OrderByDescending(tour => tour.Slug),
            (CatalogTourListSort.IsPublished, false) => tours.OrderBy(tour => tour.IsPublished),
            (CatalogTourListSort.IsPublished, true) => tours.OrderByDescending(tour => tour.IsPublished),
            (_, true) => tours.OrderByDescending(tour => tour.Title),
            _ => tours.OrderBy(tour => tour.Title),
        };
        var items = await orderedTours
            .ThenBy(tour => tour.CatalogTourId)
            .Skip(window.Skip)
            .Take(window.Take)
            .Select(tour => new CatalogTourListItemDto(
                tour.CatalogTourId, tour.Identifier, tour.Title, tour.Slug, tour.IsPublished, tour.UpdatedAt))
            .ToArrayAsync(ct)
            .ConfigureAwait(false);

        return new CatalogTourPageDto(items, total);
    }

    /// <inheritdoc />
    public async ValueTask<PublishedTourPageDto?> ListPublishedPage(string? page, int pageSize, CancellationToken ct)
    {
        var tours = EfCatalogTourReadModelStore.VisibleTours(dbContext);
        var total = await tours.CountAsync(ct).ConfigureAwait(false);
        var window = PageWindow.Create(page, pageSize, total);
        if (window.IsOutOfRange)
        {
            return null;
        }

        var ready = CatalogPublicMediaQueries.Ready(dbContext);
        var rows = await tours.OrderBy(tour => tour.Title).ThenBy(tour => tour.CatalogTourId)
            .Skip(window.Skip).Take(window.Take)
            .Select(tour => new
            {
                tour.CatalogTourId,
                tour.Title,
                tour.Slug,
                tour.Summary,
                tour.UpdatedAt,
                CoverId = ready.Where(photo => photo.TourLinks.Any(link => link.CatalogTourId == tour.CatalogTourId)
                        && (tour.GalleryPhotoIds == null || tour.GalleryPhotoIds.Contains(photo.Id)))
                    .OrderByDescending(photo => tour.GalleryPhotoIds == null
                        ? photo.TourLinks.Any(link => link.CatalogTourId == tour.CatalogTourId && link.IsCover)
                        : photo.Id == tour.GalleryCoverId)
                    .ThenBy(photo => tour.GalleryPhotoIds == null
                        ? photo.TourLinks.Where(link => link.CatalogTourId == tour.CatalogTourId).Min(link => link.DisplayOrder)
                        : Array.IndexOf(tour.GalleryPhotoIds, photo.Id))
                    .ThenBy(photo => photo.Id)
                    .Select(photo => (Guid?)photo.Id)
                    .FirstOrDefault()
            })
            .ToArrayAsync(ct).ConfigureAwait(false);

        var coverIds = rows.Select(row => row.CoverId).OfType<Guid>().Distinct().ToArray();
        var tourIds = rows.Select(row => row.CatalogTourId).ToArray();
        var covers = await ready.Where(image => coverIds.Contains(image.Id))
            .Include(image => image.ResponsiveVariants)
            .Include(image => image.TourLinks.Where(link => tourIds.Contains(link.CatalogTourId)))
            .AsSplitQuery()
            .ToDictionaryAsync(image => image.Id, ct).ConfigureAwait(false);

        var cards = rows.Select(row => new TourSummaryDto
        {
            Title = row.Title,
            Slug = row.Slug,
            Summary = row.Summary,
            UpdatedAt = row.UpdatedAt,
            Images = row.CoverId is { } id && covers.TryGetValue(id, out var cover) && cover.BelongsToTour(row.CatalogTourId)
                ? [CatalogGalleryProjection.MapImage(cover, 0, isCover: true, cover.AltText, cover.IsDecorative, cover.Caption)]
                : []
        }).ToArray();
        return new PublishedTourPageDto(window.Page, window.PageSize, total, cards);
    }

    /// <inheritdoc />
    public async ValueTask<(PageWindow Window, IReadOnlyList<CatalogTourImageDto> Images)?> ListPublishedPhotos(
        CatalogTourDraftReadModel tour, string? page, CancellationToken ct)
    {
        var tourId = tour.CatalogTourId;
        var authored = tour.GalleryPhotoIds?.ToArray();
        var photos = CatalogPublicMediaQueries.Ready(dbContext)
            .Where(photo => photo.TourLinks.Any(link => link.CatalogTourId == tourId)
                && (authored == null || authored.Contains(photo.Id)));
        var count = await photos.CountAsync(ct).ConfigureAwait(false);
        var window = PageWindow.Create(page, PublishedTourDetailsPageDto.PhotosPerPage, Math.Max(0, count - 1));
        if (window.IsOutOfRange)
        {
            return null;
        }

        if (count == 0)
        {
            return (window, []);
        }

        var ordered = authored is null
            ? photos.OrderByDescending(photo => photo.TourLinks.Any(link => link.CatalogTourId == tourId && link.IsCover))
                .ThenBy(photo => photo.TourLinks.Where(link => link.CatalogTourId == tourId).Min(link => link.DisplayOrder))
            : photos.OrderByDescending(photo => photo.Id == tour.GalleryCoverId)
                .ThenBy(photo => Array.IndexOf(authored, photo.Id));
        var identifiers = ordered.ThenBy(photo => photo.Id).Select(photo => photo.Id);
        var coverId = await identifiers.Select(id => (Guid?)id).FirstOrDefaultAsync(ct).ConfigureAwait(false);
        if (coverId is not { } cover)
        {
            return (window, []);
        }

        var pageIds = await identifiers.Skip(window.Skip + 1).Take(window.Take).ToArrayAsync(ct).ConfigureAwait(false);
        Guid[] ids = [cover, .. pageIds.Where(id => id != cover)];
        var images = await photos.Where(photo => ids.Contains(photo.Id)).Include(photo => photo.ResponsiveVariants)
            .ToDictionaryAsync(photo => photo.Id, ct).ConfigureAwait(false);
        var items = ids.Where(images.ContainsKey).Select(id =>
        {
            var image = images[id];
            return CatalogGalleryProjection.MapImage(image, id == cover ? 0 : window.Skip + Array.IndexOf(pageIds, id) + 1,
                id == cover, image.AltText, image.IsDecorative, image.Caption);
        }).ToArray();
        return (window, items);
    }
}
