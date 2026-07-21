namespace ViajantesTurismo.Catalog.Domain.Tours;

/// <summary>
/// Event raised when a Catalog tour becomes visible on the public website.
/// </summary>
/// <param name="CatalogTourId">The Catalog tour identifier.</param>
public sealed record CatalogTourPublished(Guid CatalogTourId);
