using ViajantesTurismo.Admin.SystemTests.Infrastructure;

namespace ViajantesTurismo.Admin.SystemTests.Authentication;

[Trait(SharedKernel.Testing.TestTraitNames.CategoryName, TestTraits.AuthenticationCategory)]
[Trait(SharedKernel.Testing.TestTraitNames.ScopeName, TestTraits.SystemScope)]
[Trait(SharedKernel.Testing.TestTraitNames.HostName, TestTraits.AspireHost)]
public sealed class ManagementBffConformanceTests(AspireSystemTestFixture fixture)
    : AspireSystemTestBase<AspireSystemTestFixture>(fixture)
{
    protected override bool AutomaticallySignIn => false;

    [Fact]
    public async Task Challenges_anonymous_requests_and_keeps_tokens_out_of_the_browser()
    {
        // Arrange
        var bookingsUri = new Uri(Fixture.WebAppUrl, "/bookings");

        // Act
        await Page.GotoAsync(bookingsUri.ToString(), new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });

        // Assert
        (await Page.Locator("#username").CountAsync()).ShouldBe(1);

        // Act
        await ManagementLogin.SignIn(Fixture.WebAppUrl, Fixture.ConformanceUserPassword);
        await NavigateTo("/bookings");
        var cookies = await Context.CookiesAsync();
        var browserStorage = await Page.EvaluateAsync<string>(
            "() => JSON.stringify({ localStorage: Object.fromEntries(Object.entries(localStorage)), sessionStorage: Object.fromEntries(Object.entries(sessionStorage)) })");
        var finalUrl = Page.Url;

        // Assert
        var sessionCookie = cookies.Single(cookie => string.Equals(cookie.Name, "__Host-viajantes-management", StringComparison.Ordinal));
        sessionCookie.HttpOnly.ShouldBeTrue();
        sessionCookie.Secure.ShouldBeTrue();
        sessionCookie.SameSite.ShouldBe(SameSiteAttribute.Lax);
        sessionCookie.Path.ShouldBe("/");
        var scriptReadableCookies = cookies.Where(cookie => !cookie.HttpOnly)
            .Select(cookie => $"{cookie.Name}={cookie.Value}");
        string.Join(',', scriptReadableCookies).ShouldNotContain("eyJ", StringComparison.Ordinal);
        browserStorage.ShouldNotContain("access_token", StringComparison.OrdinalIgnoreCase);
        browserStorage.ShouldNotContain("refresh_token", StringComparison.OrdinalIgnoreCase);
        browserStorage.ShouldNotContain("eyJ", StringComparison.Ordinal);
        finalUrl.ShouldNotContain("access_token", StringComparison.OrdinalIgnoreCase);
        var finalUri = new Uri(finalUrl);
        finalUri.Host.ShouldBe(Fixture.WebAppUrl.Host);
        finalUri.AbsolutePath.ShouldBe("/bookings");
        (await Page.Locator("h1").InnerTextAsync()).ShouldContain("All Bookings", StringComparison.Ordinal);
    }
}
