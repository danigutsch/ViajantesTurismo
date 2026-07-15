using System.Net;
using Microsoft.AspNetCore.TestHost;

namespace ViajantesTurismo.Management.WebTests;

/// <summary>
/// Verifies cookie deletion remains available when server-side ticket cleanup fails.
/// </summary>
[Trait(SharedKernel.Testing.TestTraitNames.CategoryName, TestTraits.SecurityCategory)]
[Trait(SharedKernel.Testing.TestTraitNames.ScopeName, TestTraits.UnitScope)]
public sealed class ProtectedDistributedTicketStoreCookieHandlerTests
{
    [Fact]
    public async Task Signing_out_deletes_the_cookie_when_ticket_cache_removal_fails()
    {
        // Arrange
        using var testHost = await ProtectedDistributedTicketStoreCookieHandlerTestHost.StartWithFailingTicketRemoval(
            Xunit.TestContext.Current.CancellationToken);
        using var client = testHost.Host.GetTestClient();
        using var signInResponse = await client.GetAsync(
            new Uri("/sign-in", UriKind.Relative),
            Xunit.TestContext.Current.CancellationToken);
        var sessionCookie = signInResponse.Headers.GetValues("Set-Cookie").Single().Split(';', 2)[0];
        using var signOutRequest = new HttpRequestMessage(HttpMethod.Post, new Uri("/sign-out", UriKind.Relative));
        signOutRequest.Headers.Add("Cookie", sessionCookie);

        // Act
        using var signOutResponse = await client.SendAsync(signOutRequest, Xunit.TestContext.Current.CancellationToken);
        var deletedCookie = signOutResponse.Headers.GetValues("Set-Cookie").Single();

        // Assert
        signOutResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        deletedCookie.ShouldStartWith($"{ProtectedDistributedTicketStoreCookieHandlerTestHost.CookieName}=", StringComparison.Ordinal);
        deletedCookie.ShouldContain("expires=", StringComparison.OrdinalIgnoreCase);
        testHost.Cache.RemoveCalls.ShouldBe(2);
    }
}
