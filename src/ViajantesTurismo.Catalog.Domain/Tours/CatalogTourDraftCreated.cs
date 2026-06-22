namespace ViajantesTurismo.Catalog.Domain.Tours;

/// <summary>
/// Event raised when a draft Catalog tour stream is created from an Admin tour.
/// </summary>
/// <param name="CatalogTourId">The Catalog tour identifier.</param>
/// <param name="AdminTourId">The source Admin tour identifier.</param>
/// <param name="Identifier">The source Admin tour business identifier.</param>
/// <param name="Title">The initial customer-facing title.</param>
/// <param name="SourceEventId">The integration event that caused the draft creation.</param>
public sealed record CatalogTourDraftCreated(
    Guid CatalogTourId,
    Guid AdminTourId,
    string Identifier,
    string Title,
    Guid SourceEventId);
