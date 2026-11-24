namespace ViajantesTurismo.Catalog.Contracts.Application;

/// <summary>One page of a published tour album.</summary>
/// <param name="Slug">The Catalog tour slug.</param>
/// <param name="Title">The title shown for the album.</param>
/// <param name="TitleLanguage">The content language of the title.</param>
/// <param name="Page">The current page number, starting at one.</param>
/// <param name="PageSize">The maximum number of photos per page.</param>
/// <param name="PhotoCount">The number of photos eligible in the requested culture.</param>
/// <param name="Photos">The requested page of photos in display order.</param>
public sealed record CatalogGalleryAlbumDto(
    string Slug, string Title, string TitleLanguage, int Page, int PageSize, int PhotoCount, IReadOnlyList<CatalogTourImageDto> Photos);
