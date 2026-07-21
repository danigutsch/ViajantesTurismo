using System.ComponentModel.DataAnnotations;

namespace ViajantesTurismo.Catalog.Contracts.Application;

/// <summary>
/// Represents a Catalog tour projection exposed to management clients.
/// </summary>
public sealed record CatalogTourDto
{
    /// <summary>
    /// Gets the Catalog tour identifier.
    /// </summary>
    [Required]
    public required Guid Id { get; init; }

    /// <summary>
    /// Gets the source Admin tour identifier.
    /// </summary>
    [Required]
    public required Guid AdminTourId { get; init; }

    /// <summary>
    /// Gets the customer-facing tour identifier.
    /// </summary>
    [Required, StringLength(ContractConstants.MaxDefaultLength, MinimumLength = 1)]
    public required string Identifier { get; init; }

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
    [StringLength(ContractConstants.MaxBodyLength)]
    public string Summary { get; init; } = string.Empty;

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
    /// Gets a value indicating whether the tour is visible on the public website.
    /// </summary>
    [Required]
    public required bool IsPublished { get; init; }

    /// <summary>
    /// Gets the optimistic stream version for management mutations.
    /// </summary>
    [Range(1, long.MaxValue)]
    public required long Version { get; init; }

    /// <summary>
    /// Gets the images that can be rendered in public tour galleries.
    /// </summary>
    [Required]
    public required IReadOnlyList<CatalogTourImageDto> Images { get; init; }

    /// <summary>
    /// Gets the timestamp of the event that last updated the projection.
    /// </summary>
    [Required]
    public required DateTimeOffset UpdatedAt { get; init; }
}
