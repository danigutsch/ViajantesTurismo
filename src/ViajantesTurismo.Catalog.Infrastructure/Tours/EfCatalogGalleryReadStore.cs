using Microsoft.EntityFrameworkCore;
using PandaBear.BuildingBlocks;
using ViajantesTurismo.Catalog.Application.Gallery;
using ViajantesTurismo.Catalog.Contracts.Application;
using ViajantesTurismo.Catalog.Domain.Media;
using ViajantesTurismo.Catalog.Domain.PublicContent;
using ViajantesTurismo.Catalog.Domain.Tours;

namespace ViajantesTurismo.Catalog.Infrastructure.Tours;

/// <summary>Reads published tour albums and loads rendition metadata only for the requested page.</summary>
internal sealed class EfCatalogGalleryReadStore(CatalogDbContext dbContext) : ICatalogGalleryReadStore
{
    /// <inheritdoc />
    public async ValueTask<CatalogGalleryIndexDto?> ListAlbums(ContentCulture culture, string? page, CancellationToken ct)
    {
        var photos = EligiblePhotos(culture);
        var eligibleTours = EfCatalogTourReadModelStore.VisibleTours(dbContext)
            .Select(tour => new
            {
                tour.CatalogTourId,
                tour.Slug,
                tour.Title,
                tour.GalleryPhotoIds,
                tour.GalleryCoverId,
                PhotoCount = photos.Count(photo => photo.TourLinks.Any(link => link.CatalogTourId == tour.CatalogTourId)
                    && (tour.GalleryPhotoIds == null || tour.GalleryPhotoIds.Contains(photo.Id)))
            })
            .Where(tour => tour.PhotoCount > 0 && tour.PhotoCount <= CatalogGalleryProjection.MaxPhotos);

        var total = await eligibleTours.CountAsync(ct).ConfigureAwait(false);
        var window = PageWindow.Create(page, CatalogGalleryProjection.PageSize, total);
        if (window.IsOutOfRange)
        {
            return null;
        }

        var rows = await eligibleTours.OrderBy(tour => tour.Title).ThenBy(tour => tour.CatalogTourId)
            .Skip(window.Skip).Take(window.Take)
            .Select(tour => new
            {
                tour.CatalogTourId,
                tour.Slug,
                tour.Title,
                tour.PhotoCount,
                Cover = photos.Where(photo => photo.TourLinks.Any(link => link.CatalogTourId == tour.CatalogTourId)
                        && (tour.GalleryPhotoIds == null || tour.GalleryPhotoIds.Contains(photo.Id)))
                    .Select(photo => new
                    {
                        photo.Id,
                        IsSelected = tour.GalleryPhotoIds == null
                            ? photo.TourLinks.Any(link => link.CatalogTourId == tour.CatalogTourId && link.IsCover)
                            : photo.Id == tour.GalleryCoverId,
                        Order = tour.GalleryPhotoIds == null
                            ? photo.TourLinks.Where(link => link.CatalogTourId == tour.CatalogTourId).Min(link => link.DisplayOrder)
                            : Array.IndexOf(tour.GalleryPhotoIds, photo.Id)
                    })
                    .OrderByDescending(photo => photo.IsSelected)
                    .ThenBy(photo => photo.Order)
                    .ThenBy(photo => photo.Id)
                    .First()
            })
            .ToArrayAsync(ct).ConfigureAwait(false);

        var covers = await LoadPhotos([.. rows.Select(tour => tour.Cover.Id).Distinct()], ct).ConfigureAwait(false);
        var albums = new List<CatalogGallerySummaryDto>(rows.Length);
        foreach (var tour in rows)
        {
            if (covers.TryGetValue(tour.Cover.Id, out var image) && image.BelongsToTour(tour.CatalogTourId)
                && CatalogGalleryProjection.MapPhoto(image, culture, tour.Cover.Order, isCover: true) is { } cover)
            {
                albums.Add(new CatalogGallerySummaryDto(tour.Slug, tour.Title, CatalogGalleryProjection.TitleCulture.Value, tour.PhotoCount, cover));
            }
        }

        return new CatalogGalleryIndexDto(window.Page, window.PageSize, total, albums);
    }

