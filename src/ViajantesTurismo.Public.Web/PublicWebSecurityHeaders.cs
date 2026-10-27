using PandaBear.AspNetCore;

namespace ViajantesTurismo.Public.Web;

/// <summary>Applies the Public Web security headers, including the content security policy.</summary>
internal static class PublicWebSecurityHeaders
{
    private const string ContentSecurityPolicy = "default-src 'self'; base-uri 'self'; frame-ancestors 'none'; object-src 'none'; script-src 'self'; style-src 'self'; img-src 'self' data: https:; font-src 'self' data:; connect-src 'self'";

    /// <summary>Adds middleware that sets the Public Web security headers.</summary>
    public static IApplicationBuilder UsePublicWebSecurityHeaders(this IApplicationBuilder app)
    {
        return app.UseSecurityHeaders(ContentSecurityPolicy);
    }
}
