namespace ViajantesTurismo.Catalog.Contracts.Application;

/// <summary>The facts shown in the Management list of editable content.</summary>
/// <param name="Key">The canonical content key.</param>
/// <param name="PublicationState">The current editorial publication state.</param>
public sealed record PublicContentOverviewItemDto(string Key, string PublicationState);
