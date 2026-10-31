using PandaBear.Testing;
using Microsoft.Playwright;
using ViajantesTurismo.HostedTests.Admin.System.Infrastructure.Bases;
using ViajantesTurismo.HostedTests.Admin.System.Infrastructure.Fixtures;
using ViajantesTurismo.Admin.Testing.Integration.Helpers;
using System.Net;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using ViajantesTurismo.HostedTests.Admin.System.Infrastructure;
using ViajantesTurismo.Resources;

namespace ViajantesTurismo.HostedTests.Admin.System.Authentication;

[Trait(TestTraitNames.CategoryName, TestTraits.AuthenticationCategory)]
[Trait(TestTraitNames.HostName, TestTraits.AspireHost)]
[TestScope(TestScope.System)]
public sealed partial class ManagementBffConformanceTests(AspireSystemTestFixture fixture)
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
        var actualValue = await Page.Locator("#username").CountAsync();
        actualValue.ShouldBe(1);

        // Act
        await ManagementLogin.SignIn(Fixture.WebAppUrl, Fixture.ConformanceUserPassword);
        await NavigateTo("/bookings");
        var cookies = await Context.CookiesAsync();
        var browserStorage = await Page.EvaluateAsync<string>(
            "() => JSON.stringify({ localStorage: Object.fromEntries(Object.entries(localStorage)), sessionStorage: Object.fromEntries(Object.entries(sessionStorage)) })");
        var finalUrl = Page.Url;

        // Assert
        var sessionCookie = cookies.ShouldHaveSingleItem(cookie => string.Equals(cookie.Name, "__Host-viajantes-management", StringComparison.Ordinal));
        sessionCookie.HttpOnly.ShouldBeTrue();
        sessionCookie.Secure.ShouldBeTrue();
        sessionCookie.SameSite.ShouldBe(SameSiteAttribute.Lax);
        sessionCookie.Path.ShouldBe("/");
        var scriptReadableCookies = cookies.Where(cookie => !cookie.HttpOnly)
            .Select(cookie => $"{cookie.Name}={cookie.Value}");
        var cookieLeaksJwt = JwtShape().IsMatch(string.Join(',', scriptReadableCookies));
        cookieLeaksJwt.ShouldBeFalse();
        browserStorage.ShouldNotContain("access_token", StringComparison.OrdinalIgnoreCase);
        browserStorage.ShouldNotContain("refresh_token", StringComparison.OrdinalIgnoreCase);
        var storageLeaksJwt = JwtShape().IsMatch(browserStorage);
        storageLeaksJwt.ShouldBeFalse();
        finalUrl.ShouldNotContain("access_token", StringComparison.OrdinalIgnoreCase);
        var finalUri = new Uri(finalUrl);
        finalUri.Host.ShouldBe(Fixture.WebAppUrl.Host);
        finalUri.AbsolutePath.ShouldBe("/bookings");
        var actualValue1 = await Page.Locator("h1").InnerTextAsync();
        actualValue1.ShouldContain("All Bookings", StringComparison.Ordinal);
    }

    [Fact]
    public async Task Uses_fixed_backend_audiences_for_management_requests()
    {
        // Arrange
        await ManagementLogin.SignIn(Fixture.WebAppUrl, Fixture.ConformanceUserPassword);

        // Act
        await NavigateTo("/bookings");
        await Page.WaitForFunctionAsync(
            "() => document.querySelector('[role=\"alert\"]')?.textContent?.includes('No bookings found.') || document.querySelector('table') !== null");
        var emptyBookingsCount = await Page.GetByText("No bookings found.", new PageGetByTextOptions { Exact = true }).CountAsync();
        var bookingsTableCount = await Page.GetByRole(AriaRole.Table).CountAsync();

        await NavigateTo("/catalog/tours");
        var catalogLoading = Page.GetByText("Loading...", new PageGetByTextOptions { Exact = true });
        await Expect(catalogLoading).Not.ToBeVisibleAsync();
        var catalogErrorCount = await Page.GetByText(
            "Catalog tours could not be loaded. Try again later.",
            new PageGetByTextOptions { Exact = true }).CountAsync();
        var catalogStatusCount = await Page.GetByRole(AriaRole.Status).CountAsync();
        var catalogTableCount = await Page.GetByRole(AriaRole.Table).CountAsync();

        await NavigateTo("/branding");
        var brandingNameInput = Page.Locator("#branding-brand-name");
        await brandingNameInput.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
        var brandingErrorCount = await Page.GetByRole(AriaRole.Alert).CountAsync();

        // Assert
        var areEqual = emptyBookingsCount == 1 || bookingsTableCount > 0;
        areEqual.ShouldBeTrue();
        catalogErrorCount.ShouldBe(0);
        var isValid = catalogStatusCount > 0 || catalogTableCount > 0;
        isValid.ShouldBeTrue();
        brandingErrorCount.ShouldBe(0);
    }

    [Fact]
    public async Task Rejects_live_tokens_issued_for_other_backend_audiences()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var identityProviderEndpoint = Fixture.IdentityProviderEndpoint;
        var adminAccessToken = await KeycloakConformanceClient.RequestAccessToken(
            identityProviderEndpoint,
            Fixture.ConformanceUserPassword,
            [ApiAudienceNames.Admin],
            ct, realm: Fixture.IdentityProviderRealm);
        var catalogAccessToken = await KeycloakConformanceClient.RequestAccessToken(
            identityProviderEndpoint,
            Fixture.ConformanceUserPassword,
            [ApiAudienceNames.Catalog],
            ct, realm: Fixture.IdentityProviderRealm);
        var brandingAccessToken = await KeycloakConformanceClient.RequestAccessToken(
            identityProviderEndpoint,
            Fixture.ConformanceUserPassword,
            [ApiAudienceNames.Branding],
            ct, realm: Fixture.IdentityProviderRealm);
        using var adminApiClient = Fixture.CreateResourceClient(ResourceNames.Api);
        using var catalogApiClient = Fixture.CreateResourceClient(ResourceNames.CatalogApi);
        using var adminRequest = new HttpRequestMessage(HttpMethod.Get, "/api/v1/bookings/");
        using var catalogRequest = new HttpRequestMessage(HttpMethod.Get, "/api/v1/catalog/tours");
        using var brandingRequest = new HttpRequestMessage(HttpMethod.Get, "/api/v1/branding/settings");
        adminRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", catalogAccessToken);
        catalogRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", brandingAccessToken);
        brandingRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminAccessToken);

        // Act
        using var adminResponse = await adminApiClient.SendAsync(adminRequest, ct);
        using var catalogResponse = await catalogApiClient.SendAsync(catalogRequest, ct);
        // Branding routes are hosted by Catalog and accept only Branding-audience tokens.
        using var brandingResponse = await catalogApiClient.SendAsync(brandingRequest, ct);

        // Assert
        adminResponse.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        catalogResponse.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        brandingResponse.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Preserves_the_opaque_subject_through_bff_token_exchange_to_document_audit()
    {
        // Arrange
        var documentId = Guid.CreateVersion7();

        // Act
        await ManagementLogin.SignIn(Fixture.WebAppUrl, Fixture.ConformanceUserPassword);
        await NavigateTo($"/documents/{documentId}");
        var expectedUrl = new Uri(Fixture.WebAppUrl, $"/documents/{documentId}").ToString();
        Page.Url.ShouldBe(expectedUrl);
        var missingDocumentAlert = Page.GetByRole(AriaRole.Alert);
        await Expect(missingDocumentAlert).ToContainTextAsync("Document not found.");
        var actorId = await Fixture.GetRejectedDocumentReadAuditActor(documentId, TestContext.Current.CancellationToken);

        // Assert
        actorId.ShouldBe(Fixture.ConformanceUserId);
    }

    // A bare "eyJ" also appears in random base64 values such as Keycloak's KC_AUTH_SESSION_HASH, so match the JWT shape.
    [GeneratedRegex(@"eyJ[\w-]+\.[\w-]+\.[\w-]*", RegexOptions.None, matchTimeoutMilliseconds: 1000)]
    private static partial Regex JwtShape();
}
