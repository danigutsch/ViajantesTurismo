using System.Globalization;
using Microsoft.Playwright;
using Microsoft.Playwright.Xunit.v3;
using SharedKernel.Testing;
using SharedKernel.Testing.Playwright;
using ViajantesTurismo.HostedTests.Admin.System.Infrastructure.Fixtures;
using ViajantesTurismo.HostedTests.Admin.System.Infrastructure.Workflows;

namespace ViajantesTurismo.HostedTests.Admin.System.Operations;

/// <summary>Verifies the authenticated Management view of real Catalog inbox observations.</summary>
[TestScope(TestScope.System)]
[TestHost(TestHost.Browser)]
public sealed class CatalogInboxScreenTests(AspireSystemTestFixture fixture)
    : BrowserTest, IClassFixture<AspireSystemTestFixture>
{
    [Fact]
    public async Task Admin_reads_retained_duplicate_counts_and_the_latest_observation_in_management()
    {
        // Arrange
        var scenario = await CatalogInboxScreenScenario.Create(fixture, TestContext.Current.CancellationToken);
        var observedAt = scenario.LastObservedAt.ToString("u", CultureInfo.InvariantCulture);
        Assertions.SetDefaultExpectTimeout(30000);
        await using var context = await Browser.NewContextAsync(new BrowserNewContextOptions { IgnoreHTTPSErrors = true });
        var page = await context.NewPageAsync();
        var login = new ManagementLoginWorkflow(page);

        // Act
        await login.SignIn(scenario.ManagementUrl, fixture.ConformanceUserPassword);
        await page.GotoInteractive(new Uri(scenario.ManagementUrl, "/operations"));

        // Assert
        var card = page.GetByRole(AriaRole.Region, new PageGetByRoleOptions { Name = "Idempotency duplicates" });
        await Expect(card.GetByText("2", new LocatorGetByTextOptions { Exact = true })).ToBeVisibleAsync();

        // Act
        await card.GetByRole(AriaRole.Link, new LocatorGetByRoleOptions { Name = "Inspect Catalog inbox duplicates" }).ClickAsync();
        var inbox = page.GetByRole(AriaRole.Region, new PageGetByRoleOptions { Name = "Completed inbox duplicates" });

        // Assert
        await Expect(inbox.GetByText("Inbox consumer catalog", new LocatorGetByTextOptions { Exact = true })).ToBeVisibleAsync();
        await Expect(inbox.GetByText("2 duplicate deliveries across 1 retained events.")).ToBeVisibleAsync();
        await Expect(inbox.GetByText($"Latest duplicate (UTC) {observedAt}")).ToBeVisibleAsync();
        var rows = inbox.GetByRole(AriaRole.Table, new LocatorGetByRoleOptions { Name = "Completed Catalog duplicate deliveries" });
        var duplicate = rows.GetByRole(AriaRole.Row).Filter(new LocatorFilterOptions { HasText = scenario.DuplicateEventId.ToString("D") });
        await Expect(duplicate.GetByRole(AriaRole.Cell, new LocatorGetByRoleOptions { Name = "2", Exact = true })).ToBeVisibleAsync();
        await Expect(duplicate.GetByRole(AriaRole.Cell, new LocatorGetByRoleOptions { Name = observedAt, Exact = true })).ToBeVisibleAsync();

        // Act
        await inbox.GetByLabel("Completed event ID").FillAsync(scenario.SingleDeliveryEventId.ToString("D"));
        await inbox.GetByRole(AriaRole.Button, new LocatorGetByRoleOptions { Name = "Search duplicates" }).ClickAsync();

        // Assert
        await Expect(inbox.GetByRole(AriaRole.Status)).ToHaveTextAsync("No completed duplicates match this search.");
        await Expect(inbox.GetByText("Inbox consumer catalog", new LocatorGetByTextOptions { Exact = true })).ToBeVisibleAsync();
        await Expect(inbox.GetByText("2 duplicate deliveries across 1 retained events.")).ToBeVisibleAsync();
    }
}
