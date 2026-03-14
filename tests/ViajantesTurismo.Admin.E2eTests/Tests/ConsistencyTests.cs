using System.Text.RegularExpressions;
using Microsoft.Playwright;
using ViajantesTurismo.Admin.Contracts;

namespace ViajantesTurismo.Admin.E2ETests.Tests;

public partial class ConsistencyTests(E2EFixture fixture) : E2ESerialTestBase(fixture)
{
    [Fact]
    public async Task Formatting_And_Badges_Are_Consistent_Across_Pages()
    {
        // Arrange: create owned tours for each currency and bookings for each status shape.
        var brlTour = await ApiTestHelper.CreateTourAsync(ApiClient, currency: CurrencyDto.Real);
        var eurTour = await ApiTestHelper.CreateTourAsync(ApiClient, currency: CurrencyDto.Euro);
        var usdTour = await ApiTestHelper.CreateTourAsync(ApiClient, currency: CurrencyDto.UsDollar);

        var pendingCustomer = await ApiTestHelper.CreateCustomerAsync(ApiClient);
        var confirmedCustomer = await ApiTestHelper.CreateCustomerAsync(ApiClient);
        var paidCustomer = await ApiTestHelper.CreateCustomerAsync(ApiClient);

        var pendingBooking = await ApiTestHelper.CreateBookingAsync(ApiClient, usdTour.Id, pendingCustomer.Id);
        var confirmedBooking = await ApiTestHelper.CreateBookingAsync(ApiClient, usdTour.Id, confirmedCustomer.Id);
        _ = await ApiTestHelper.ConfirmBookingAsync(ApiClient, confirmedBooking.Id);
        var paidBooking = await ApiTestHelper.CreateBookingAsync(ApiClient, usdTour.Id, paidCustomer.Id);
        _ = await ApiTestHelper.ConfirmBookingAsync(ApiClient, paidBooking.Id);
        await ApiTestHelper.RecordPaymentAsync(ApiClient, paidBooking.Id, 1_250m);

        // === Currency formatting: Tour list → Tour details ===
        await NavigateToAsync("/tours");
        await Expect(Page).ToHaveTitleAsync("Tours");

        // BRL row/list and details formatting.
        var brlRow = Page.Locator("table tbody tr").Filter(new LocatorFilterOptions { HasText = brlTour.Identifier });
        await Expect(brlRow).ToBeVisibleAsync();
        var brlListPriceText = await brlRow.InnerTextAsync();

        // BRL prices should use "R$" prefix
        Assert.Contains("R$", brlListPriceText, StringComparison.Ordinal);

        await brlRow.GetLink("View").ClickAsync();
        await Expect(Page).ToHaveTitleAsync("Tour Details");
        await Expect(Page.GetByText(BrlPriceRegex()).First).ToBeVisibleAsync();

        // EUR row/list and details formatting.
        await NavigateToAsync("/tours");
        var eurRow = Page.Locator("table tbody tr").Filter(new LocatorFilterOptions { HasText = eurTour.Identifier });
        await Expect(eurRow).ToBeVisibleAsync();
        var eurListPriceText = await eurRow.InnerTextAsync();

        // EUR prices should use "€" suffix
        Assert.Contains("€", eurListPriceText, StringComparison.Ordinal);

        await eurRow.GetLink("View").ClickAsync();
        await Expect(Page).ToHaveTitleAsync("Tour Details");
        await Expect(Page.GetByText(EurPriceRegex()).First).ToBeVisibleAsync();

        // USD row/list and details formatting.
        await NavigateToAsync("/tours");
        var usdRow = Page.Locator("table tbody tr").Filter(new LocatorFilterOptions { HasText = usdTour.Identifier });
        await Expect(usdRow).ToBeVisibleAsync();
        var usdListPriceText = await usdRow.InnerTextAsync();
        Assert.Contains("$", usdListPriceText, StringComparison.Ordinal);

        // === Date formatting: dd/MM/yyyy across list and details ===
        // Tour list and details should use consistent date format
        await usdRow.GetLink("View").ClickAsync();
        await Expect(Page).ToHaveTitleAsync("Tour Details");
        await Expect(Page.GetByText(DateFormatRegex()).First).ToBeVisibleAsync();

        // === Booking status badge consistency: list vs details ===
        await NavigateToAsync("/bookings");
        await Expect(Page).ToHaveTitleAsync("Bookings");

        // Pending booking: list badge and details badge.
        var pendingRow = await Page.RequireRowByLinkAcrossPagesAsync($"/bookings/{pendingBooking.Id}");
        await Expect(pendingRow.Locator(".badge:has-text('Pending')")).ToBeVisibleAsync();
        await NavigateToAsync($"/bookings/{pendingBooking.Id}");
        await Expect(Page).ToHaveTitleAsync("Booking Details");
        var detailPendingBadge = Page.Locator(".badge.bg-warning");
        await Expect(detailPendingBadge.First).ToBeVisibleAsync();
        await Expect(detailPendingBadge.First).ToContainTextAsync("Pending");

        // Confirmed booking: list badge and details badge.
        await NavigateToAsync("/bookings");
        var confirmedRow = await Page.RequireRowByLinkAcrossPagesAsync($"/bookings/{confirmedBooking.Id}");
        await Expect(confirmedRow.Locator(".badge:has-text('Confirmed')")).ToBeVisibleAsync();
        await NavigateToAsync($"/bookings/{confirmedBooking.Id}");
        await Expect(Page).ToHaveTitleAsync("Booking Details");
        var detailConfirmedBadge = Page.Locator(".badge.bg-success");
        await Expect(detailConfirmedBadge.First).ToBeVisibleAsync();

        // === Payment status badge consistency ===
        await NavigateToAsync("/bookings");
        var paidRow = await Page.RequireRowByLinkAcrossPagesAsync($"/bookings/{paidBooking.Id}");
        await Expect(paidRow.Locator(".badge:has-text('Paid')")).ToBeVisibleAsync();
        await NavigateToAsync($"/bookings/{paidBooking.Id}");
        await Expect(Page).ToHaveTitleAsync("Booking Details");
        var detailPaymentBadge = Page.Locator(".badge:has-text('Paid')");
        await Expect(detailPaymentBadge.First).ToBeVisibleAsync();

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
