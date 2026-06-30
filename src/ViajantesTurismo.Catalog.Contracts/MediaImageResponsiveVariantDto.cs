using System.ComponentModel.DataAnnotations;

namespace ViajantesTurismo.Catalog.Contracts;

/// <summary>
/// Responsive rendition for a public media image contract.
/// </summary>
public sealed record MediaImageResponsiveVariantDto
{
    /// <summary>
    /// Gets the public rendition URI.
    /// </summary>
    [Required]
    public required Uri Uri { get; init; }

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
