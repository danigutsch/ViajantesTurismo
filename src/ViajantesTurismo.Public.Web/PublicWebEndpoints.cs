using Microsoft.Extensions.Options;
using PandaBear.AspNetCore.Seo;
using PandaBear.Branding;
using PandaBear.Blazor.Site.Theming;
using PandaBear.BuildingBlocks;
using PandaBear.HttpCaching.AspNetCore;
using ViajantesTurismo.Catalog.Contracts.Application;
using ViajantesTurismo.Public.Web.Branding;
using ViajantesTurismo.Public.Web.Components;

namespace ViajantesTurismo.Public.Web;

/// <summary>Maps the Public Web endpoints: error, robots.txt, sitemap, theme stylesheet, catalog media and Razor components.</summary>
internal static class PublicWebEndpoints
{
    private const string SitemapPath = "/sitemap.xml";

    /// <summary>Path of the stylesheet that renders the brand theme; pages link it with the theme hash as <c>v</c>.</summary>
    internal const string ThemeStylesheetPath = "/theme.css";

    /// <summary>Endpoint name of the public media-by-rendition route.</summary>
    internal const string PublicMediaByRenditionEndpointName = "public-media-by-rendition";

    private static readonly string[] StaticPagePaths = ["/", "/group-bike-tours", "/gallery"];

    /// <summary>Maps the Public Web endpoints onto the route builder.</summary>
    internal static IEndpointRouteBuilder MapPublicWebEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapGet("/Error", (HttpContext httpContext) =>
            {
                PublicWebHttpCache.SetNoStore(httpContext);
                return Results.Problem();
            })
            .ExcludeFromDescription();

        var sitemapOptions = app.ServiceProvider.GetRequiredService<IOptions<CanonicalOriginOptions>>().Value;
        var sitemapUri = new PublicSiteLinks(sitemapOptions).Absolute(SitemapPath);
        app.MapRobotsTxt($"User-agent: *\nAllow: /\nSitemap: {sitemapUri.AbsoluteUri}")
            .ExcludeFromDescription();

        app.MapGet(SitemapPath, GetSitemap)
            .RequireRateLimiting(PublicWebSecurityBaseline.PublicReadRateLimitPolicy)
            .ExcludeFromDescription();

        app.MapGet(ThemeStylesheetPath, GetThemeStylesheet)
            .RequireRateLimiting(PublicWebSecurityBaseline.PublicReadRateLimitPolicy)
            .ExcludeFromDescription();

        app.MapGet("/catalog/media/{id:guid}/{width:int}/{format}", GetPublicMedia)
            .RequireRateLimiting(PublicWebSecurityBaseline.PublicReadRateLimitPolicy)
            .WithName(PublicMediaByRenditionEndpointName)
            .ExcludeFromDescription();

        app.MapStaticAssets();

        app.MapRazorComponents<App>()
            .RequireRateLimiting(PublicWebSecurityBaseline.PublicReadRateLimitPolicy);

        return app;
    }

    /// <summary>
    /// Renders the current theme. A request for its content hash (<c>?v=</c>, as the pages link it) is cached as immutable; any
    /// other request keeps the default freshness, so a stale link never pins old colours.
    /// </summary>
    private static async Task<IResult> GetThemeStylesheet(IBrandingApiClient brandingApi, HttpContext httpContext, string? v, CancellationToken ct)
    {
        var theme = PublicBranding.Theme(await PublicBranding.Load(brandingApi, ct));
        if (string.Equals(v, SiteThemeStylesheet.Hash(theme), StringComparison.Ordinal))
        {
            HttpCacheHeaders.SetImmutable(httpContext);
        }

        return Results.Text(SiteThemeStylesheet.Render(theme), "text/css; charset=utf-8");
    }

    private static async Task<IResult> GetSitemap(
        HttpContext httpContext,
        IOptions<CanonicalOriginOptions> sitemapOptions,
        IPublicCatalogApiClient catalogApi,
        CancellationToken ct)
    {
        PublicWebHttpCache.SetNoStore(httpContext);
        TourSummaryDto[] tours;
        try
        {
            var outcome = await catalogApi.GetPublishedTours(ct);
            if (outcome is not { Kind: PandaBear.HttpClients.HttpOutcomeKind.Succeeded, Value: { } publishedTours })
            {
                PublicWebHttpCache.SetServiceUnavailableNoStore(httpContext);
                return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
            }

            tours = publishedTours;
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

        var links = new PublicSiteLinks(sitemapOptions.Value);
        var cultureCount = PublicSiteCultures.Supported.Cultures.Count;
        var tourLimit = (SitemapXmlSerializer.MaximumUrlCount / cultureCount) - StaticPagePaths.Length;
        var urls = StaticPagePaths
            .SelectMany(path => links.SitemapEntries(path))
            .Concat(tours
                .Where(tour => Slug.IsCanonical(tour.Slug))
                .Take(tourLimit)
                .SelectMany(tour => links.SitemapEntries($"/group-bike-tours/{tour.Slug}", tour.UpdatedAt)));

        var sitemap = await SitemapXmlSerializer.Serialize(urls, ct);
        return Results.File(sitemap, SitemapXmlSerializer.ContentType);
    }

    private static async Task<IResult> GetPublicMedia(
        Guid id,
        int width,
        string format,
        IPublicCatalogApiClient catalogApi,
        HttpContext httpContext,
        CancellationToken ct)
    {
        PublicMediaObjectResponse? media;
        try
        {
            media = await catalogApi.GetPublicMedia(id, width, format, ct).ConfigureAwait(false);
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
        if (media is null)
        {
            PublicWebHttpCache.SetNoStore(httpContext);
            return Results.NotFound();
        }

        PublicWebHttpCache.SetNoStore(httpContext);
        httpContext.Response.Headers["X-Content-Type-Options"] = "nosniff";
        httpContext.Response.RegisterForDisposeAsync(media);
        return Results.Stream(media.Content, media.ContentType, enableRangeProcessing: false);
    }
}
