using ViajantesTurismo.Management.Web.Components;

namespace ViajantesTurismo.Management.Web;

internal static class ManagementWebEndpoints
{
    internal static IEndpointRouteBuilder MapManagementWebEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapStaticAssets();

        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();

        return app;
    }
}
