using System.Globalization;
using System.Security.Claims;

namespace ViajantesTurismo.Management.Web;

/// <summary>
/// Identifies one authenticated Management BFF login session and its absolute expiry.
/// </summary>
internal readonly record struct ManagementTokenSession(string Id, DateTimeOffset ExpiresAt)
{
    /// <summary>
    /// Gets the opaque session identity from an authenticated principal.
    /// </summary>
    /// <param name="user">The authenticated Management user.</param>
    /// <returns>The current token session.</returns>
    /// <exception cref="InvalidOperationException">The user has no valid token session.</exception>
    internal static ManagementTokenSession From(ClaimsPrincipal user)
    {
        ArgumentNullException.ThrowIfNull(user);

        var sessionId = user.FindFirst(ManagementAuthenticationDefaults.UserTokenStoreSessionIdClaimType)?.Value;
        var expiresAtValue = user.FindFirst(ManagementAuthenticationDefaults.UserTokenStoreSessionExpiresAtClaimType)?.Value;
        if (string.IsNullOrWhiteSpace(sessionId)
            || !DateTimeOffset.TryParse(
                expiresAtValue,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out var expiresAt))
        {
            throw new InvalidOperationException("The management token session is unavailable.");
        }

        return new ManagementTokenSession(sessionId, expiresAt);
    }

    /// <summary>
    /// Throws when the session is no longer active.
    /// </summary>
    /// <param name="timeProvider">The time source used to evaluate expiry.</param>
    internal void EnsureActive(TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);

        if (ExpiresAt <= timeProvider.GetUtcNow())
        {
            throw new InvalidOperationException("The management token session has expired.");
        }
    }
}
