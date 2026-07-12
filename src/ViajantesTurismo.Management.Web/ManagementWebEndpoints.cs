using SharedKernel.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using ViajantesTurismo.Management.Web.Components;

namespace ViajantesTurismo.Management.Web;

internal static class ManagementWebEndpoints
{
    private const string ManagementRobotsTxt = "User-agent: *\nDisallow: /";

    internal static IEndpointRouteBuilder MapManagementWebEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapRobotsTxt(ManagementRobotsTxt)
            .AllowAnonymous();

        app.MapGet(ManagementAuthenticationDefaults.LoginPath, (HttpContext context, string? returnUrl) =>
            context.ChallengeAsync(
                OpenIdConnectDefaults.AuthenticationScheme,
                new AuthenticationProperties
                {
                    RedirectUri = IsLocalReturnUrl(returnUrl) ? returnUrl : "/"
                }))
            .AllowAnonymous();

        app.MapPost(ManagementAuthenticationDefaults.LogoutPath, async (HttpContext context) =>
            {
                await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                await context.SignOutAsync(
                    OpenIdConnectDefaults.AuthenticationScheme,
                    new AuthenticationProperties { RedirectUri = "/" });
            })
            .RequireAuthorization();

        app.MapStaticAssets()
            .AllowAnonymous();

        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode()
            .RequireAuthorization();

        return app;
    }

    private static bool IsLocalReturnUrl(string? returnUrl)
    {
        return !string.IsNullOrWhiteSpace(returnUrl)
               && returnUrl.StartsWith('/')
               && !returnUrl.StartsWith("//", StringComparison.Ordinal)
               && !returnUrl.StartsWith("/\\", StringComparison.Ordinal);
    }
}
