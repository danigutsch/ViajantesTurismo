using Microsoft.Playwright;
using ViajantesTurismo.HostedTests.Admin.System.Infrastructure.Helpers;
using PandaBear.Testing.Playwright;
using System.Globalization;
using static Microsoft.Playwright.Assertions;

namespace ViajantesTurismo.HostedTests.Admin.System.Tours;

public static class CapacityIndicatorTestHelpers
{
    public static async Task UpdateCapacity(IPage page, int minCustomers, int maxCustomers)
    {
        ArgumentNullException.ThrowIfNull(page);

        await page.GetLink("Edit Tour").ClickAsync();
        await page.ShouldHaveTitle("Edit Tour");

        var maximumCustomers = page.GetByLabel("Maximum Customers");
        var maximumCustomersText = maxCustomers.ToString(CultureInfo.InvariantCulture);
        await maximumCustomers.FillAsync(maximumCustomersText);
        await maximumCustomers.BlurAsync();
        await Expect(maximumCustomers).ToHaveValueAsync(maximumCustomersText);

        var minimumCustomers = page.GetByLabel("Minimum Customers");
        var minimumCustomersText = minCustomers.ToString(CultureInfo.InvariantCulture);
        await minimumCustomers.FillAsync(minimumCustomersText);
        await minimumCustomers.BlurAsync();
        await Expect(minimumCustomers).ToHaveValueAsync(minimumCustomersText);

        await page.GetButton("Update Tour").ClickAsync();

        var editSuccess = page.Locator(".alert-success");
        await Expect(editSuccess).ToBeVisibleAsync();
        await Expect(editSuccess).ToContainTextAsync("Tour updated successfully!");

    }

    public static async Task ExpectCapacitySummary(IPage page, string expectedText)
    {
        ArgumentNullException.ThrowIfNull(page);

        var capacitySection = page.Locator("h5:has-text('Capacity') + dl");
        await Expect(capacitySection.GetByText(expectedText)).ToBeVisibleAsync();
    }

    public static async Task ExpectCapacityStateOnDetails(
        IPage page,
        Func<Task> navigateToDetails,
        string tourName,
        CapacityStateExpectation expectation)
    {
        ArgumentNullException.ThrowIfNull(page);
        ArgumentNullException.ThrowIfNull(navigateToDetails);
        ArgumentNullException.ThrowIfNull(expectation);

        await navigateToDetails();
        await Expect(page.GetHeading(tourName)).ToBeVisibleAsync();
        var capacitySection = page.Locator("h5:has-text('Capacity') + dl");
        await Expect(capacitySection.GetByText(
                expectation.DetailsBadgeText,
                new LocatorGetByTextOptions { Exact = true }))
            .ToBeVisibleAsync();
        await Expect(capacitySection.GetByText(expectation.DetailsCapacityText))
            .ToBeVisibleAsync();
    }
}
