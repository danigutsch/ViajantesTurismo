namespace ViajantesTurismo.Catalog.Contracts.Application;

/// <summary>Identifies a tour and its publishing status in the Management list.</summary>
/// <param name="Id">The Catalog tour identifier.</param>
/// <param name="Identifier">The tour identifier shown to managers.</param>
/// <param name="Title">The public title.</param>
/// <param name="Slug">The public URL slug.</param>
/// <param name="IsPublished">Whether the projection is flagged as published.</param>
/// <param name="UpdatedAt">When the projection last changed.</param>
public sealed record CatalogTourListItemDto(
    Guid Id,
    string Identifier,
    string Title,
    string Slug,
    bool IsPublished,
    DateTimeOffset UpdatedAt);
