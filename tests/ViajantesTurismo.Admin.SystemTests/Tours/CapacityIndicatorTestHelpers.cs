using System.Globalization;
using static Microsoft.Playwright.Assertions;
namespace ViajantesTurismo.Admin.SystemTests.Tours;

public static class CapacityIndicatorTestHelpers
{
    public static async Task UpdateCapacity(IPage page, int minCustomers, int maxCustomers)
    {
        ArgumentNullException.ThrowIfNull(page);

        await page.GetLink("Edit Tour").ClickAsync();
        await Expect(page).ToHaveTitleAsync("Edit Tour");

        await page.Locator("#minCustomers").FillAsync(minCustomers.ToString(CultureInfo.InvariantCulture));
        await page.Locator("#maxCustomers").FillAsync(maxCustomers.ToString(CultureInfo.InvariantCulture));
        await page.GetButton("Update Tour").ClickAsync();

        var editSuccess = page.Locator(".alert-success");
        await Expect(editSuccess).ToBeVisibleAsync();
        await Expect(editSuccess).ToContainTextAsync("Tour updated successfully!");

        await page.CancelTimedRedirect();
    }

    public static async Task ExpectCapacitySummary(IPage page, string expectedText)
    {
        ArgumentNullException.ThrowIfNull(page);

        var capacitySection = page.Locator("h5:has-text('Capacity') + dl");
        await Expect(capacitySection.GetByText(expectedText)).ToBeVisibleAsync();
    }

    public static async Task ExpectCapacityStateOnListAndDetails(
        IPage page,
        Func<Guid, Task<ILocator>> getTourRow,
        Guid tourId,
        string tourName,
        CapacityStateExpectation expectation)
    {
        ArgumentNullException.ThrowIfNull(page);
        ArgumentNullException.ThrowIfNull(getTourRow);
        ArgumentNullException.ThrowIfNull(expectation);

        var tourRow = await getTourRow(tourId);
        await Expect(tourRow.Locator(expectation.ListBadgeSelector))
            .ToContainTextAsync(expectation.ListBadgeText);
        await Expect(tourRow.Locator("span.text-nowrap"))
            .ToHaveTextAsync(expectation.ListCapacityText);

        await tourRow.GetLink("View").ClickAsync();
        await Expect(page.GetHeading(tourName)).ToBeVisibleAsync();
        await Expect(page.Locator("h5:has-text('Capacity') + dl").Locator(expectation.DetailsBadgeSelector))
            .ToContainTextAsync(expectation.DetailsBadgeText);
    }
}
