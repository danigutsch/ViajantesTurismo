namespace ViajantesTurismo.Catalog.Contracts.Application;

/// <summary>A published tour album and its effective cover.</summary>
/// <param name="Slug">The Catalog tour slug.</param>
/// <param name="Title">The title shown for the album.</param>
/// <param name="TitleLanguage">The content language of the title.</param>
/// <param name="PhotoCount">The number of photos eligible in the requested culture.</param>
/// <param name="Cover">The eligible chosen cover, or the first eligible photo.</param>
public sealed record CatalogGallerySummaryDto(string Slug, string Title, string TitleLanguage, int PhotoCount, CatalogTourImageDto Cover);
