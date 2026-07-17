using System.Security.Claims;
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
        var context = ManagementOpenIdConnectEventsTestContext.Create(principal);

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

    [Theory]
    [InlineData(null, "refresh-token", "Bearer", "300", "web-app")]
    [InlineData("access-token", null, "Bearer", "300", "web-app")]
    [InlineData("access-token", "refresh-token", null, "300", "web-app")]
    [InlineData("access-token", "refresh-token", "Bearer", null, "web-app")]
    [InlineData("access-token", "refresh-token", "Bearer", "invalid", "web-app")]
    [InlineData("access-token", "refresh-token", "Bearer", "0", "web-app")]
    [InlineData("access-token", "refresh-token", "Bearer", "-1", "web-app")]
    [InlineData("access-token", "refresh-token", "Bearer", "300", null)]
    public async Task Rejects_invalid_token_responses_without_storing_tokens(
        string? accessToken,
        string? refreshToken,
        string? tokenType,
        string? expiresIn,
        string? clientId)
    {
        // Arrange
        var store = new RecordingUserTokenStore();
        var events = new ManagementOpenIdConnectEvents(store, TimeProvider.System);
        var context = ManagementOpenIdConnectEventsTestContext.Create(
            accessToken: accessToken,
            refreshToken: refreshToken,
            tokenType: tokenType,
            expiresIn: expiresIn,
            clientId: clientId);
        Func<Task> validateToken = () => events.TokenValidated(context);

        // Act
        var exception = await validateToken.ShouldThrow<InvalidOperationException>();

        // Assert
        exception.Message.ShouldBe("The identity provider did not return a valid token response.");
        store.StoredUser.ShouldBeNull();
    }

    [Fact]
    public async Task Rejects_a_missing_token_response_without_storing_tokens()
    {
        // Arrange
        var store = new RecordingUserTokenStore();
        var events = new ManagementOpenIdConnectEvents(store, TimeProvider.System);
        var context = ManagementOpenIdConnectEventsTestContext.CreateWithoutTokenResponse();
        Func<Task> validateToken = () => events.TokenValidated(context);

        // Act
        var exception = await validateToken.ShouldThrow<InvalidOperationException>();

        // Assert
        exception.Message.ShouldBe("The identity provider did not return a token response.");
        store.StoredUser.ShouldBeNull();
    }
}
