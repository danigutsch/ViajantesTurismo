using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using ViajantesTurismo.Management.Web;

namespace ViajantesTurismo.Management.WebTests;

/// <summary>
/// Verifies OIDC sign-in creates trusted token-session claims.
/// </summary>
[Trait(SharedKernel.Testing.TestTraitNames.CategoryName, TestTraits.SecurityCategory)]
[Trait(SharedKernel.Testing.TestTraitNames.ScopeName, TestTraits.UnitScope)]
public sealed class ManagementOpenIdConnectEventsTests
{
    [Fact]
    public async Task Replaces_colliding_provider_session_claims_before_storing_tokens()
    {
        // Arrange
        var store = new RecordingUserTokenStore();
        var events = new ManagementOpenIdConnectEvents(store, TimeProvider.System);
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ManagementAuthenticationDefaults.UserTokenStoreSessionIdClaimType, "provider-session"),
            new Claim(ManagementAuthenticationDefaults.UserTokenStoreSessionExpiresAtClaimType, "2000-01-01T00:00:00.0000000+00:00")
        ],
        "provider"));
        var context = new TokenValidatedContext(
            new DefaultHttpContext(),
            new AuthenticationScheme(
                OpenIdConnectDefaults.AuthenticationScheme,
                displayName: null,
                handlerType: typeof(OpenIdConnectHandler)),
            new OpenIdConnectOptions { ClientId = "web-app" },
            principal,
            new AuthenticationProperties())
        {
            ProtocolMessage = new OpenIdConnectMessage(),
            TokenEndpointResponse = new OpenIdConnectMessage
            {
                AccessToken = "access-token",
                ExpiresIn = "300",
                RefreshToken = "refresh-token",
                TokenType = "Bearer"
            }
        };

        // Act
        await events.TokenValidated(context);
        var storedUser = store.StoredUser ?? throw new InvalidOperationException("The token store did not receive the user.");
        var sessionClaims = storedUser.FindAll(ManagementAuthenticationDefaults.UserTokenStoreSessionIdClaimType).ToArray();
        var expiryClaims = storedUser.FindAll(ManagementAuthenticationDefaults.UserTokenStoreSessionExpiresAtClaimType).ToArray();

        // Assert
        sessionClaims.Length.ShouldBe(1);
        expiryClaims.Length.ShouldBe(1);
        sessionClaims[0].Value.ShouldNotBe("provider-session");
        expiryClaims[0].Value.ShouldNotBe("2000-01-01T00:00:00.0000000+00:00");
    }
}
