using Microsoft.AspNetCore.Http.HttpResults;
using SharedKernel.HttpCaching.AspNetCore;
using ViajantesTurismo.Catalog.Application.Gallery;
using ViajantesTurismo.Catalog.Contracts.Application;
using ViajantesTurismo.Catalog.Domain.PublicContent;

namespace ViajantesTurismo.Catalog.Api.Gallery;

/// <summary>Maps the anonymous, paged Catalog gallery.</summary>
internal static class CatalogGalleryEndpoints
{
    /// <summary>Maps the gallery index and the photo page for one tour.</summary>
    internal static void MapCatalogGalleryEndpoints(this IEndpointRouteBuilder versionedApi)
    {
        versionedApi.MapGet("/public/catalog/gallery", ListPublishedAlbums)
            .WithName(nameof(ListPublishedAlbums))
            .RequireRateLimiting(CatalogSecurityBaseline.PublicReadRateLimitPolicy)
            .AllowAnonymous();
        versionedApi.MapGet("/public/catalog/gallery/{slug}", GetPublishedAlbum)
            .WithName(nameof(GetPublishedAlbum))
            .RequireRateLimiting(CatalogSecurityBaseline.PublicReadRateLimitPolicy)
            .AllowAnonymous();
    }

    private static async Task<Results<Ok<CatalogGalleryIndexDto>, NotFound>> ListPublishedAlbums(
        string? culture, string? page, ICatalogGalleryReadStore store, HttpContext context, CancellationToken ct)
    {
        HttpCacheHeaders.SetNoStore(context);
        if (PublicContentCultures.Resolve(culture) is not { } resolved)
        {
            return TypedResults.NotFound();
        }

        var albumPage = await store.ListAlbums(resolved, page, ct);
        return albumPage is null ? TypedResults.NotFound() : TypedResults.Ok(albumPage);
    }

    private static async Task<Results<Ok<CatalogGalleryAlbumDto>, NotFound>> GetPublishedAlbum(
        string slug, string? culture, string? page, ICatalogGalleryReadStore store, HttpContext context, CancellationToken ct)
    {
        HttpCacheHeaders.SetNoStore(context);
        if (PublicContentCultures.Resolve(culture) is not { } resolved)
        {
            return TypedResults.NotFound();
        }

        var albumPage = await store.GetAlbum(slug, resolved, page, ct);
        return albumPage is null ? TypedResults.NotFound() : TypedResults.Ok(albumPage);
    }
}
