namespace ViajantesTurismo.Catalog.Contracts;

/// <summary>
/// Represents a Catalog tour projection exposed to management and public web clients.
/// </summary>
/// <param name="Id">The Catalog tour identifier.</param>
/// <param name="AdminTourId">The source Admin tour identifier.</param>
/// <param name="Identifier">The customer-facing tour identifier.</param>
/// <param name="Title">The customer-facing tour title.</param>
/// <param name="Slug">The stable public URL slug.</param>
/// <param name="IsPublished">A value indicating whether the tour is visible on the public website.</param>
/// <param name="Images">The images that can be rendered in public tour galleries.</param>
/// <param name="UpdatedAt">The timestamp of the event that last updated the projection.</param>
public sealed record CatalogTourDto(
    Guid Id,
    Guid AdminTourId,
    string Identifier,
    string Title,
    string Slug,
    bool IsPublished,
    IReadOnlyList<CatalogTourImageDto> Images,
    DateTimeOffset UpdatedAt);
