using System.ComponentModel.DataAnnotations;

namespace ViajantesTurismo.Catalog.Contracts.Application;

/// <summary>
/// Request to update customer-facing Catalog tour presentation fields.
/// </summary>
public sealed record UpsertCatalogTourPresentationRequest
{
    /// <summary>
    /// Gets the customer-facing tour title.
    /// </summary>
    [Required, StringLength(ContractConstants.MaxNameLength, MinimumLength = 1)]
    public required string Title { get; init; }

    /// <summary>
    /// Gets the public URL slug.
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
    /// Gets the stream version on which this edit is based.
    /// </summary>
    [Range(1, long.MaxValue)]
    public required long ExpectedVersion { get; init; }
}
