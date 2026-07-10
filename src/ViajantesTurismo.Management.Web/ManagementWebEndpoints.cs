using SharedKernel.AspNetCore;
using ViajantesTurismo.Management.Web.Components;

namespace ViajantesTurismo.Management.Web;

internal static class ManagementWebEndpoints
{
    private const string ManagementRobotsTxt = "User-agent: *\nDisallow: /";

    internal static IEndpointRouteBuilder MapManagementWebEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapRobotsTxt(ManagementRobotsTxt);

        app.MapStaticAssets();

        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();

        return app;
    }
}
