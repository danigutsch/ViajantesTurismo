using SharedKernel.AspNetCore;
using SharedKernel.AspNetCore.Localization;
using SharedKernel.HttpCaching.AspNetCore;

namespace ViajantesTurismo.Public.Web;

/// <summary>
/// Finds where a culture address in another form moved, for <c>UsePermanentRedirects</c> (a 301): a regional or differently
/// cased culture segment (<c>/pt-BR/gallery</c>, <c>/EN/gallery</c>) and the former query-string localization
/// (<c>/gallery?culture=pt-BR</c> or <c>?language=pt</c>) both move to the short culture path (<c>/pt/gallery</c>).
/// </summary>
internal static class PublicCultureRedirects
{
    /// <summary>Returns the short culture path of the request, or <see langword="null" /> when it already has one or names no culture.</summary>
    public static ValueTask<PermanentRedirect?> Resolve(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        var request = httpContext.Request;
        if (CulturePath.TryParse(request.Path, PublicSiteCultures.Supported, out _, out _)
            || IsUnlocalized(request.Path))
        {
            return ValueTask.FromResult<PermanentRedirect?>(null);
        }

        return ValueTask.FromResult(FromCultureSegment(request.Path) ?? FromCultureQuery(request));
    }

    private static PermanentRedirect? FromCultureSegment(PathString path)
    {
        var value = path.Value;
        if (string.IsNullOrEmpty(value) || value.Length < 2)
        {
            return null;
        }

        var segmentEnd = value.IndexOf('/', 1, StringComparison.Ordinal);
        var segment = segmentEnd < 0 ? value[1..] : value[1..segmentEnd];
        var culture = PublicSiteCultures.Supported.Resolve(segment.Trim());
        if (culture is null)
        {
            return null;
        }

        var remainder = new PathString(segmentEnd < 0 ? "/" : value[segmentEnd..]);
        return new PermanentRedirect(
            CulturePath.Prepend(remainder, culture, PublicSiteCultures.Supported).ToUriComponent(),
            PreserveQuery: true);
    }

    private static PermanentRedirect? FromCultureQuery(HttpRequest request)
    {
        var requested = HttpCacheCultures.Normalize(request.Query[PublicContentCultureQuery.Culture].ToString())
            ?? HttpCacheCultures.Normalize(request.Query[PublicContentCultureQuery.Language].ToString());
        var culture = PublicSiteCultures.Supported.Resolve(requested);
        return culture is null
            ? null
            : new PermanentRedirect(CulturePath.Prepend(request.Path, culture, PublicSiteCultures.Supported).ToUriComponent());
    }

    private static bool IsUnlocalized(PathString path)
    {
        return path.Value?.AsSpan(path.Value.LastIndexOf('/', StringComparison.Ordinal) + 1).Contains('.') == true
            || PublicSiteCultures.UnlocalizedPrefixes.Any(prefix => path.StartsWithSegments(prefix, StringComparison.OrdinalIgnoreCase));
    }
}
