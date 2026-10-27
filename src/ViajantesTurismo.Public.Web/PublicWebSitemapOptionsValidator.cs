using Microsoft.Extensions.Options;
using PandaBear.AspNetCore;

namespace ViajantesTurismo.Public.Web;

/// <summary>Validates that the sitemap canonical origin is an absolute HTTP or HTTPS origin.</summary>
internal sealed class PublicWebSitemapOptionsValidator : IValidateOptions<PublicWebSitemapOptions>
{
    /// <summary>Fails when the canonical origin is missing or not a valid origin.</summary>
    public ValidateOptionsResult Validate(string? name, PublicWebSitemapOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrWhiteSpace(options.CanonicalOrigin))
        {
            return ValidateOptionsResult.Fail("PublicWeb:Sitemap:CanonicalOrigin must be provided.");
        }

        return SitemapCanonicalOrigin.IsValid(options.CanonicalOrigin)
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(
                "PublicWeb:Sitemap:CanonicalOrigin must be an absolute HTTP or HTTPS origin without a path, query, fragment, or userinfo.");
    }
}
