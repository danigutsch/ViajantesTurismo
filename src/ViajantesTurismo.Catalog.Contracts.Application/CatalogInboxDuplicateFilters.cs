namespace ViajantesTurismo.Catalog.Contracts.Application;

/// <summary>Specifies filters and a keyset for retained Catalog duplicate observations.</summary>
/// <param name="EventId">The exact event identifier to inspect, when present.</param>
/// <param name="AfterObservedAt">The observation time from the previous page's continuation.</param>
/// <param name="AfterEventId">The event identifier from the previous page's continuation.</param>
/// <param name="Limit">The maximum number of observations in this page.</param>
/// <param name="ObservedSince">The inclusive lower bound for the latest observation time, when present.</param>
public sealed record CatalogInboxDuplicateFilters(
    Guid? EventId = null, DateTimeOffset? AfterObservedAt = null, Guid? AfterEventId = null,
    int Limit = CatalogInboxDuplicateFilters.MaxPageSize, DateTimeOffset? ObservedSince = null)
{
    /// <summary>The maximum size of a Catalog inbox inspection page.</summary>
    public const int MaxPageSize = 25;
}
