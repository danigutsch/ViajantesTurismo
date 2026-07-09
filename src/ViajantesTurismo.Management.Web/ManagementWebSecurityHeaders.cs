namespace ViajantesTurismo.Management.Web;

internal static class ManagementWebSecurityHeaders
{
    private const string ContentSecurityPolicy = "default-src 'self'; base-uri 'self'; frame-ancestors 'none'; object-src 'none'; script-src 'self'; style-src 'self'; img-src 'self' data: https:; font-src 'self' data:; connect-src 'self' ws: wss:";

    private const string ReferrerPolicyHeaderName = "Referrer-Policy";

    private const string PermissionsPolicyHeaderName = "Permissions-Policy";

    public static IApplicationBuilder UseManagementWebSecurityHeaders(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        return app.Use(static async (httpContext, next) =>
        {
            SetSecurityHeaders(httpContext);
            await next(httpContext);
        });
    }

    private static void SetSecurityHeaders(HttpContext httpContext)
    {
        httpContext.Response.Headers.ContentSecurityPolicy = ContentSecurityPolicy;
        httpContext.Response.Headers.XFrameOptions = "DENY";
        httpContext.Response.Headers[ReferrerPolicyHeaderName] = "no-referrer";
        httpContext.Response.Headers.XContentTypeOptions = "nosniff";
        httpContext.Response.Headers[PermissionsPolicyHeaderName] = "camera=(), geolocation=(), microphone=(), payment=()";
    }
}
