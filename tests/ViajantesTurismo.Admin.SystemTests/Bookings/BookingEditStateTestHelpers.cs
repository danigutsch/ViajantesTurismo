using static Microsoft.Playwright.Assertions;

namespace ViajantesTurismo.Admin.SystemTests.Bookings;

public static class BookingEditStateTestHelpers
{
    public static async Task ExpectWarningContains(IPage page, params string[] expectedTexts)
    {
        ArgumentNullException.ThrowIfNull(page);
        ArgumentNullException.ThrowIfNull(expectedTexts);

        var warning = page.Locator(".alert-warning");
        foreach (var expectedText in expectedTexts)
        {
            await Expect(warning).ToContainTextAsync(expectedText);
        }
    }

    public static async Task ExpectPaymentsSummaryVisible(IPage page)
    {
        ArgumentNullException.ThrowIfNull(page);

        var paymentsCard = page.Locator(".card").Filter(new LocatorFilterOptions { HasText = "Payments" });
        await Expect(paymentsCard.GetByText("Total Price", new LocatorGetByTextOptions { Exact = true })).ToBeVisibleAsync();
        await Expect(paymentsCard.GetByText("Amount Paid", new LocatorGetByTextOptions { Exact = true })).ToBeVisibleAsync();
        await Expect(paymentsCard.GetByText("Remaining Balance", new LocatorGetByTextOptions { Exact = true })).ToBeVisibleAsync();
    }
}
