namespace ViajantesTurismo.Catalog.Domain.Tours;

/// <summary>Records that publication superseded an accepted draft-only placement without changing the gallery.</summary>
/// <param name="CatalogTourId">The Catalog tour identifier.</param>
/// <param name="IntentId">The accepted placement identity.</param>
/// <param name="IntentSequence">The consumed journal sequence.</param>
public sealed record CatalogTourGalleryIntentRejected(Guid CatalogTourId, Guid IntentId, long IntentSequence);
