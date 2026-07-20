using System.ComponentModel.DataAnnotations;

namespace ViajantesTurismo.Catalog.Contracts.Application;

/// <summary>
/// Represents a published Catalog tour used by public listing and gallery pages.
/// </summary>
public sealed record TourSummaryDto
{
    /// <summary>
    /// Gets the customer-facing tour title.
    /// </summary>
    [Required, StringLength(ContractConstants.MaxNameLength, MinimumLength = 1)]
    public required string Title { get; init; }

    /// <summary>
    /// Gets the stable public URL slug.
    /// </summary>
    [Required, StringLength(ContractConstants.MaxSlugLength, MinimumLength = 1)]
    public required string Slug { get; init; }

    /// <summary>
    /// Gets the concise customer-facing tour summary.
    /// </summary>
    [Required, StringLength(ContractConstants.MaxBodyLength, MinimumLength = 1)]
    public required string Summary { get; init; }

    /// <summary>
    /// Gets reviewed images that can be rendered publicly.
    /// </summary>
    [Required]
    public required IReadOnlyList<CatalogTourImageDto> Images { get; init; }

    /// <summary>
    /// Gets the timestamp of the event that last updated the public projection.
    /// </summary>
    [Required]
    public required DateTimeOffset UpdatedAt { get; init; }
}
