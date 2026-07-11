using Microsoft.Extensions.Options;

namespace ViajantesTurismo.Public.Web;

internal sealed class PublicWebSitemapOptionsValidator : IValidateOptions<PublicWebSitemapOptions>
{
    public ValidateOptionsResult Validate(string? name, PublicWebSitemapOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrWhiteSpace(options.CanonicalOrigin))
        {
            return ValidateOptionsResult.Fail("PublicWeb:Sitemap:CanonicalOrigin must be provided.");
        }

        return IsCanonicalOrigin(options.CanonicalOrigin)
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(
                "PublicWeb:Sitemap:CanonicalOrigin must be an absolute HTTP or HTTPS origin without a path, query, fragment, or userinfo.");
    }

    private static bool IsCanonicalOrigin(string value)
    {
        return value.Trim().Length == value.Length
            && Uri.TryCreate(value, UriKind.Absolute, out var uri)
            && uri.IsWellFormedOriginalString()
            && (string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
                || string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            && !string.IsNullOrEmpty(uri.Host)
            && uri.AbsolutePath == "/"
            && string.IsNullOrEmpty(uri.Query)
            && string.IsNullOrEmpty(uri.Fragment)
            && string.IsNullOrEmpty(uri.UserInfo)
            && !value.Contains('?', StringComparison.Ordinal)
            && !value.Contains('#', StringComparison.Ordinal);
    }
}
