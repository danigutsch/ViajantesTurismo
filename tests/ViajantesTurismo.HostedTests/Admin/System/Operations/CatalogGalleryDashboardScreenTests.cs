using System.Globalization;
using Microsoft.Playwright;
using Microsoft.Playwright.Xunit.v3;
using SharedKernel.Testing;
using SharedKernel.Testing.Playwright;
using ViajantesTurismo.HostedTests.Admin.System.Infrastructure.Fixtures;
using ViajantesTurismo.HostedTests.Admin.System.Infrastructure.Workflows;

namespace ViajantesTurismo.HostedTests.Admin.System.Operations;

/// <summary>Verifies the authenticated Management view of repeated Gallery wake retries.</summary>
[TestScope(TestScope.System)]
[TestHost(TestHost.Browser)]
public sealed class CatalogGalleryDashboardScreenTests(AspireSystemTestFixture fixture)
    : BrowserTest, IClassFixture<AspireSystemTestFixture>
{
    [Fact]
    public async Task Admin_reads_the_count_and_oldest_due_retry_for_pending_gallery_placements()
    {
        // Arrange
        var ct = TestContext.Current.CancellationToken;
        var scenario = await CatalogGalleryDashboardScenario.Create(fixture, ct);
        var oldestDueAt = scenario.OldestDueAt.ToString("O", CultureInfo.InvariantCulture);
        Assertions.SetDefaultExpectTimeout(30000);
        await using var context = await Browser.NewContextAsync(new BrowserNewContextOptions { IgnoreHTTPSErrors = true });
        var page = await context.NewPageAsync();
        var login = new ManagementLoginWorkflow(page);
        await login.SignIn(scenario.ManagementUrl, fixture.ConformanceUserPassword);
        await using var fence = await scenario.HoldDueWakes(ct);

        // Act
        await page.GotoInteractive(new Uri(scenario.ManagementUrl, "/operations"));

        // Assert
        var card = page.GetByRole(AriaRole.Region, new PageGetByRoleOptions { Name = "Stuck sagas" });
        await Expect(card.GetByText("2", new LocatorGetByTextOptions { Exact = true })).ToBeVisibleAsync();
        await Expect(card.Locator("time")).ToHaveAttributeAsync("datetime", oldestDueAt);
        await Expect(card.GetByText("Not configured", new LocatorGetByTextOptions { Exact = true })).ToBeVisibleAsync();
        var lookup = card.GetByRole(AriaRole.Link, new LocatorGetByRoleOptions { Name = "Find a Gallery placement" });
        await Expect(lookup).ToHaveAttributeAsync("href", "operations/gallery");
    }
}
