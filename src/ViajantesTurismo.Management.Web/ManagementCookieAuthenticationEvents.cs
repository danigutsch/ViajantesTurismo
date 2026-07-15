using Microsoft.AspNetCore.Authentication.Cookies;

namespace ViajantesTurismo.Management.Web;

/// <summary>
/// Removes the current session's protected user tokens when the BFF cookie is signed out.
/// </summary>
internal sealed class ManagementCookieAuthenticationEvents(
    ProtectedDistributedUserTokenStore userTokenStore,
    ProtectedDistributedAudienceTokenStore audienceTokenStore)
    : CookieAuthenticationEvents
{
    public override async Task SigningOut(CookieSigningOutContext context)
    {
        var user = context.HttpContext.User;
        if (user.Identity?.IsAuthenticated is not true)
        {
            return;
        }

        var ct = context.HttpContext.RequestAborted;
        try
        {
            var sourceAccessToken = await userTokenStore.GetSourceAccessToken(user, ct);
            if (!string.IsNullOrWhiteSpace(sourceAccessToken))
            {
                await audienceTokenStore.ClearAll(sourceAccessToken, ct);
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException || !ct.IsCancellationRequested)
        {
            // User-token cleanup and browser cookie deletion must proceed when audience-token cleanup fails.
        }

        try
        {
            await userTokenStore.ClearAll(user, ct);
        }
        catch (Exception exception) when (exception is not OperationCanceledException || !ct.IsCancellationRequested)
        {
            // Browser cookie deletion must proceed even when protected user-token cleanup fails.
        }
    }
}
