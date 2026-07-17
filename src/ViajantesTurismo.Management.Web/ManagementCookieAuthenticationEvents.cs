using Microsoft.AspNetCore.Authentication.Cookies;
using SharedKernel.BuildingBlocks;

namespace ViajantesTurismo.Management.Web;

/// <summary>
/// Removes the current session's protected user tokens when the BFF cookie is signed out.
/// </summary>
internal sealed partial class ManagementCookieAuthenticationEvents(
    ProtectedDistributedUserTokenStore userTokenStore,
    ProtectedDistributedAudienceTokenStore audienceTokenStore,
    ILogger<ManagementCookieAuthenticationEvents> logger)
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
        ManagementTokenSession session;
        try
        {
            session = ManagementTokenSession.From(user);
        }
        catch (InvalidOperationException)
        {
            return;
        }

        bool userTokenEntriesRemoved;
        try
        {
            userTokenEntriesRemoved = await userTokenStore.ClearAll(user, ct);
        }
        catch (Exception exception) when (exception.ShouldHandleAsFailure(ct))
        {
            LogUserTokenCleanupFailure(logger, exception.GetType().Name);
            throw;
        }

        if (!userTokenEntriesRemoved)
        {
            LogUserTokenCleanupFailure(logger, "TokenEntryRemoval");
        }

        try
        {
            await audienceTokenStore.ClearAll(session, ct);
        }
        catch (Exception exception) when (exception.ShouldHandleAsFailure(ct))
        {
            // User-token cleanup and browser cookie deletion must proceed when audience-token cleanup fails.
            LogAudienceTokenCleanupFailure(logger, exception.GetType().Name);
        }
    }

    [LoggerMessage(LogLevel.Warning, "Management audience-token sign-out cleanup failed. Failure type: {FailureType}.")]
    private static partial void LogAudienceTokenCleanupFailure(ILogger logger, string failureType);

    [LoggerMessage(LogLevel.Warning, "Management user-token sign-out cleanup failed. Failure type: {FailureType}.")]
    private static partial void LogUserTokenCleanupFailure(ILogger logger, string failureType);
}
