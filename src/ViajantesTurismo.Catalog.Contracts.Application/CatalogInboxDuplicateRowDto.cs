namespace ViajantesTurismo.Catalog.Contracts.Application;

/// <summary>Contains the retained duplicate-delivery metadata for one Catalog event.</summary>
/// <param name="EventId">The immutable event identifier.</param>
/// <param name="DuplicateCount">The number of completed duplicates observed for that event.</param>
/// <param name="LastObservedAt">The UTC time of the latest duplicate observation.</param>
public sealed record CatalogInboxDuplicateRowDto(Guid EventId, int DuplicateCount, DateTimeOffset LastObservedAt);
