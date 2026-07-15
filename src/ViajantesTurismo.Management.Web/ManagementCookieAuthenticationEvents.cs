using Microsoft.AspNetCore.Authentication.Cookies;

namespace ViajantesTurismo.Management.Web;

/// <summary>
/// Removes the current session's protected user tokens when the BFF cookie is signed out.
/// </summary>
internal sealed class ManagementCookieAuthenticationEvents(ProtectedDistributedUserTokenStore userTokenStore)
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
            await userTokenStore.ClearAll(user, ct);
        }
        catch (Exception exception) when (exception is not OperationCanceledException || !ct.IsCancellationRequested)
        {
            // Cookie deletion must proceed even when best-effort token cleanup fails.
        }
    }
}
