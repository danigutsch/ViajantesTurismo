using System.Text.RegularExpressions;
using Microsoft.Playwright;
using ViajantesTurismo.Admin.Contracts;
using ViajantesTurismo.Admin.E2ETests.Infrastructure.Api;
using ViajantesTurismo.Admin.E2ETests.Infrastructure.Bases;
using ViajantesTurismo.Admin.E2ETests.Infrastructure.Fixtures;
using ViajantesTurismo.Admin.E2ETests.Infrastructure.Helpers;

namespace ViajantesTurismo.Admin.E2ETests.Shared;

public partial class ConsistencyTests(E2EFixture fixture) : E2ESerialTestBase(fixture)
{
    [Fact]
    public async Task Formatting_And_Badges_Are_Consistent_Across_Pages()
    {
        // Arrange: create owned tours for each currency and bookings for each status shape.
        var brlTour = await ApiClient.CreateTour(currency: CurrencyDto.Real);
        var eurTour = await ApiClient.CreateTour(currency: CurrencyDto.Euro);
        var usdTour = await ApiClient.CreateTour(currency: CurrencyDto.UsDollar);

        var pendingCustomer = await ApiClient.CreateCustomer();
        var confirmedCustomer = await ApiClient.CreateCustomer();
        var paidCustomer = await ApiClient.CreateCustomer();

        var pendingBooking = await ApiClient.CreateBooking(usdTour.Id, pendingCustomer.Id);
        var confirmedBooking = await ApiClient.CreateBooking(usdTour.Id, confirmedCustomer.Id);
        _ = await ApiClient.ConfirmBookingAsync(confirmedBooking.Id);
        var paidBooking = await ApiClient.CreateBooking(usdTour.Id, paidCustomer.Id);
        _ = await ApiClient.ConfirmBookingAsync(paidBooking.Id);
        await ApiClient.RecordPaymentAsync(paidBooking.Id, 1_250m);

        // === Currency formatting: Tour list → Tour details ===
        await NavigateTo("/tours");
        await Expect(Page).ToHaveTitleAsync("Tours");

        // BRL row/list and details formatting.
        var brlRow = Page.Locator("table tbody tr").Filter(new LocatorFilterOptions { HasText = brlTour.Identifier });
        await Expect(brlRow).ToBeVisibleAsync();
        var brlListPriceText = await brlRow.InnerTextAsync();

        // BRL prices should use the "R$" prefix
        Assert.Contains("R$", brlListPriceText, StringComparison.Ordinal);

        await brlRow.GetLink("View").ClickAsync();
        await Expect(Page).ToHaveTitleAsync("Tour Details");
        await Expect(Page.GetByText(BrlPriceRegex()).First).ToBeVisibleAsync();

        // EUR row/list and details formatting.
        await NavigateTo("/tours");
        var eurRow = Page.Locator("table tbody tr").Filter(new LocatorFilterOptions { HasText = eurTour.Identifier });
        await Expect(eurRow).ToBeVisibleAsync();
        var eurListPriceText = await eurRow.InnerTextAsync();

        // EUR prices should use "€" suffix
        Assert.Contains("€", eurListPriceText, StringComparison.Ordinal);

        await eurRow.GetLink("View").ClickAsync();
        await Expect(Page).ToHaveTitleAsync("Tour Details");
        await Expect(Page.GetByText(EurPriceRegex()).First).ToBeVisibleAsync();

        // USD row/list and details formatting.
        await NavigateTo("/tours");
        var usdRow = Page.Locator("table tbody tr").Filter(new LocatorFilterOptions { HasText = usdTour.Identifier });
        await Expect(usdRow).ToBeVisibleAsync();
        var usdListPriceText = await usdRow.InnerTextAsync();
        Assert.Contains("$", usdListPriceText, StringComparison.Ordinal);

        // === Date formatting: dd/MM/yyyy across the list and details ===
        // Tour list and details should use a consistent date format
        await usdRow.GetLink("View").ClickAsync();
        await Expect(Page).ToHaveTitleAsync("Tour Details");
        await Expect(Page.GetByText(DateFormatRegex()).First).ToBeVisibleAsync();

        // === Booking status badge consistency: list vs details ===
        await NavigateTo("/bookings");
        await Expect(Page).ToHaveTitleAsync("Bookings");

        // Pending booking: list badge and details badge.
        var pendingRow = await BookingsList.GetBookingRow(pendingBooking.Id);
        await Expect(pendingRow.Locator(".badge:has-text('Pending')")).ToBeVisibleAsync();
        await NavigateTo($"/bookings/{pendingBooking.Id}");
        await Expect(Page).ToHaveTitleAsync("Booking Details");
        var detailPendingBadge = Page.Locator(".badge.bg-warning");
        await Expect(detailPendingBadge.First).ToBeVisibleAsync();
        await Expect(detailPendingBadge.First).ToContainTextAsync("Pending");

        // Confirmed booking: list badge and details badge.
        await NavigateTo("/bookings");
        var confirmedRow = await BookingsList.GetBookingRow(confirmedBooking.Id);
        await Expect(confirmedRow.Locator(".badge:has-text('Confirmed')")).ToBeVisibleAsync();
        await NavigateTo($"/bookings/{confirmedBooking.Id}");
        await Expect(Page).ToHaveTitleAsync("Booking Details");
        var detailConfirmedBadge = Page.Locator(".badge.bg-success");
        await Expect(detailConfirmedBadge.First).ToBeVisibleAsync();

        // === Payment status badge consistency ===
        await NavigateTo("/bookings");
        var paidRow = await BookingsList.GetBookingRow(paidBooking.Id);
        await Expect(paidRow.Locator(".badge:has-text('Paid')")).ToBeVisibleAsync();
        await NavigateTo($"/bookings/{paidBooking.Id}");
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
            await NavigateTo(route);
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
