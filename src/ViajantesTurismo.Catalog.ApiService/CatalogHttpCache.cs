using System.Globalization;
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
        httpContext.Response.Headers[HeaderNames.ETag] = CreateWeakEtag(etagSeed);
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
        foreach (var tour in tours)
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
        var builder = new StringBuilder();
        AppendFields(
            builder,
            key,
            variant.Language.ToString(),
            variant.Title,
            variant.Body,
            variant.SeoTitle,
            variant.MetaDescription,
            variant.ShareSummary,
            variant.RequiresHumanReview.ToString());
        return builder.ToString();
    }

    public static string CreateThemeEtagSeed(PublicThemeSettingsDto theme)
    {
        var builder = new StringBuilder();
        AppendFields(
            builder,
            theme.PrimaryColor,
            theme.AccentColor,
            theme.BackgroundColor,
            theme.TextColor,
            theme.HeadingFontFamily,
            theme.BodyFontFamily);
        return builder.ToString();
    }

    private static void AppendTour(StringBuilder builder, CatalogTourDto tour)
    {
        builder
            .AppendLengthPrefixed(tour.Id.ToString())
            .AppendLengthPrefixed(tour.Title)
            .AppendLengthPrefixed(tour.Slug)
            .AppendLengthPrefixed(tour.IsPublished.ToString())
            .AppendLengthPrefixed(tour.UpdatedAt.ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture));

        foreach (var image in tour.Images.OrderBy(image => image.SortOrder).ThenBy(image => image.Uri.ToString(), StringComparer.Ordinal))
        {
            builder
                .AppendLengthPrefixed(image.Uri.ToString())
                .AppendLengthPrefixed(image.AltText)
                .AppendLengthPrefixed(image.Caption)
                .AppendLengthPrefixed(image.IsCover.ToString());
        }
    }

    private static void AppendFields(StringBuilder builder, params string?[] values)
    {
        foreach (var value in values)
        {
            builder.AppendLengthPrefixed(value);
        }
    }

    private static StringBuilder AppendLengthPrefixed(this StringBuilder builder, string? value)
    {
        value ??= string.Empty;
        return builder.Append(value.Length).Append(':').Append(value);
    }

    private static string CreateWeakEtag(string value)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return $"W/\"{Convert.ToHexString(hash)}\"";
    }
}
