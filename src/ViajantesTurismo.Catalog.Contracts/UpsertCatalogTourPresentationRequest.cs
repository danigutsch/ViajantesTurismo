using System.ComponentModel.DataAnnotations;

namespace ViajantesTurismo.Catalog.Contracts;

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
    /// Gets a value indicating whether the tour is visible on the public website.
    /// </summary>
    [Required]
    public required bool IsPublished { get; init; }
}
