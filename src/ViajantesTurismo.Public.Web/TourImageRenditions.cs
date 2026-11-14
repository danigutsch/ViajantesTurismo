using PandaBear.Blazor.Site;
using ViajantesTurismo.Catalog.Contracts.Application;

namespace ViajantesTurismo.Public.Web;

/// <summary>Chooses catalog image renditions and builds their local media addresses.</summary>
internal static class TourImageRenditions
{
    /// <summary>The smallest width a card image should have, so it stays sharp at a third of a wide screen.</summary>
    public const int CardMinimumWidth = 640;

    /// <summary>The <c>sizes</c> of a hero or gallery image: half the page from the 48rem breakpoint, else the full width.</summary>
    public const string HalfWidthSizes = "(min-width: 48rem) 50vw, 100vw";

    /// <summary>Gets the media route segment of a rendition content type, or <see langword="null" /> when the type is not served.</summary>
    public static string? FormatSegment(string contentType) => contentType.Trim().ToUpperInvariant() switch
    {
        "IMAGE/AVIF" => "avif",
        "IMAGE/JPEG" => "jpg",
        "IMAGE/PNG" => "png",
        "IMAGE/WEBP" => "webp",
        _ => null
    };

    /// <summary>Gets the rendition every browser can show: the largest JPEG or PNG, else the largest served rendition.</summary>
    public static CatalogMediaImageVariantDto? Fallback(CatalogTourImageDto image)
    {
        ArgumentNullException.ThrowIfNull(image);

        return Served(image)
            .OrderByDescending(IsUniversal)
            .ThenByDescending(variant => variant.Width)
            .FirstOrDefault();
    }

    /// <summary>Gets the largest served rendition, for sharing previews.</summary>
    public static CatalogMediaImageVariantDto? Largest(CatalogTourImageDto image)
    {
        ArgumentNullException.ThrowIfNull(image);

        return Served(image).OrderByDescending(variant => variant.Width).FirstOrDefault();
    }

    /// <summary>
    /// Gets the single rendition of a card image: the smallest JPEG or PNG at least <see cref="CardMinimumWidth" /> wide, else
    /// the largest one, else the same choice among the other served formats.
    /// </summary>
    public static CatalogMediaImageVariantDto? Card(CatalogTourImageDto image)
    {
        ArgumentNullException.ThrowIfNull(image);

        var served = Served(image).ToArray();
        var candidates = served.Any(IsUniversal) ? [.. served.Where(IsUniversal)] : served;
        return candidates.Where(variant => variant.Width >= CardMinimumWidth).OrderBy(variant => variant.Width).FirstOrDefault()
            ?? candidates.OrderByDescending(variant => variant.Width).FirstOrDefault();
    }

    /// <summary>Gets the image shown for a tour: the cover, else the first by sort order.</summary>
    public static CatalogTourImageDto? Cover(IEnumerable<CatalogTourImageDto> images) => images
        .OrderByDescending(image => image.IsCover)
        .ThenBy(image => image.SortOrder)
        .FirstOrDefault();

    /// <summary>Builds the local media address of a rendition.</summary>
    public static string Path(LinkGenerator linkGenerator, Guid imageId, CatalogMediaImageVariantDto variant)
    {
        ArgumentNullException.ThrowIfNull(linkGenerator);
        ArgumentNullException.ThrowIfNull(variant);

        var format = FormatSegment(variant.ContentType)
            ?? throw new ArgumentOutOfRangeException(nameof(variant), variant.ContentType, "Unsupported media content type.");
        return linkGenerator.GetPathByName(
                PublicWebEndpoints.PublicMediaByRenditionEndpointName,
                new
                {
                    id = imageId,
                    width = variant.Width,
                    format
                })
            ?? throw new InvalidOperationException("The public media rendition route is not available.");
    }

    /// <summary>
    /// Builds the responsive image of a catalog image: one source per served content type (AVIF, then WebP, then JPEG, then PNG)
    /// with a width candidate per rendition, and <see cref="Fallback" /> as the <c>img</c>.
    /// </summary>
    /// <exception cref="InvalidOperationException">The image has no served rendition.</exception>
    public static ResponsiveImage Responsive(LinkGenerator linkGenerator, CatalogTourImageDto image, string sizes)
    {
        ArgumentNullException.ThrowIfNull(image);

        var fallback = Fallback(image)
            ?? throw new InvalidOperationException("A responsive tour image requires a supported media variant.");
        var sources = Served(image)
            .GroupBy(variant => variant.ContentType.Trim(), StringComparer.OrdinalIgnoreCase)
            .OrderBy(group => ContentTypeSortOrder(group.Key))
            .Select(group => ImageSource.Create(group.Key, group
                .DistinctBy(variant => variant.Width)
                .OrderBy(variant => variant.Width)
                .Select(variant => ImageCandidate.AtWidth(Target(linkGenerator, image.Id, variant), variant.Width))))
            .ToArray();
        return ResponsiveImage.Create(Target(linkGenerator, image.Id, fallback), fallback.Width, fallback.Height, Alternative(image), sizes, sources: sources);
    }

    /// <summary>Builds the gallery of catalog images, each at <see cref="HalfWidthSizes" /> with its caption when it has one.</summary>
    /// <exception cref="InvalidOperationException">An image has no served rendition.</exception>
    public static IReadOnlyList<GalleryImage> Gallery(LinkGenerator linkGenerator, IEnumerable<CatalogTourImageDto> images)
    {
        ArgumentNullException.ThrowIfNull(images);

        return [.. images.Select(image => new GalleryImage(
            Responsive(linkGenerator, image, HalfWidthSizes),
            string.IsNullOrWhiteSpace(image.Caption) ? null : image.Caption))];
    }

    /// <summary>Gets the accessibility decision of a catalog image: its alternative text, or decorative when it has none.</summary>
    public static ImageAlternative Alternative(CatalogTourImageDto image)
    {
        ArgumentNullException.ThrowIfNull(image);

        return string.IsNullOrWhiteSpace(image.AltText) ? ImageAlternative.Decorative() : ImageAlternative.Describe(image.AltText);
    }

    private static LinkTarget Target(LinkGenerator linkGenerator, Guid imageId, CatalogMediaImageVariantDto variant) =>
        LinkTarget.FromString(Path(linkGenerator, imageId, variant));

    private static int ContentTypeSortOrder(string contentType) => FormatSegment(contentType) switch
    {
        "avif" => 0,
        "webp" => 1,
        "jpg" => 2,
        _ => 3
    };

    private static IEnumerable<CatalogMediaImageVariantDto> Served(CatalogTourImageDto image) =>
        image.ResponsiveVariants.Where(variant => FormatSegment(variant.ContentType) is not null);

    private static bool IsUniversal(CatalogMediaImageVariantDto variant) => FormatSegment(variant.ContentType) is "jpg" or "png";
}
