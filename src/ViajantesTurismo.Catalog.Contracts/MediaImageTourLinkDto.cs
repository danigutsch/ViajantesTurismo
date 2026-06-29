using System.ComponentModel.DataAnnotations;

namespace ViajantesTurismo.Catalog.Contracts;

/// <summary>
/// Catalog tour placement for a public media image contract.
/// </summary>
public sealed record MediaImageTourLinkDto
{
    /// <summary>
    /// Gets the Catalog tour identifier.
    /// </summary>
    [Required]
    public required Guid CatalogTourId { get; init; }

    /// <summary>
    /// Gets the display order within the tour gallery.
    /// </summary>
    [Required, Range(0, int.MaxValue)]
    public required int DisplayOrder { get; init; }

    /// <summary>
    /// Gets a value indicating whether the image is the tour cover image.
    /// </summary>
    [Required]
    public required bool IsCover { get; init; }
}
