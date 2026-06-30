namespace ViajantesTurismo.Catalog.Domain.Media;

/// <summary>
/// Public rendition of a media image for responsive rendering.
/// </summary>
/// <param name="Uri">The public rendition URI.</param>
/// <param name="Width">The rendition width in pixels.</param>
/// <param name="Height">The rendition height in pixels.</param>
/// <param name="ContentType">The rendition media content type.</param>
/// <param name="FileSizeBytes">The rendition file size in bytes.</param>
/// <param name="SortOrder">The persisted display order.</param>
public sealed record MediaImageResponsiveVariant(
    Uri Uri,
    int Width,
    int Height,
    string ContentType,
    long FileSizeBytes,
    int SortOrder = 0);
