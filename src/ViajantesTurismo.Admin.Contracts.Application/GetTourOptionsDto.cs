namespace ViajantesTurismo.Admin.Contracts.Application;

/// <summary>A bounded slice of active tour choices.</summary>
/// <param name="Items">The choices ordered from newest to oldest by ID.</param>
/// <param name="Total">The number of active tours that match the search.</param>
public sealed record GetTourOptionsDto(IReadOnlyList<GetTourOptionDto> Items, int Total);
