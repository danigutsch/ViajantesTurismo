namespace ViajantesTurismo.Catalog.Contracts.Application;

/// <summary>Summarizes unresolved Gallery placements with repeated delivery attempts and a due retry.</summary>
/// <param name="DuePlacementCount">The number of placements with a due retry.</param>
/// <param name="OldestDueAt">The oldest due retry time in UTC, when any placements have a due retry.</param>
public sealed record GetCatalogGalleryRetryHealthDto(long DuePlacementCount, DateTimeOffset? OldestDueAt);
