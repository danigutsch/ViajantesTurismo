using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace ViajantesTurismo.Admin.E2ETests.Tests;

public partial class ConsistencyTests(E2EFixture fixture) : E2ETestBase(fixture)
{
    [Fact]
    public async Task Formatting_And_Badges_Are_Consistent_Across_Pages()
    {
        // === Currency formatting: Tour list → Tour details ===
        await NavigateToAsync("/tours");
        await Expect(Page).ToHaveTitleAsync("Tours");

        // Get pricing text for first BRL tour (City Highlights: R$ 1,500.00)
        var cityHighlightsRow = Page.Locator("table tbody tr").Filter(new LocatorFilterOptions { HasText = "City Highlights" });
        await Expect(cityHighlightsRow).ToBeVisibleAsync();
        var listPriceText = await cityHighlightsRow.InnerTextAsync();

        // BRL prices should use "R$" prefix
        Assert.Contains("R$", listPriceText, StringComparison.Ordinal);

        // Navigate to "City Highlights" details
        await cityHighlightsRow.GetLink("View").ClickAsync();
        await Expect(Page).ToHaveTitleAsync("Tour Details");

        // Currency formatting on details should match: R$ prefix for Real
        await Expect(Page.GetByText(BrlPriceRegex()).First).ToBeVisibleAsync();

        // Navigate back and check EUR tour (Historical Landmarks: 2,000.00 €)
        await NavigateToAsync("/tours");
        var historicalRow = Page.Locator("table tbody tr").Filter(new LocatorFilterOptions { HasText = "Historical Landmarks" });
        var historicalPriceText = await historicalRow.InnerTextAsync();

        // EUR prices should use "€" suffix
        Assert.Contains("€", historicalPriceText, StringComparison.Ordinal);

        await historicalRow.GetLink("View").ClickAsync();
        await Expect(Page).ToHaveTitleAsync("Tour Details");
        await Expect(Page.GetByText(EurPriceRegex()).First).ToBeVisibleAsync();

        // Navigate back and check USD tour (Cultural Experience)
        await NavigateToAsync("/tours");
        var culturalRow = Page.Locator("table tbody tr").Filter(new LocatorFilterOptions { HasText = "Cultural Experience" });
        var culturalPriceText = await culturalRow.InnerTextAsync();
        Assert.Contains("$", culturalPriceText, StringComparison.Ordinal);

        // === Date formatting: dd/MM/yyyy across list and details ===
        // Tour list and details should use consistent date format
        await culturalRow.GetLink("View").ClickAsync();
        await Expect(Page).ToHaveTitleAsync("Tour Details");
        await Expect(Page.GetByText(DateFormatRegex()).First).ToBeVisibleAsync();

        // === Booking status badge consistency: list vs details ===
        await NavigateToAsync("/bookings");
        await Expect(Page).ToHaveTitleAsync("Bookings");

        // Find a booking with "Pending" status in the list
        var pendingBadgeInList = Page.Locator("table tbody tr .badge.bg-warning");
        if (await pendingBadgeInList.CountAsync() > 0)
        {
            var pendingRow = Page.Locator("table tbody tr")
                .Filter(new LocatorFilterOptions { Has = Page.Locator(".badge.bg-warning") }).First;
            await pendingRow.GetLink("View").ClickAsync();
            await Expect(Page).ToHaveTitleAsync("Booking Details");

            // Details page should also show Pending badge with same class
            var detailPendingBadge = Page.Locator(".badge.bg-warning");
            await Expect(detailPendingBadge.First).ToBeVisibleAsync();
            await Expect(detailPendingBadge.First).ToContainTextAsync("Pending");
        }

        // Find a booking with "Confirmed" status
        await NavigateToAsync("/bookings");
        var confirmedBadgeInList = Page.Locator("table tbody tr .badge.bg-success");
        if (await confirmedBadgeInList.CountAsync() > 0)
        {
            var confirmedRow = Page.Locator("table tbody tr")
                .Filter(new LocatorFilterOptions { Has = Page.Locator(".badge.bg-success") }).First;
            await confirmedRow.GetLink("View").ClickAsync();
            await Expect(Page).ToHaveTitleAsync("Booking Details");

            var detailConfirmedBadge = Page.Locator(".badge.bg-success");
            await Expect(detailConfirmedBadge.First).ToBeVisibleAsync();
        }

        // === Payment status badge consistency ===
        await NavigateToAsync("/bookings");

        // Check for "Paid" badge
        var paidBadge = Page.Locator("table tbody tr .badge:has-text('Paid')");
        if (await paidBadge.CountAsync() > 0)
        {
            var paidRow = Page.Locator("table tbody tr")
                .Filter(new LocatorFilterOptions { Has = Page.Locator(".badge:has-text('Paid')") }).First;
            await paidRow.GetLink("View").ClickAsync();
            await Expect(Page).ToHaveTitleAsync("Booking Details");

            // Payment status badge on details should be consistent
            var detailPaymentBadge = Page.Locator(".badge:has-text('Paid')");
            await Expect(detailPaymentBadge.First).ToBeVisibleAsync();
        }

        // === Route-title consistency for major pages ===
        var routeTitles = new Dictionary<string, string>
        {
            ["/"] = "Home - ViajantesTurismo",
            ["/tours"] = "Tours",
            ["/customers"] = "Customers",
            ["/bookings"] = "Bookings",
            ["/addtour"] = "Add Tour"
        };

        foreach (var (route, expectedTitle) in routeTitles)
        {
            await NavigateToAsync(route);
            await Expect(Page).ToHaveTitleAsync(expectedTitle);
        }
    }

    [GeneratedRegex(@"R\$\s[\d,]+\.\d{2}")]
    private static partial Regex BrlPriceRegex();

    [GeneratedRegex(@"[\d,]+\.\d{2}\s€")]
    private static partial Regex EurPriceRegex();

    [GeneratedRegex(@"\d{2}/\d{2}/\d{4}")]
    private static partial Regex DateFormatRegex();
}
