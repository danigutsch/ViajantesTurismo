using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.SystemTests.Shared;

public class ConsistencyTests(AspireSystemTestFixture fixture) : AspireSystemTestBase<AspireSystemTestFixture>(fixture)
{
    [Fact]
    public async Task Tour_details_show_expected_currency_and_date_formatting()
    {
        // Arrange
        var tour = await ApiClient.CreateTour(new CreateTourOptions { Currency = CurrencyDto.Real });

        // Act
        await NavigateTo($"/tours/{tour.Id}");
        await Expect(Page).ToHaveTitleAsync("Tour Details");

        // Assert
        await Expect(Page.GetByText(ConsistencyTestRegexes.BrlPrice()).First).ToBeVisibleAsync();
        await Expect(Page.GetByText(ConsistencyTestRegexes.DateFormat()).First).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Booking_list_and_details_show_consistent_status_and_payment_badges()
    {
        // Arrange
        var tour = await ApiClient.CreateTour(new CreateTourOptions { Currency = CurrencyDto.UsDollar });
        var pendingCustomer = await ApiClient.CreateCustomer();
        var paidCustomer = await ApiClient.CreateCustomer();

        var pendingBooking = await ApiClient.CreateBooking(tour.Id, pendingCustomer.Id);
        var paidBooking = await ApiClient.CreateConfirmedPaidBooking(tour.Id, paidCustomer.Id);

        // Act
        await NavigateTo($"/tours/{tour.Id}");
        await Expect(Page).ToHaveTitleAsync("Tour Details");
        var pendingRow = Page.Locator($".table tbody tr:has(a[href='/bookings/{pendingBooking.Id}'])");
        var paidRow = Page.Locator($".table tbody tr:has(a[href='/bookings/{paidBooking.Id}'])");
        await Expect(pendingRow).ToHaveCountAsync(1);
        await Expect(paidRow).ToHaveCountAsync(1);
        var pendingStatusFromList = (await pendingRow.Locator("td .badge").First.InnerTextAsync()).Trim();
        var paidStatusFromList = (await paidRow.Locator("td .badge").First.InnerTextAsync()).Trim();
        var paidPaymentFromList = (await paidRow.Locator("td .badge").Last.InnerTextAsync()).Trim();

        var pendingStatusFromDetails = await ReadBookingDetailsBadgeText(pendingBooking.Id, "Status");
        var paidStatusFromDetails = await ReadBookingDetailsBadgeText(paidBooking.Id, "Status");
        var paidPaymentFromDetails = await ReadBookingDetailsBadgeText(paidBooking.Id, "Payment Status");

        // Assert
        (pendingStatusFromDetails).ShouldBe(pendingStatusFromList);
        (paidStatusFromDetails).ShouldBe(paidStatusFromList);
        (paidPaymentFromDetails).ShouldBe(paidPaymentFromList);
        (paidPaymentFromDetails).ShouldBe("Paid");
    }

    [Theory]
    [InlineData("/", "Home - ViajantesTurismo")]
    [InlineData("/tours", "Tours")]
    [InlineData("/customers", "Customers")]
    [InlineData("/bookings", "Bookings")]
    [InlineData("/addtour", "Add Tour")]
    public async Task Major_routes_show_expected_page_titles(string route, string expectedTitle)
    {
        // Arrange
        // Act
        // Assert
        await NavigateTo(route);
        await Expect(Page).ToHaveTitleAsync(expectedTitle);
    }
}
