using System.Net;
using System.Net.Http.Headers;
using ViajantesTurismo.Admin.SystemTests.Infrastructure;
using ViajantesTurismo.Resources;

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
        (emptyBookingsCount == 1 || bookingsTableCount > 0).ShouldBeTrue();
        catalogErrorCount.ShouldBe(0);
        (catalogStatusCount > 0 || catalogTableCount > 0).ShouldBeTrue();
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
            ct);
        var catalogAccessToken = await KeycloakConformanceClient.RequestAccessToken(
            identityProviderEndpoint,
            Fixture.ConformanceUserPassword,
            [ApiAudienceNames.Catalog],
            ct);
        var brandingAccessToken = await KeycloakConformanceClient.RequestAccessToken(
            identityProviderEndpoint,
            Fixture.ConformanceUserPassword,
            [ApiAudienceNames.Branding],
            ct);
        using var adminApiClient = Fixture.CreateResourceClient(ResourceNames.Api);
        using var catalogApiClient = Fixture.CreateResourceClient(ResourceNames.CatalogApi);
        using var brandingApiClient = Fixture.CreateResourceClient(ResourceNames.BrandingApi);
        using var adminRequest = new HttpRequestMessage(HttpMethod.Get, "/api/v1/bookings/");
        using var catalogRequest = new HttpRequestMessage(HttpMethod.Get, "/api/v1/catalog/tours");
        using var brandingRequest = new HttpRequestMessage(HttpMethod.Get, "/api/v1/branding/settings");
        adminRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", catalogAccessToken);
        catalogRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", brandingAccessToken);
        brandingRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminAccessToken);

        // Act
        using var adminResponse = await adminApiClient.SendAsync(adminRequest, ct);
        using var catalogResponse = await catalogApiClient.SendAsync(catalogRequest, ct);
        using var brandingResponse = await brandingApiClient.SendAsync(brandingRequest, ct);

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
        actorId.ShouldBe(KeycloakConformanceClient.ConformanceUserId);
    }
}
