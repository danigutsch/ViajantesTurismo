using ViajantesTurismo.Public.Web.Components;

namespace ViajantesTurismo.Public.Web;

internal static class PublicWebEndpoints
{
    internal static IEndpointRouteBuilder MapPublicWebEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/Error", () => Results.Problem())
            .ExcludeFromDescription();

        app.MapStaticAssets();

        app.MapRazorComponents<App>();

        return app;
    }
}
