using SharedKernel.AspNetCore;

namespace ViajantesTurismo.Public.Web;

internal static class PublicWebSecurityHeaders
{
    private const string ContentSecurityPolicy = "default-src 'self'; base-uri 'self'; frame-ancestors 'none'; object-src 'none'; script-src 'self'; style-src 'self'; img-src 'self' data: https:; font-src 'self' data:; connect-src 'self'";

    public static IApplicationBuilder UsePublicWebSecurityHeaders(this IApplicationBuilder app)
    {
        return app.UseSecurityHeaders(ContentSecurityPolicy);
    }
}
