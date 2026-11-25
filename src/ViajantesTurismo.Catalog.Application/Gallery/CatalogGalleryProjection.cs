using SharedKernel.BuildingBlocks;
using SharedKernel.Galleries;
using ViajantesTurismo.Catalog.Domain;
using ViajantesTurismo.Catalog.Contracts.Application;
using ViajantesTurismo.Catalog.Domain.Media;
using ViajantesTurismo.Catalog.Domain.PublicContent;

namespace ViajantesTurismo.Catalog.Application.Gallery;

/// <summary>Projects the current Catalog tour gallery rows into the shared public album model.</summary>
public static class CatalogGalleryProjection
{
    /// <summary>Gets the number of album cards or photos served per public page.</summary>
    public const int PageSize = 24;

    /// <summary>Gets the maximum public photo count for one tour album.</summary>
    public const int MaxPhotos = CatalogDomainLimits.MaxGalleryPhotos;

    /// <summary>Gets the content language of the current single-language Catalog tour titles.</summary>
    public static ContentCulture TitleCulture => PublicContentCultures.Supported.Default;

    /// <summary>Builds a public album view from already reviewed photos ordered by their Catalog placement.</summary>
    public static AlbumView<Guid, Guid>? PublishedAlbum(
        Guid tourId,
        string title,
        string slug,
        IReadOnlyList<Guid> orderedPhotos,
        Guid? coverId,
        ContentCulture culture)
    {
        ArgumentNullException.ThrowIfNull(orderedPhotos);

        if (orderedPhotos.Count is 0 or > MaxPhotos)
        {
            return null;
        }

        var translation = AlbumTranslation.Restore(TitleCulture, title, Slug.Restore(slug), PublicationStatus.Published);
        var eligibleCover = coverId is { } chosen && orderedPhotos.Contains(chosen) ? coverId : null;
        return Album.Restore<Guid, Guid>(0, tourId, orderedPhotos, eligibleCover, [translation])
            .PublishedIn(culture, PublicContentCultures.Supported);
    }

    /// <summary>Maps the reviewed accessibility and processed renditions for one tour's photo.</summary>
    public static CatalogTourImageDto? MapPhoto(PublicMediaImage image, Guid tourId, ContentCulture culture)
    {
        ArgumentNullException.ThrowIfNull(image);
        return MapPhoto(image, culture, image.GetDisplayOrderForTour(tourId), image.IsCoverForTour(tourId));
    }

    /// <summary>Maps a photo with its effective public display order and cover flag.</summary>
    public static CatalogTourImageDto? MapPhoto(PublicMediaImage image, ContentCulture culture, int order, bool isCover)
    {
        ArgumentNullException.ThrowIfNull(image);

        if (image.GetGalleryAccessibility(culture) is not { } text)
        {
            return null;
        }

        return MapImage(image, order, isCover, text.AltText ?? string.Empty, text.IsDecorative, text.Caption) with
        {
            TextLanguage = text.Language != culture ? text.Language.Value : null
        };
    }

    /// <summary>Maps a Catalog image and its resolved public accessibility fields.</summary>
    public static CatalogTourImageDto MapImage(
        PublicMediaImage image, int order, bool isCover, string altText, bool isDecorative, string? caption)
    {
        ArgumentNullException.ThrowIfNull(image);

        return new CatalogTourImageDto
        {
            Id = image.Id,
            SortOrder = order,
            IsCover = isCover,
            AltText = altText,
            IsDecorative = isDecorative,
            Caption = caption,
            ResponsiveVariants = [.. image.ResponsiveVariants
                .OrderBy(variant => variant.Width)
                .Select(variant => new CatalogMediaImageVariantDto
                {
                    Width = variant.Width,
                    Height = variant.Height,
                    ContentType = variant.ContentType,
                    FileSizeBytes = variant.FileSizeBytes
                })]
        };
    }
}
