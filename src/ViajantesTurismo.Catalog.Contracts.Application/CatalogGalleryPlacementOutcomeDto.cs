namespace ViajantesTurismo.Catalog.Contracts.Application;

/// <summary>Reports the outcome of one accepted Catalog placement.</summary>
/// <param name="IntentId">The accepted placement identity.</param>
/// <param name="Sequence">The tour's placement sequence.</param>
/// <param name="Status">The observed status of this placement.</param>
public sealed record CatalogGalleryPlacementOutcomeDto(Guid IntentId, long Sequence, CatalogGalleryPlacementStatusDto Status);
