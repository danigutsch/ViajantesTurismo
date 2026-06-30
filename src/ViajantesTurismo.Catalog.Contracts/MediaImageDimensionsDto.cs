using System.ComponentModel.DataAnnotations;

namespace ViajantesTurismo.Catalog.Contracts;

/// <summary>
/// Pixel dimensions for a media image contract.
/// </summary>
public sealed record MediaImageDimensionsDto
{
    /// <summary>
    /// Gets the image width in pixels.
    /// </summary>
    [Required, Range(1, int.MaxValue)]
    public required int Width { get; init; }

    /// <summary>
    /// Gets the image height in pixels.
    /// </summary>
    [Required, Range(1, int.MaxValue)]
    public required int Height { get; init; }
}
