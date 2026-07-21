namespace ViajantesTurismo.Catalog.Domain.Tours;

/// <summary>
/// Event raised when a Catalog tour is removed from the public website.
/// </summary>
/// <param name="CatalogTourId">The Catalog tour identifier.</param>
public sealed record CatalogTourUnpublished(Guid CatalogTourId);
