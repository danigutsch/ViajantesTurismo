namespace ViajantesTurismo.Admin.Contracts.Application;

/// <summary>One page of the active Management tours.</summary>
/// <param name="Items">The tours in the requested bounded window.</param>
/// <param name="Total">The total number of active tours.</param>
public sealed record GetTourPageDto(IReadOnlyList<GetTourListItemDto> Items, int Total);
