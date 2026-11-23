namespace ViajantesTurismo.Admin.Contracts.Application;

/// <summary>Text and capacity for a bounded active-tour option search.</summary>
/// <param name="Query">Text to match in the tour name or identifier.</param>
/// <param name="Limit">The requested number of options.</param>
public sealed record TourOptionSearchDto(string? Query, int Limit);
