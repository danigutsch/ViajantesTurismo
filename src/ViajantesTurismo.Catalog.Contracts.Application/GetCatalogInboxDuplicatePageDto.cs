namespace ViajantesTurismo.Catalog.Contracts.Application;

/// <summary>Contains one bounded page of completed Catalog duplicate observations.</summary>
/// <param name="Rows">The rows ordered by most recent observation and event identifier.</param>
/// <param name="NextObservedAt">The observation time of the continuation cursor, when present.</param>
/// <param name="NextEventId">The event identifier of the continuation cursor, when present.</param>
public sealed record GetCatalogInboxDuplicatePageDto(
    IReadOnlyList<CatalogInboxDuplicateRowDto> Rows, DateTimeOffset? NextObservedAt, Guid? NextEventId);
