using System.Xml;
using System.Xml.Linq;
using Microsoft.Extensions.Options;
using ViajantesTurismo.Catalog.Contracts.Application;
using ViajantesTurismo.Public.Web.Components;

namespace ViajantesTurismo.Public.Web;

internal static class PublicWebEndpoints
{
    private const string RobotsTxtPath = "/robots.txt";
    private const string SitemapPath = "/sitemap.xml";
    private const string SitemapContentType = "application/xml; charset=utf-8";
    private const int SitemapProtocolMaximumUrlCount = 50_000;
    private static readonly XNamespace SitemapNamespace = "http://www.sitemaps.org/schemas/sitemap/0.9";

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

        app.MapStaticAssets();

        app.MapRazorComponents<App>();

        return app;
    }

    private static IResult GetPublicRobotsTxt(IOptions<PublicWebSitemapOptions> sitemapOptions)
    {
        var origin = new Uri(sitemapOptions.Value.CanonicalOrigin, UriKind.Absolute);
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
        var origin = new Uri(sitemapOptions.Value.CanonicalOrigin, UriKind.Absolute);

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

        var urls = new List<XElement>
        {
            CreateSitemapUrl(new Uri(origin, "/")),
            CreateSitemapUrl(new Uri(origin, "/group-bike-tours")),
            CreateSitemapUrl(new Uri(origin, "/gallery"))
        };

        foreach (var tour in tours.Where(IsPublicTourPage).Take(SitemapProtocolMaximumUrlCount - urls.Count))
        {
            var tourUri = new Uri(origin, $"/group-bike-tours/{Uri.EscapeDataString(tour.Slug)}");
            urls.Add(CreateSitemapUrl(tourUri, tour.UpdatedAt));
        }

        var document = new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            new XElement(SitemapNamespace + "urlset", urls));

        await using var stream = new MemoryStream();
        await document.SaveAsync(stream, SaveOptions.None, ct);

        return Results.File(stream.ToArray(), SitemapContentType);
    }

    private static XElement CreateSitemapUrl(Uri uri, DateTimeOffset? lastModified = null)
    {
        var url = new XElement(
            SitemapNamespace + "url",
            new XElement(SitemapNamespace + "loc", uri.AbsoluteUri));

        if (lastModified is { } timestamp && timestamp != default)
        {
            url.Add(new XElement(
                SitemapNamespace + "lastmod",
                XmlConvert.ToString(timestamp.UtcDateTime, XmlDateTimeSerializationMode.Utc)));
        }

        return url;
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
