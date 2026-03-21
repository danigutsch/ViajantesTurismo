using System.Text.RegularExpressions;
using ViajantesTurismo.Admin.Contracts;

namespace ViajantesTurismo.Admin.E2ETests.Shared;

public partial class ConsistencyTests(E2EFixture fixture) : E2ETestBase(fixture)
{
    [Fact]
    public async Task Tour_List_And_Details_Show_Consistent_Currency_And_Date_Formatting()
    {
        // Arrange
        var tour = await ApiClient.CreateTour(new CreateTourOptions { Currency = CurrencyDto.Real });

        // Act
        await NavigateTo("/tours");
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
        var tour = await ApiClient.CreateTour(new CreateTourOptions { Currency = CurrencyDto.UsDollar });
        var pendingCustomer = await ApiClient.CreateCustomer();
        var paidCustomer = await ApiClient.CreateCustomer();

        var pendingBooking = await ApiClient.CreateBooking(tour.Id, pendingCustomer.Id);
        var paidBooking = await ApiClient.CreateConfirmedPaidBooking(tour.Id, paidCustomer.Id);

        // Act
        await NavigateTo("/bookings");
        await Expect(Page).ToHaveTitleAsync("Bookings");

        var pendingStatusFromList = await BookingsList.GetBookingStatus(pendingBooking.Id);
        var paidStatusFromList = await BookingsList.GetBookingStatus(paidBooking.Id);
        var paidPaymentFromList = await BookingsList.GetPaymentStatus(paidBooking.Id);

        var pendingStatusFromDetails = await ReadBookingDetailsBadgeText(pendingBooking.Id, "Status");
        var paidStatusFromDetails = await ReadBookingDetailsBadgeText(paidBooking.Id, "Status");
        var paidPaymentFromDetails = await ReadBookingDetailsBadgeText(paidBooking.Id, "Payment Status");

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
        await NavigateTo(route);
        await Expect(Page).ToHaveTitleAsync(expectedTitle);
    }

    [GeneratedRegex(@"R\$\s[\d,]+\.\d{2}")]
    private static partial Regex BrlPriceRegex();

    [GeneratedRegex(@"\d{2}/\d{2}/\d{4}")]
    private static partial Regex DateFormatRegex();
}
