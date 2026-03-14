namespace ViajantesTurismo.Admin.E2ETests.Shared;

public class CrossEntityNavigationTests(E2EFixture fixture) : E2ETestBase(fixture)
{
    [Fact]
    public async Task Can_Navigate_Between_Entities_From_Booking_Details_And_Lists()
    {
        // Arrange
        var tour = await ApiClient.CreateTour();
        var customer = await ApiClient.CreateCustomer();
        var booking = await ApiClient.CreateBooking(tour.Id, customer.Id);

        await NavigateTo($"/bookings/{booking.Id}");
        await Expect(Page).ToHaveTitleAsync("Booking Details");

        await FollowLinkAndExpectTitle(Page.Locator("a[href^='/tours/']").First, "Tour Details");

        await Page.GoBackAsync();
        await Expect(Page).ToHaveTitleAsync("Booking Details");

        await FollowLinkAndExpectTitle(Page.Locator("a[href^='/customers/']").First, "Customer Details");

        var bookingRow = await BookingsList.GetBookingRow(booking.Id);

        await FollowLinkAndExpectTitle(bookingRow.Locator("a[href^='/tours/']").First, "Tour Details");

        await Page.GoBackAsync();
        await Expect(Page).ToHaveTitleAsync("Bookings");

        bookingRow = await BookingsList.GetBookingRow(booking.Id);
        await FollowLinkAndExpectTitle(bookingRow.Locator("a[href^='/customers/']").First, "Customer Details");
    }

    [Fact]
    public async Task Can_View_Contextual_Bookings_On_Customer_And_Tour_Details()
    {
        // Arrange
        var tour = await ApiClient.CreateTour();
        var customer = await ApiClient.CreateCustomer();
        var booking = await ApiClient.CreateBooking(tour.Id, customer.Id);

        await NavigateTo($"/tours/{tour.Id}");
        await Expect(Page).ToHaveTitleAsync("Tour Details");

        var tourBookingsTable = Page.Locator("table");
        await Expect(tourBookingsTable).ToBeVisibleAsync();

        await ExpectColumnVisibility(tourBookingsTable, hiddenHeader: "Tour", visibleHeader: "Customer");
        await FollowLinkAndExpectTitle(tourBookingsTable.Locator($"a[href='/bookings/{booking.Id}']"), "Booking Details");

        await NavigateTo($"/customers/{customer.Id}");
        await Expect(Page).ToHaveTitleAsync("Customer Details");

        var customerBookingsTable = Page.Locator("table");
        await Expect(customerBookingsTable).ToBeVisibleAsync();

        await ExpectColumnVisibility(customerBookingsTable, hiddenHeader: "Customer", visibleHeader: "Tour");
        await Expect(customerBookingsTable.Locator($"a[href='/bookings/{booking.Id}']")).ToBeVisibleAsync();
    }

    private async Task FollowLinkAndExpectTitle(ILocator link, string expectedTitle)
    {
        await Expect(link).ToBeVisibleAsync();
        await link.ClickAsync();
        await Expect(Page).ToHaveTitleAsync(expectedTitle);
    }

    private async Task ExpectColumnVisibility(ILocator table, string hiddenHeader, string visibleHeader)
    {
        await Expect(table.Locator($"th:has-text('{hiddenHeader}')")).ToHaveCountAsync(0);
        await Expect(table.Locator($"th:has-text('{visibleHeader}')")).ToBeVisibleAsync();
    }
}
