using System.Security.Cryptography;
using System.Text;
using Microsoft.Net.Http.Headers;
using ViajantesTurismo.Catalog.Contracts;

namespace ViajantesTurismo.Catalog.ApiService;

internal static class CatalogHttpCache
{
    public static readonly TimeSpan PublicFreshness = TimeSpan.FromSeconds(60);

    public const string PublicCatalogTag = "public-catalog";

    public const string PublicContentTag = "public-content";

    public const string PublicThemeTag = "public-theme";

    public const string PublicCatalogArea = "public-catalog";

    public const string PublicContentArea = "public-content";

    public const string PublicThemeArea = "public-theme";

    private const string StaleWhileRevalidateDirective = "stale-while-revalidate";

    private const string StaleWhileRevalidateSeconds = "300";

    private const string PragmaNoCache = "no-cache";

    private const string ExpiredAtUnixEpoch = "0";

    public static void SetPublicHeaders(HttpContext httpContext, string etagSeed)
    {
        httpContext.Response.GetTypedHeaders().CacheControl = new CacheControlHeaderValue
        {
            Public = true,
            MaxAge = PublicFreshness,
            Extensions = { new NameValueHeaderValue(StaleWhileRevalidateDirective, StaleWhileRevalidateSeconds) }
        };
        httpContext.Response.Headers.ETag = CreateWeakEtag(etagSeed);
    }

    public static void SetNoStore(HttpContext httpContext)
    {
        httpContext.Response.GetTypedHeaders().CacheControl = new CacheControlHeaderValue
        {
            NoStore = true
        };
        httpContext.Response.Headers.Pragma = PragmaNoCache;
        httpContext.Response.Headers.Expires = ExpiredAtUnixEpoch;
    }

    public static string CreateToursEtagSeed(IReadOnlyCollection<CatalogTourDto> tours)
    {
        var builder = new StringBuilder();
        foreach (var tour in tours.OrderBy(tour => tour.Id))
        {
            AppendTour(builder, tour);
        }

        return builder.ToString();
    }

    public static string CreateTourEtagSeed(CatalogTourDto tour)
    {
        var builder = new StringBuilder();
        AppendTour(builder, tour);
        return builder.ToString();
    }

    public static string CreatePublicContentEtagSeed(string key, PublicContentVariantDto variant)
    {
        return string.Join(
            '|',
            key,
            variant.Language,
            variant.Title,
            variant.Body,
            variant.SeoTitle,
            variant.MetaDescription,
            variant.ShareSummary,
            variant.RequiresHumanReview);
    }

    public static string CreateThemeEtagSeed(PublicThemeSettingsDto theme)
    {
        return string.Join(
            '|',
            theme.PrimaryColor,
            theme.AccentColor,
            theme.BackgroundColor,
            theme.TextColor,
            theme.HeadingFontFamily,
            theme.BodyFontFamily);
    }

    private static void AppendTour(StringBuilder builder, CatalogTourDto tour)
    {
        builder
            .Append(tour.Id).Append('|')
            .Append(tour.Title).Append('|')
            .Append(tour.Slug).Append('|')
            .Append(tour.IsPublished).Append('|')
            .Append(tour.UpdatedAt.ToUnixTimeMilliseconds()).Append('|');

        foreach (var image in tour.Images.OrderBy(image => image.SortOrder).ThenBy(image => image.Uri.ToString(), StringComparer.Ordinal))
        {
            builder
                .Append(image.Uri).Append('|')
                .Append(image.AltText).Append('|')
                .Append(image.Caption).Append('|')
                .Append(image.IsCover).Append('|');
        }
    }

    private static string CreateWeakEtag(string value)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return $"W/\"{Convert.ToHexString(hash)}\"";
    }
}
