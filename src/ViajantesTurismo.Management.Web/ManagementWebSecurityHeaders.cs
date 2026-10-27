using PandaBear.AspNetCore;

namespace ViajantesTurismo.Management.Web;

/// <summary>
/// Applies the Management Web security headers and content security policy.
/// </summary>
internal static class ManagementWebSecurityHeaders
{
    private const string ContentSecurityPolicy = "default-src 'self'; base-uri 'self'; frame-ancestors 'none'; object-src 'none'; script-src 'self'; style-src 'self'; img-src 'self' data: https:; font-src 'self' data:; connect-src 'self' ws: wss:";

    /// <summary>
    /// Adds the security headers middleware with the Management Web content security policy.
    /// </summary>
    public static IApplicationBuilder UseManagementWebSecurityHeaders(this IApplicationBuilder app)
    {
        return app.UseSecurityHeaders(ContentSecurityPolicy);
    }
}
