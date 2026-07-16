using System.ComponentModel.DataAnnotations;

namespace ViajantesTurismo.Catalog.Contracts.Application;

/// <summary>
/// Represents a responsive rendition available for authenticated management preview.
/// </summary>
public sealed record CatalogMediaImageVariantDto
{
    /// <summary>
    /// Gets the rendition width in pixels.
    /// </summary>
    [Required, Range(1, int.MaxValue)]
    public required int Width { get; init; }

    /// <summary>
    /// Gets the rendition height in pixels.
    /// </summary>
    [Required, Range(1, int.MaxValue)]
    public required int Height { get; init; }

    /// <summary>
    /// Gets the rendition media content type.
    /// </summary>
    [Required, StringLength(ContractConstants.MaxContentTypeLength, MinimumLength = 1)]
    public required string ContentType { get; init; }

    /// <summary>
    /// Gets the rendition file size in bytes.
    /// </summary>
    [Required, Range(1, long.MaxValue)]
    public required long FileSizeBytes { get; init; }
}
