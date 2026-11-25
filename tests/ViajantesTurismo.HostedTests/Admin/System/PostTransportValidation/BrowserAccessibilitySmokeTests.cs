using Microsoft.Playwright;
using SharedKernel.Testing;
using ViajantesTurismo.Catalog.Contracts.Http;
using ViajantesTurismo.HostedTests.Admin.System.Infrastructure.Bases;
using ViajantesTurismo.HostedTests.Admin.System.Infrastructure.Fixtures;
using ViajantesTurismo.HostedTests.Admin.System.Infrastructure.Helpers;

namespace ViajantesTurismo.HostedTests.Admin.System.PostTransportValidation;

/// <summary>Audits representative routes through the existing browser host with synthetic data.</summary>
[Trait(TestTraitNames.AreaName, TestAreas.Shared)]
[TestHost(TestHost.Aspire)]
[TestScope(TestScope.System)]
public sealed class BrowserAccessibilitySmokeTests(AspireSystemTestFixture fixture)
    : AspireSystemTestBase<AspireSystemTestFixture>(fixture)
{
    private const string EngineSetup = "Set VT_WCAG_ENGINE_PATH as documented in docs/testing/WEB_QUALITY.md.";

    [Fact(SkipType = typeof(BrowserAccessibilitySmokeAsset), SkipUnless = nameof(BrowserAccessibilitySmokeAsset.IsConfigured), Skip = EngineSetup)]
    public async Task Public_tour_list_with_a_published_tour_meets_the_automated_WCAG_smoke_check()
    {
        // Arrange
        const string route = "/en/group-bike-tours";
        var cancellationToken = TestContext.Current.CancellationToken;
        using var catalogClient = await Fixture.CreateCatalogApiClient(cancellationToken);
        var scenario = new PostTransportValidationScenario(ApiClient, new CatalogToursApiClient(catalogClient));
        var unique = Guid.CreateVersion7().ToString("N")[..12];
        var title = $"Accessibility tour {unique}";
        var identifier = $"A11Y-{unique}";
        var adminTour = await scenario.CreateAdminTour(identifier, title);
        var catalogTour = await scenario.WaitForCatalogTour(adminTour.Id, identifier, cancellationToken);
        await scenario.PublishCatalogTour(catalogTour, title, $"accessibility-tour-{unique}", cancellationToken);

        // Act
        await NavigateTo(new Uri(Fixture.PublicWebAppUrl, route));

        // Assert
        await Expect(Page.GetByRole(AriaRole.Link, new PageGetByRoleOptions { Name = title, Exact = true })).ToBeVisibleAsync();
        await BrowserAccessibilitySmokeAsset.Verify(Page, route);
    }

    [Fact(SkipType = typeof(BrowserAccessibilitySmokeAsset), SkipUnless = nameof(BrowserAccessibilitySmokeAsset.IsConfigured), Skip = EngineSetup)]
    public async Task Authenticated_management_customer_form_meets_the_automated_WCAG_smoke_check()
    {
        // Arrange
        const string route = "/customers/create/personal-info";
        var formHeading = Page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions { Name = "Create New Customer", Level = 1 });

        // Act
        await NavigateTo(route);

        // Assert
        await Expect(formHeading).ToBeVisibleAsync();
        await BrowserAccessibilitySmokeAsset.Verify(Page, route);
    }

    [Fact(SkipType = typeof(BrowserAccessibilitySmokeAsset), SkipUnless = nameof(BrowserAccessibilitySmokeAsset.IsConfigured), Skip = EngineSetup)]
    public async Task Synthetic_image_without_alt_reports_the_WCAG_route_and_rule()
    {
        // Arrange
        const string route = "/testing/wcag/missing-alt";
        await Page.GotoAsync("about:blank");
        await Page.SetContentAsync("""
            <!doctype html>
            <html lang="en">
            <head><meta charset="utf-8"><title>Gallery example</title></head>
            <body>
                <main>
                    <h1>Gallery example</h1>
                    <img width="200" height="200"
                         src="data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='200' height='200'%3E%3Crect width='200' height='200' fill='navy'/%3E%3C/svg%3E">
                </main>
            </body>
            </html>
            """);

        // Act
        Func<Task> audit = () => BrowserAccessibilitySmokeAsset.Verify(Page, route);
        var failure = await audit.ShouldThrow<PlaywrightException>();

        // Assert
        failure.Message.ShouldStartWith($"WCAG A/AA smoke scan for {route}: ", StringComparison.Ordinal);
        failure.Message.ShouldEndWith(": img_alt_valid.", StringComparison.Ordinal);
    }
}
