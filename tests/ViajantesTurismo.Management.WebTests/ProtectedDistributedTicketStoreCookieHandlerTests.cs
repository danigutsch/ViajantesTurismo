using Microsoft.AspNetCore.TestHost;

namespace ViajantesTurismo.Management.WebTests;

/// <summary>
/// Verifies sign-out fails closed when server-side ticket revocation cannot complete.
/// </summary>
[Trait(SharedKernel.Testing.TestTraitNames.CategoryName, TestTraits.SecurityCategory)]
[Trait(SharedKernel.Testing.TestTraitNames.ScopeName, TestTraits.UnitScope)]
public sealed class ProtectedDistributedTicketStoreCookieHandlerTests
{
    [Fact]
    public async Task Signing_out_fails_when_ticket_cache_removal_fails()
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
        Func<Task> signOut = async () =>
        {
            using var response = await client.SendAsync(signOutRequest, Xunit.TestContext.Current.CancellationToken);
        };

        // Assert
        await signOut.ShouldThrow<InvalidOperationException>();
        testHost.Cache.RemoveCalls.ShouldBe(2);
    }
}
