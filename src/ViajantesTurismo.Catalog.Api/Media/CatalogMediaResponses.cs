using ViajantesTurismo.Catalog.Application.Gallery;
using ViajantesTurismo.Catalog.Application.Media;
using ViajantesTurismo.Catalog.Application.Tours;
using ViajantesTurismo.Catalog.Contracts.Application;
using ViajantesTurismo.Catalog.Domain.Media;

namespace ViajantesTurismo.Catalog.Api.Media;

/// <summary>Shares public image projections between tours and media delivery.</summary>
internal static class CatalogMediaResponses
{
    /// <summary>Maps ready images in gallery order for tourism responses.</summary>
    internal static CatalogTourImageDto[] MapImages(IReadOnlyList<PublicMediaImage> images)
    {
        return [.. PublicMediaImage
            .OrderForGallery([.. images.Where(image => image.HasPublicVariants)])
            .Select(MapImage)];
    }

    /// <summary>Maps the eligible photos in the current authored tour order, with legacy placement before initialization.</summary>
    internal static CatalogTourImageDto[] MapPublicImages(CatalogTourDraftReadModel tour, IReadOnlyList<PublicMediaImage> images)
    {
        if (tour.GalleryPhotoIds is not { } authored)
        {
            return MapImages([.. images.Where(CanDeliverPublicImage)]);
        }

        var inventory = images.ToDictionary(image => image.Id);
        var published = authored.Where(id => inventory.TryGetValue(id, out var image) && CanDeliverPublicImage(image)).ToArray();
        var cover = tour.GalleryCoverId is { } chosen && published.Contains(chosen) ? chosen : published.FirstOrDefault();
        return [.. published.Select((id, order) => MapImage(inventory[id], order, id == cover))];
    }

    /// <summary>Checks public rendition eligibility and records accessibility publication blockers.</summary>
    internal static bool CanDeliverPublicImage(PublicMediaImage image)
    {
        if (!image.HasReviewedAccessibilityText)
        {
            MediaAccessibilityTelemetry.RecordPublicationBlocker(!image.IsDecorative && string.IsNullOrWhiteSpace(image.AltText));
        }

        return image.HasPublicVariants;
    }

    private static CatalogTourImageDto MapImage(PublicMediaImage image) =>
        MapImage(image, image.DisplayOrder, image.IsCover);

    private static CatalogTourImageDto MapImage(PublicMediaImage image, int order, bool isCover) =>
        CatalogGalleryProjection.MapImage(image, order, isCover, image.AltText, image.IsDecorative, image.Caption);
}
