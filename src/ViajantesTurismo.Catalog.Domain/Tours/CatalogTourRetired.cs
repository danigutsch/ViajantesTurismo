namespace ViajantesTurismo.Catalog.Domain.Tours;

/// <summary>Permanently closes the Catalog stream for a retired Admin tour.</summary>
/// <param name="AdminTourId">The source tour identifier.</param>
/// <param name="RetirementId">The retirement operation identifier.</param>
public sealed record CatalogTourRetired(Guid AdminTourId, Guid RetirementId);
