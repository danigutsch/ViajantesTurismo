using PandaBear.AspNetCore;
using PandaBear.HttpCaching.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using ViajantesTurismo.Management.Web.Components;

namespace ViajantesTurismo.Management.Web;

/// <summary>
/// Maps Management Web minimal API endpoints such as login, logout, robots.txt and media previews.
/// </summary>
internal static class ManagementWebEndpoints
{
    private const string ManagementRobotsTxt = "User-agent: *\nDisallow: /";

    /// <summary>
    /// Endpoint name of the media preview by rendition route.
    /// </summary>
    internal const string MediaPreviewByRenditionEndpointName = "management-media-preview-by-rendition";

    /// <summary>
    /// Maps the Management Web endpoints onto the route builder.
    /// </summary>
    internal static IEndpointRouteBuilder MapManagementWebEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapRobotsTxt(ManagementRobotsTxt);

        app.MapGet(ManagementAuthenticationDefaults.LoginPath, (HttpContext context, string? returnUrl) =>
            context.ChallengeAsync(
                OpenIdConnectDefaults.AuthenticationScheme,
                new AuthenticationProperties
                {
                    RedirectUri = IsLocalReturnUrl(returnUrl) ? returnUrl : "/"
                }))
            .AllowAnonymous();

        app.MapAntiforgeryProtectedSignOut(
            ManagementAuthenticationDefaults.LogoutPath,
            CookieAuthenticationDefaults.AuthenticationScheme,
            OpenIdConnectDefaults.AuthenticationScheme,
            "/");

        app.MapGet("/catalog/media/images/{id:guid}/preview/{width:int}/{format}", (Guid id, int width, string format, [FromServices] ICatalogToursApiClient catalogToursApi, HttpContext context, CancellationToken ct) =>
                GetMediaPreview(id, width, format, catalogToursApi, context, ct))
            .WithName(MediaPreviewByRenditionEndpointName)
            .RequireAuthorization();

        app.MapDocumentArtifactProxy();

        app.MapStaticAssets()
            .AllowAnonymous();

        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode()
            .RequireAuthorization();

        return app;
    }

    private static async Task<IResult> GetMediaPreview(
        Guid id,
        int width,
        string format,
        ICatalogToursApiClient catalogToursApi,
        HttpContext context,
        CancellationToken ct)
    {
        HttpCacheHeaders.SetNoStore(context);
        if (id == Guid.Empty)
        {
            return Results.BadRequest();
        }

        try
        {
            var media = await catalogToursApi.GetMediaPreview(id, width, format, ct).ConfigureAwait(false);
            if (media is null)
            {
                return Results.NotFound();
            }

            context.Response.Headers["X-Content-Type-Options"] = "nosniff";
            context.Response.RegisterForDisposeAsync(media);
            return Results.Stream(media.Content, media.ContentType, enableRangeProcessing: false);
        }
        catch (HttpRequestException)
        {
            return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
        }
    }

    private static bool IsLocalReturnUrl(string? returnUrl)
    {
        return !string.IsNullOrWhiteSpace(returnUrl)
               && returnUrl.StartsWith('/')
               && RedirectHttpResult.IsLocalUrl(returnUrl);
    }
}
