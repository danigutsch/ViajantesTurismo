namespace ViajantesTurismo.Admin.E2ETests.Shared;

public class CrossEntityNavigationTests(E2EFixture fixture) : E2ETestBase(fixture)
{
    private const string CustomerDetailsTitle = "Customer Details";

    [Fact]
    public async Task Booking_Details_Should_Navigate_To_Related_Tour_And_Customer_Details()
    {
        // Arrange
        var tour = await ApiClient.CreateTour();
        var customer = await ApiClient.CreateCustomer();
        var booking = await ApiClient.CreateBooking(tour.Id, customer.Id);

        // Act
        await NavigateTo($"/bookings/{booking.Id}");
        await FollowLinkAndExpectTitle(Page.Locator("a[href^='/tours/']").First, "Tour Details");
        await Page.GoBackAsync();
        await FollowLinkAndExpectTitle(Page.Locator("a[href^='/customers/']").First, CustomerDetailsTitle);

        // Assert
        await Expect(Page).ToHaveTitleAsync(CustomerDetailsTitle);
    }

    [Fact]
    public async Task Bookings_List_Should_Navigate_To_Related_Tour_And_Customer_Details()
    {
        // Arrange
        var tour = await ApiClient.CreateTour();
        var customer = await ApiClient.CreateCustomer();
        var booking = await ApiClient.CreateBooking(tour.Id, customer.Id);

        // Act
        var bookingRow = await BookingsList.GetBookingRow(booking.Id);
        await FollowLinkAndExpectTitle(bookingRow.Locator("a[href^='/tours/']").First, "Tour Details");
        await Page.GoBackAsync();
        bookingRow = await BookingsList.GetBookingRow(booking.Id);
        await FollowLinkAndExpectTitle(bookingRow.Locator("a[href^='/customers/']").First, CustomerDetailsTitle);

        // Assert
        await Expect(Page).ToHaveTitleAsync(CustomerDetailsTitle);
    }

    [Fact]
    public async Task Tour_Details_Should_Show_Contextual_Bookings_Without_Tour_Column()
    {
        // Arrange
        var tour = await ApiClient.CreateTour();
        var customer = await ApiClient.CreateCustomer();
        var booking = await ApiClient.CreateBooking(tour.Id, customer.Id);

        // Act
        await NavigateTo($"/tours/{tour.Id}");

        // Assert
        await Expect(Page).ToHaveTitleAsync("Tour Details");
        var tourBookingsTable = Page.Locator("table");
        await Expect(tourBookingsTable).ToBeVisibleAsync();
        await ExpectColumnVisibility(tourBookingsTable, hiddenHeader: "Tour", visibleHeader: "Customer");
        await FollowLinkAndExpectTitle(tourBookingsTable.Locator($"a[href='/bookings/{booking.Id}']"), "Booking Details");
    }

    [Fact]
    public async Task Customer_Details_Should_Show_Contextual_Bookings_Without_Customer_Column()
    {
        // Arrange
        var tour = await ApiClient.CreateTour();
        var customer = await ApiClient.CreateCustomer();
        var booking = await ApiClient.CreateBooking(tour.Id, customer.Id);

        // Act
        await NavigateTo($"/customers/{customer.Id}");

        // Assert
        await Expect(Page).ToHaveTitleAsync(CustomerDetailsTitle);
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
