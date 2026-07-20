using System.ComponentModel.DataAnnotations;

namespace ViajantesTurismo.Catalog.Contracts.Application;

/// <summary>
/// Represents the published Catalog tour content rendered by a public detail page.
/// </summary>
public sealed record TourDetailsDto
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
    /// Gets the detailed customer-facing tour description.
    /// </summary>
    [StringLength(ContractConstants.MaxBodyLength)]
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Gets the plain-text customer-facing itinerary.
    /// </summary>
    [StringLength(ContractConstants.MaxBodyLength)]
    public string Itinerary { get; init; } = string.Empty;

    /// <summary>
    /// Gets the optional search-engine title override.
    /// </summary>
    [StringLength(ContractConstants.MaxNameLength)]
    public string SeoTitle { get; init; } = string.Empty;

    /// <summary>
    /// Gets the optional search-engine description override.
    /// </summary>
    [StringLength(ContractConstants.MaxBodyLength)]
    public string SeoDescription { get; init; } = string.Empty;

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
