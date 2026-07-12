using Microsoft.Extensions.Options;
using SharedKernel.AspNetCore;
using ViajantesTurismo.Catalog.Contracts.Application;
using ViajantesTurismo.Public.Web.Components;

namespace ViajantesTurismo.Public.Web;

internal static class PublicWebEndpoints
{
    private const string RobotsTxtPath = "/robots.txt";
    private const string SitemapPath = "/sitemap.xml";

    internal static IEndpointRouteBuilder MapPublicWebEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapGet("/Error", (HttpContext httpContext) =>
            {
                PublicWebHttpCache.SetNoStore(httpContext);
                return Results.Problem();
            })
            .ExcludeFromDescription();

        app.MapGet(RobotsTxtPath, GetPublicRobotsTxt)
            .ExcludeFromDescription();

        app.MapGet(SitemapPath, GetSitemap)
            .ExcludeFromDescription();

        app.MapGet("/catalog/media/{id:guid}", GetPublicMedia)
            .ExcludeFromDescription();
        app.MapGet("/catalog/media/{id:guid}/{width:int}", GetPublicMedia)
            .ExcludeFromDescription();

        app.MapStaticAssets();

        app.MapRazorComponents<App>();

        return app;
    }

    private static IResult GetPublicRobotsTxt(IOptions<PublicWebSitemapOptions> sitemapOptions)
    {
        var origin = SitemapCanonicalOrigin.Parse(sitemapOptions.Value.CanonicalOrigin);
        var sitemapUri = new Uri(origin, SitemapPath);
        return Results.Text(
            $"User-agent: *\nAllow: /\nSitemap: {sitemapUri.AbsoluteUri}",
            "text/plain; charset=utf-8");
    }

    private static async Task<IResult> GetSitemap(
        HttpContext httpContext,
        IOptions<PublicWebSitemapOptions> sitemapOptions,
        IPublicCatalogApiClient catalogApi,
        CancellationToken ct)
    {
        var origin = SitemapCanonicalOrigin.Parse(sitemapOptions.Value.CanonicalOrigin);

        CatalogTourDto[] tours;
        try
        {
            tours = await catalogApi.GetPublishedTours(ct);
        }
        catch (HttpRequestException)
        {
            PublicWebHttpCache.SetServiceUnavailableNoStore(httpContext);
            return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            PublicWebHttpCache.SetServiceUnavailableNoStore(httpContext);
            return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
        }

        var urls = new List<SitemapEntry>
        {
            new(new Uri(origin, "/")),
            new(new Uri(origin, "/group-bike-tours")),
            new(new Uri(origin, "/gallery"))
        };

        foreach (var tour in tours.Where(IsPublicTourPage).Take(SitemapXmlSerializer.MaximumUrlCount - urls.Count))
        {
            var tourUri = new Uri(origin, $"/group-bike-tours/{Uri.EscapeDataString(tour.Slug)}");
            urls.Add(new SitemapEntry(tourUri, tour.UpdatedAt));
        }

        var sitemap = await SitemapXmlSerializer.Serialize(urls, ct);
        return Results.File(sitemap, SitemapXmlSerializer.ContentType);
    }

    private static async Task<IResult> GetPublicMedia(
        Guid id,
        int? width,
        IPublicCatalogApiClient catalogApi,
        HttpContext httpContext,
        CancellationToken ct)
    {
        PublicMediaObjectResponse? media;
        try
        {
            media = await catalogApi.GetPublicMedia(id, width, ct).ConfigureAwait(false);
        }
        catch (HttpRequestException)
        {
            PublicWebHttpCache.SetServiceUnavailableNoStore(httpContext);
            return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
        }
        if (media is null)
        {
            PublicWebHttpCache.SetNoStore(httpContext);
            return Results.NotFound();
        }

        PublicWebHttpCache.SetPublishedContent(httpContext);
        httpContext.Response.RegisterForDisposeAsync(media);
        return Results.Stream(media.Content, media.ContentType, enableRangeProcessing: false);
    }

    private static bool IsPublicTourPage(CatalogTourDto tour)
    {
        return tour.IsPublished
            && !string.IsNullOrWhiteSpace(tour.Slug)
            && !string.Equals(tour.Slug, ".", StringComparison.Ordinal)
            && !string.Equals(tour.Slug, "..", StringComparison.Ordinal)
            && tour.Slug.All(character => !char.IsControl(character)
                && character is not '/' and not '\\' and not '?' and not '#');
    }
}
