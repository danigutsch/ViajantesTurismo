using System.Security.Claims;
using ViajantesTurismo.Management.Web;

namespace ViajantesTurismo.Management.WebTests;

[Trait(SharedKernel.Testing.TestTraitNames.CategoryName, TestTraits.SecurityCategory)]
[Trait(SharedKernel.Testing.TestTraitNames.ScopeName, TestTraits.UnitScope)]
public sealed class ManagementTokenSessionTests
{
    [Fact]
    public void From_returns_the_claimed_session()
    {
        // Arrange
        var expiresAt = new DateTimeOffset(2026, 7, 17, 12, 0, 0, TimeSpan.Zero);
        var user = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ManagementAuthenticationDefaults.UserTokenStoreSessionIdClaimType, "session-1"),
            new Claim(ManagementAuthenticationDefaults.UserTokenStoreSessionExpiresAtClaimType, expiresAt.ToString("O"))
        ], "test"));

        // Act
        var session = ManagementTokenSession.From(user);

        // Assert
        session.Id.ShouldBe("session-1");
        session.ExpiresAt.ShouldBe(expiresAt);
    }

    [Fact]
    public void From_rejects_missing_blank_or_malformed_session_claims()
    {
        // Arrange
        ClaimsPrincipal[] users =
        [
            new(new ClaimsIdentity("test")),
            new(new ClaimsIdentity(
            [
                new Claim(ManagementAuthenticationDefaults.UserTokenStoreSessionIdClaimType, " "),
                new Claim(ManagementAuthenticationDefaults.UserTokenStoreSessionExpiresAtClaimType, "2026-07-17T12:00:00.0000000+00:00")
            ], "test")),
            new(new ClaimsIdentity(
            [
                new Claim(ManagementAuthenticationDefaults.UserTokenStoreSessionIdClaimType, "session-1"),
                new Claim(ManagementAuthenticationDefaults.UserTokenStoreSessionExpiresAtClaimType, "not-a-date")
            ], "test"))
        ];

        foreach (var user in users)
        {
            // Act
            Action act = () => ManagementTokenSession.From(user);

            // Assert
            var exception = act.ShouldThrow<InvalidOperationException>();
            exception.Message.ShouldBe("The management token session is unavailable.");
        }
    }

    [Fact]
    public void EnsureActive_rejects_expired_sessions()
    {
        // Arrange
        var session = new ManagementTokenSession("session-1", DateTimeOffset.UtcNow);

        // Act
        Action act = () => session.EnsureActive(TimeProvider.System);

        // Assert
        var exception = act.ShouldThrow<InvalidOperationException>();
        exception.Message.ShouldBe("The management token session has expired.");
    }
}
