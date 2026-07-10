using ViajantesTurismo.Public.Web.Components;

namespace ViajantesTurismo.Public.Web;

internal static class PublicWebEndpoints
{
    internal static IEndpointRouteBuilder MapPublicWebEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapGet("/Error", (HttpContext httpContext) =>
            {
                PublicWebHttpCache.SetNoStore(httpContext);
                return Results.Problem();
            })
            .ExcludeFromDescription();

        app.MapStaticAssets();

        app.MapRazorComponents<App>();

        return app;
    }
}
