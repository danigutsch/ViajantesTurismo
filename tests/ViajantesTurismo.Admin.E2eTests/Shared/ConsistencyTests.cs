using System.Text.RegularExpressions;
using Microsoft.Playwright;
using ViajantesTurismo.Admin.Contracts;
using ViajantesTurismo.Admin.E2ETests.Infrastructure.Api;
using ViajantesTurismo.Admin.E2ETests.Infrastructure.Bases;
using ViajantesTurismo.Admin.E2ETests.Infrastructure.Fixtures;
using ViajantesTurismo.Admin.E2ETests.Infrastructure.Helpers;

namespace ViajantesTurismo.Admin.E2ETests.Shared;

public partial class ConsistencyTests(E2EFixture fixture) : E2ETestBase(fixture)
{
    [Fact]
    public async Task Tour_List_And_Details_Show_Consistent_Currency_And_Date_Formatting()
    {
        // Arrange
        var tour = await ApiClient.CreateTour(currency: CurrencyDto.Real);

        // Act
        await NavigateToAsync("/tours");
        await Expect(Page).ToHaveTitleAsync("Tours");

        var tourRow = Page.Locator("table tbody tr").Filter(new LocatorFilterOptions { HasText = tour.Identifier });
        await Expect(tourRow).ToBeVisibleAsync();
        var listText = await tourRow.InnerTextAsync();

        // Assert
        Assert.Contains("R$", listText, StringComparison.Ordinal);

        await tourRow.GetLink("View").ClickAsync();
        await Expect(Page).ToHaveTitleAsync("Tour Details");
        await Expect(Page.GetByText(BrlPriceRegex()).First).ToBeVisibleAsync();
        await Expect(Page.GetByText(DateFormatRegex()).First).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Booking_List_And_Details_Show_Consistent_Status_And_Payment_Badges()
    {
        // Arrange
        var tour = await ApiClient.CreateTour(currency: CurrencyDto.UsDollar);
        var pendingCustomer = await ApiClient.CreateCustomer();
        var paidCustomer = await ApiClient.CreateCustomer();

        var pendingBooking = await ApiClient.CreateBooking(tour.Id, pendingCustomer.Id);
        var paidBooking = await ApiClient.CreateBooking(tour.Id, paidCustomer.Id);
        _ = await ApiClient.ConfirmBookingAsync(paidBooking.Id);
        await ApiClient.RecordPaymentAsync(paidBooking.Id, 1_250m);

        // Act
        await NavigateToAsync("/bookings");
        await Expect(Page).ToHaveTitleAsync("Bookings");

        var pendingRow = await BookingsList.GetBookingRow(pendingBooking.Id);
        var paidRow = await BookingsList.GetBookingRow(paidBooking.Id);

        var pendingStatusFromList = (await pendingRow.Locator(".badge").First.InnerTextAsync()).Trim();
        var paidStatusFromList = (await paidRow.Locator(".badge").First.InnerTextAsync()).Trim();
        var paidPaymentFromList = (await paidRow.Locator("td .badge").Last.InnerTextAsync()).Trim();

        var pendingStatusFromDetails = await ReadBookingStatusFromDetails(pendingBooking.Id);
        var paidStatusFromDetails = await ReadBookingStatusFromDetails(paidBooking.Id);
        var paidPaymentFromDetails = await ReadPaymentStatusFromDetails(paidBooking.Id);

        // Assert
        Assert.Equal(pendingStatusFromList, pendingStatusFromDetails);
        Assert.Equal(paidStatusFromList, paidStatusFromDetails);
        Assert.Equal(paidPaymentFromList, paidPaymentFromDetails);
        Assert.Equal("Paid", paidPaymentFromDetails);
    }

    [Theory]
    [InlineData("/", "Home - ViajantesTurismo")]
    [InlineData("/tours", "Tours")]
    [InlineData("/customers", "Customers")]
    [InlineData("/bookings", "Bookings")]
    [InlineData("/addtour", "Add Tour")]
    public async Task Major_Routes_Show_Expected_Page_Titles(string route, string expectedTitle)
    {
        // Arrange
        // Act
        // Assert
        await NavigateToAsync(route);
        await Expect(Page).ToHaveTitleAsync(expectedTitle);
    }

    private async Task<string> ReadBookingStatusFromDetails(Guid bookingId)
    {
        await NavigateToAsync($"/bookings/{bookingId}");
        await Expect(Page).ToHaveTitleAsync("Booking Details");

        var bookingStatusBadge = Page.Locator("dd .badge").First;
        await Expect(bookingStatusBadge).ToBeVisibleAsync();
        return (await bookingStatusBadge.InnerTextAsync()).Trim();
    }

    private async Task<string> ReadPaymentStatusFromDetails(Guid bookingId)
    {
        await NavigateToAsync($"/bookings/{bookingId}");
        await Expect(Page).ToHaveTitleAsync("Booking Details");

        var paymentStatusBadge = Page.Locator("dd .badge").Nth(1);
        await Expect(paymentStatusBadge).ToBeVisibleAsync();
        return (await paymentStatusBadge.InnerTextAsync()).Trim();
    }

    [GeneratedRegex(@"R\$\s[\d,]+\.\d{2}")]
    private static partial Regex BrlPriceRegex();

    [GeneratedRegex(@"\d{2}/\d{2}/\d{4}")]
    private static partial Regex DateFormatRegex();
}
