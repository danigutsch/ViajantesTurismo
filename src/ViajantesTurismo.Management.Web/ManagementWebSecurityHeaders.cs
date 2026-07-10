using SharedKernel.AspNetCore;

namespace ViajantesTurismo.Management.Web;

internal static class ManagementWebSecurityHeaders
{
    private const string ContentSecurityPolicy = "default-src 'self'; base-uri 'self'; frame-ancestors 'none'; object-src 'none'; script-src 'self'; style-src 'self'; img-src 'self' data: https:; font-src 'self' data:; connect-src 'self' ws: wss:";

    public static IApplicationBuilder UseManagementWebSecurityHeaders(this IApplicationBuilder app)
    {
        return app.UseSecurityHeaders(ContentSecurityPolicy);
    }
}