    /// <inheritdoc />
    public async ValueTask<CatalogGalleryAlbumDto?> GetAlbum(string slug, ContentCulture culture, string? page, CancellationToken ct)
    {
        if (!CatalogTourSlug.TryNormalize(slug, out var normalizedSlug))
        {
            return null;
        }

        var tour = await EfCatalogTourReadModelStore.VisibleTours(dbContext)
            .Where(tour => tour.Slug == normalizedSlug)
            .Select(tour => new { tour.CatalogTourId, tour.Slug, tour.Title, tour.GalleryPhotoIds, tour.GalleryCoverId })
            .SingleOrDefaultAsync(ct).ConfigureAwait(false);
        if (tour is null)
        {
            return null;
        }

        var placements = EligiblePhotos(culture)
            .SelectMany(photo => photo.TourLinks.Where(link => link.CatalogTourId == tour.CatalogTourId),
                (photo, link) => new { photo.Id, link.DisplayOrder, link.IsCover });
        Guid[] orderedPhotoIds;
        Guid? coverId;
        if (tour.GalleryPhotoIds is { } authored)
        {
            var eligible = await placements.Where(photo => authored.Contains(photo.Id)).Select(photo => photo.Id)
                .ToArrayAsync(ct).ConfigureAwait(false);
            var eligibleIds = eligible.ToHashSet();
            orderedPhotoIds = [.. authored.Where(eligibleIds.Contains)];
            coverId = tour.GalleryCoverId;
        }
        else
        {
            var legacy = await placements.OrderBy(photo => photo.DisplayOrder).ThenBy(photo => photo.Id)
                .Take(CatalogGalleryProjection.MaxPhotos + 1)
                .ToArrayAsync(ct).ConfigureAwait(false);
            orderedPhotoIds = [.. legacy.Select(photo => photo.Id)];
            coverId = legacy.FirstOrDefault(photo => photo.IsCover)?.Id;
        }

        var published = CatalogGalleryProjection.PublishedAlbum(tour.CatalogTourId, tour.Title, tour.Slug,
            orderedPhotoIds, coverId, culture);
        if (published?.Page(page, CatalogGalleryProjection.PageSize) is not { Window.IsOutOfRange: false } albumPage)
        {
            return null;
        }

        var photos = await LoadPhotos(albumPage.PhotoIds, ct).ConfigureAwait(false);
        var output = new List<CatalogTourImageDto>(photos.Count);
        for (var index = 0; index < albumPage.PhotoIds.Count; index++)
        {
            var id = albumPage.PhotoIds[index];
            if (!photos.TryGetValue(id, out var image) || !image.BelongsToTour(tour.CatalogTourId))
            {
                continue;
            }

            var order = tour.GalleryPhotoIds is null ? image.GetDisplayOrderForTour(tour.CatalogTourId) : albumPage.Window.Skip + index;
            if (CatalogGalleryProjection.MapPhoto(image, culture, order, id == published.CoverId) is { } photo)
            {
                output.Add(photo);
            }
        }

        return new CatalogGalleryAlbumDto(published.Slug.Value, published.Title, published.Culture.Value,
            albumPage.Window.Page, albumPage.Window.PageSize, published.PhotoCount, output);
    }

    // Match the aggregate's public readiness and locale policy before counting and paging in the database.
    private IQueryable<PublicMediaImage> EligiblePhotos(ContentCulture culture)
    {
        var ready = dbContext.PublicMediaImages.AsNoTracking().Where(photo => photo.ProcessingStatus == MediaImageProcessingStatus.Ready
            && photo.ResponsiveVariants.Any() && !photo.RequiresHumanReview && (photo.IsDecorative || !string.IsNullOrWhiteSpace(photo.AltText)));
        return culture == PublicContentCultures.Supported.Default
            ? ready
            : ready.Where(photo => !photo.AccessibilityTexts.Any(text => text.Language == culture
                && (text.RequiresHumanReview || (!text.IsDecorative && string.IsNullOrWhiteSpace(text.AltText)))));
    }

    private async ValueTask<Dictionary<Guid, PublicMediaImage>> LoadPhotos(IReadOnlyList<Guid> ids, CancellationToken ct)
    {
        return await dbContext.PublicMediaImages.AsNoTracking()
            .Where(photo => ids.Contains(photo.Id))
            .Include(photo => photo.ResponsiveVariants)
            .Include(photo => photo.TourLinks)
            .Include(photo => photo.AccessibilityTexts)
            .AsSplitQuery()
            .ToDictionaryAsync(photo => photo.Id, ct).ConfigureAwait(false);
    }
}
