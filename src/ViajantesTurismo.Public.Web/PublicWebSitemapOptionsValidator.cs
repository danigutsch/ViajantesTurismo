using Microsoft.Extensions.Options;
using SharedKernel.AspNetCore;

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

        return SitemapCanonicalOrigin.IsValid(options.CanonicalOrigin)
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(
                "PublicWeb:Sitemap:CanonicalOrigin must be an absolute HTTP or HTTPS origin without a path, query, fragment, or userinfo.");
    }
}
