using SharedKernel.BuildingBlocks;
using ViajantesTurismo.Catalog.Application.Gallery;
using ViajantesTurismo.Catalog.Application.Tours;
using ViajantesTurismo.Catalog.Contracts.Application;

namespace ViajantesTurismo.Catalog.Testing.Infrastructure;

/// <summary>Serves gallery contract data from the endpoint test's seeded tour and media stores.</summary>
internal sealed class TestCatalogGalleryReadStore(TestCatalogTourReadModelStore tours, TestPublicMediaImageStore photos) : ICatalogGalleryReadStore
{
    /// <inheritdoc />
    public async ValueTask<CatalogGalleryIndexDto?> ListAlbums(ContentCulture culture, string? page, CancellationToken ct)
    {
        var albums = new List<CatalogGallerySummaryDto>();
        foreach (var tour in (await tours.ListTours(ct)).Where(tour => tour.IsPubliclyVisible))
        {
            var photosForTour = await ProjectPhotos(tour, culture, ct);
            if (photosForTour.OrderByDescending(photo => photo.IsCover).ThenBy(photo => photo.SortOrder).FirstOrDefault() is { } cover)
            {
                albums.Add(new CatalogGallerySummaryDto(tour.Slug, tour.Title, CatalogGalleryProjection.TitleCulture.Value, photosForTour.Length, cover));
            }
        }

        var window = PageWindow.Create(page, CatalogGalleryProjection.PageSize, albums.Count);
        return window.IsOutOfRange ? null : new CatalogGalleryIndexDto(window.Page, window.PageSize, window.Total,
            [.. albums.Skip(window.Skip).Take(window.Take)]);
    }

    /// <inheritdoc />
    public async ValueTask<CatalogGalleryAlbumDto?> GetAlbum(string slug, ContentCulture culture, string? page, CancellationToken ct)
    {
        if (await tours.GetPublishedTourBySlug(slug, ct) is not { } tour)
        {
            return null;
        }

        var photosForTour = await ProjectPhotos(tour, culture, ct);
        var window = PageWindow.Create(page, CatalogGalleryProjection.PageSize, photosForTour.Length);
        return window.IsOutOfRange || photosForTour.Length == 0
            ? null
            : new CatalogGalleryAlbumDto(tour.Slug, tour.Title, CatalogGalleryProjection.TitleCulture.Value,
                window.Page, window.PageSize, window.Total, [.. photosForTour.Skip(window.Skip).Take(window.Take)]);
    }

    /// <inheritdoc />
    public async ValueTask<(string? ObjectKey, bool HasAccessibilityBlocker, bool IsMissingAltText)> FindPublicRendition(
        Guid imageId, int width, string contentType, CancellationToken ct)
    {
        if (await photos.GetImage(imageId, ct) is not { } image)
        {
            return default;
        }

        if (!image.HasReviewedAccessibilityText)
        {
            return (null, true, !image.IsDecorative && string.IsNullOrWhiteSpace(image.AltText));
        }

        var visible = tours.GetSnapshot().Any(tour => tour.IsPubliclyVisible && image.BelongsToTour(tour.CatalogTourId)
            && (tour.GalleryPhotoIds is null || tour.GalleryPhotoIds.Contains(image.Id)));
        return (image.HasPublicVariants && visible ? image.FindResponsiveVariant(width, contentType)?.ObjectKey : null, false, false);
    }

    private async Task<CatalogTourImageDto[]> ProjectPhotos(CatalogTourDraftReadModel tour, ContentCulture culture, CancellationToken ct)
    {
        var candidates = (await photos.ListByTour(tour.CatalogTourId, ct))
            .Select(photo => CatalogGalleryProjection.MapPhoto(photo, tour.CatalogTourId, culture)).OfType<CatalogTourImageDto>();
        if (tour.GalleryPhotoIds is not { } authored)
        {
            return [.. candidates.OrderBy(photo => photo.SortOrder)];
        }

        var inventory = candidates.ToDictionary(photo => photo.Id);
        var selected = authored.Where(inventory.ContainsKey).ToArray();
        var cover = tour.GalleryCoverId is { } explicitCover && selected.Contains(explicitCover) ? explicitCover : selected.FirstOrDefault();
        return [.. selected.Select((id, order) => inventory[id] with { SortOrder = order, IsCover = id == cover })];
    }
}
