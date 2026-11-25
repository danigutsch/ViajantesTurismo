using ViajantesTurismo.Catalog.Application.Gallery;
using ViajantesTurismo.Catalog.Contracts.Application;
using ViajantesTurismo.Catalog.Domain.Media;

namespace ViajantesTurismo.Catalog.Api.Media;

/// <summary>Maps gallery images for tour management responses.</summary>
internal static class CatalogMediaResponses
{
    /// <summary>Maps ready images in gallery order.</summary>
    internal static CatalogTourImageDto[] MapImages(IReadOnlyList<PublicMediaImage> images)
    {
        return [.. PublicMediaImage
            .OrderForGallery([.. images.Where(image => image.HasPublicVariants)])
            .Select(MapImage)];
    }

    private static CatalogTourImageDto MapImage(PublicMediaImage image) =>
        CatalogGalleryProjection.MapImage(image, image.DisplayOrder, image.IsCover, image.AltText, image.IsDecorative, image.Caption);
}
