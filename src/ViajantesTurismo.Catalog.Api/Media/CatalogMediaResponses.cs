using ViajantesTurismo.Catalog.Application.Media;
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

    /// <summary>Filters images eligible for public responses.</summary>
    internal static IReadOnlyList<PublicMediaImage> GetReadyImages(IReadOnlyList<PublicMediaImage> images)
    {
        return [.. images.Where(CanDeliverPublicImage)];
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

    private static CatalogTourImageDto MapImage(PublicMediaImage image)
    {
        return new CatalogTourImageDto
        {
            Id = image.Id,
            SortOrder = image.DisplayOrder,
            IsCover = image.IsCover,
            AltText = image.AltText,
            IsDecorative = image.IsDecorative,
            Caption = image.Caption,
            ResponsiveVariants = [.. image.ResponsiveVariants
                .OrderBy(variant => variant.Width)
                .Select(MapPublicResponsiveVariant)]
        };
    }

    private static CatalogMediaImageVariantDto MapPublicResponsiveVariant(MediaImageResponsiveVariant variant)
    {
        return new CatalogMediaImageVariantDto
        {
            Width = variant.Width,
            Height = variant.Height,
            ContentType = variant.ContentType,
            FileSizeBytes = variant.FileSizeBytes
        };
    }
}
