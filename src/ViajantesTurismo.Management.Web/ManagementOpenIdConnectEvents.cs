using System.Globalization;
using System.Security.Claims;
using System.Security.Cryptography;
using Duende.AccessTokenManagement;
using Duende.AccessTokenManagement.OpenIdConnect;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.WebUtilities;

namespace ViajantesTurismo.Management.Web;

/// <summary>
/// Creates the protected server-side token session after a successful OIDC code exchange.
/// </summary>
internal sealed class ManagementOpenIdConnectEvents(
    IUserTokenStore userTokenStore,
    TimeProvider timeProvider) : OpenIdConnectEvents
{
    public override async Task TokenValidated(TokenValidatedContext context)
    {
        ArgumentNullException.ThrowIfNull(context.Principal);

        var tokenResponse = context.TokenEndpointResponse
            ?? throw new InvalidOperationException("The identity provider did not return a token response.");
        var accessToken = tokenResponse.AccessToken;
        var refreshToken = tokenResponse.RefreshToken;
        var tokenType = tokenResponse.TokenType;
        var clientId = context.Options.ClientId;
        if (string.IsNullOrWhiteSpace(accessToken)
            || string.IsNullOrWhiteSpace(refreshToken)
            || string.IsNullOrWhiteSpace(tokenType)
            || string.IsNullOrWhiteSpace(clientId)
            || !int.TryParse(tokenResponse.ExpiresIn, NumberStyles.None, CultureInfo.InvariantCulture, out var expiresIn)
            || expiresIn <= 0)
        {
            throw new InvalidOperationException("The identity provider did not return a valid token response.");
        }

        var now = timeProvider.GetUtcNow();
        DateTimeOffset tokenExpiresAt;
        try
        {
            tokenExpiresAt = now.AddSeconds(expiresIn);
        }
        catch (ArgumentOutOfRangeException)
        {
            throw new InvalidOperationException("The identity provider returned an invalid token lifetime.");
        }

        RemoveSessionClaims(context.Principal, ManagementAuthenticationDefaults.UserTokenStoreSessionIdClaimType);
        RemoveSessionClaims(context.Principal, ManagementAuthenticationDefaults.UserTokenStoreSessionExpiresAtClaimType);
        var sessionIdentity = new ClaimsIdentity();
        sessionIdentity.AddClaim(new Claim(
            ManagementAuthenticationDefaults.UserTokenStoreSessionIdClaimType,
            WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32))));
        sessionIdentity.AddClaim(new Claim(
            ManagementAuthenticationDefaults.UserTokenStoreSessionExpiresAtClaimType,
            now.Add(ManagementAuthenticationDefaults.SessionLifetime).ToString("O", CultureInfo.InvariantCulture)));
        context.Principal.AddIdentity(sessionIdentity);

        var token = new UserToken
        {
            AccessToken = AccessToken.Parse(accessToken),
            AccessTokenType = AccessTokenType.Parse(tokenType),
            ClientId = ClientId.Parse(clientId),
            Expiration = tokenExpiresAt,
            IdentityToken = string.IsNullOrWhiteSpace(tokenResponse.IdToken) ? null : IdentityToken.Parse(tokenResponse.IdToken),
            RefreshToken = RefreshToken.Parse(refreshToken),
            Scope = string.IsNullOrWhiteSpace(tokenResponse.Scope) ? null : Scope.Parse(tokenResponse.Scope)
        };

        await userTokenStore.StoreTokenAsync(context.Principal, token, ct: context.HttpContext.RequestAborted);
    }

    private static void RemoveSessionClaims(ClaimsPrincipal principal, string claimType)
    {
        foreach (var identity in principal.Identities)
        {
            foreach (var claim in identity.FindAll(claimType).ToArray())
            {
                identity.RemoveClaim(claim);
            }
        }
    }
}
