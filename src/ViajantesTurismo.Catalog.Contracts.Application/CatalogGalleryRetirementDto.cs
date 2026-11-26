namespace ViajantesTurismo.Catalog.Contracts.Application;

/// <summary>Reports the recorded withdrawal that closed an accepted Gallery placement.</summary>
/// <param name="RetirementId">The retirement identity carried by the Catalog completion event.</param>
/// <param name="WithdrawnAt">When Catalog recorded the withdrawal.</param>
public sealed record CatalogGalleryRetirementDto(Guid RetirementId, DateTimeOffset WithdrawnAt);
