using SharedKernel.AspNetCore;
using ViajantesTurismo.Public.Web.Components;

namespace ViajantesTurismo.Public.Web;

internal static class PublicWebEndpoints
{
    private const string PublicRobotsTxt = "User-agent: *\nAllow: /";

    internal static IEndpointRouteBuilder MapPublicWebEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapGet("/Error", (HttpContext httpContext) =>
            {
                PublicWebHttpCache.SetNoStore(httpContext);
                return Results.Problem();
            })
            .ExcludeFromDescription();

        app.MapRobotsTxt(PublicRobotsTxt);

        app.MapStaticAssets();

        app.MapRazorComponents<App>();

        return app;
    }
}
