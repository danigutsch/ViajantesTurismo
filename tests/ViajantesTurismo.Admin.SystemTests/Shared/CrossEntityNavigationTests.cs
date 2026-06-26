namespace ViajantesTurismo.Admin.SystemTests.Shared;

public class CrossEntityNavigationTests(AspireSystemTestFixture fixture) : AspireSystemTestBase<AspireSystemTestFixture>(fixture)
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
        await CrossEntityNavigationTestHelpers.FollowLinkAndExpectTitle(Page, Page.Locator("a[href^='/tours/']").First, "Tour Details");
        await Page.GoBackAsync();
        await CrossEntityNavigationTestHelpers.FollowLinkAndExpectTitle(Page, Page.Locator("a[href^='/customers/']").First, CustomerDetailsTitle);

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
        await CrossEntityNavigationTestHelpers.FollowLinkAndExpectTitle(Page, bookingRow.Locator("a[href^='/tours/']").First, "Tour Details");
        await Page.GoBackAsync();
        bookingRow = await BookingsList.GetBookingRow(booking.Id);
        await CrossEntityNavigationTestHelpers.FollowLinkAndExpectTitle(Page, bookingRow.Locator("a[href^='/customers/']").First, CustomerDetailsTitle);

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
        await CrossEntityNavigationTestHelpers.ExpectColumnVisibility(tourBookingsTable, hiddenHeader: "Tour", visibleHeader: "Customer");
        await CrossEntityNavigationTestHelpers.FollowLinkAndExpectTitle(Page, tourBookingsTable.Locator($"a[href='/bookings/{booking.Id}']"), "Booking Details");
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
        await CrossEntityNavigationTestHelpers.ExpectColumnVisibility(customerBookingsTable, hiddenHeader: "Customer", visibleHeader: "Tour");
        await Expect(customerBookingsTable.Locator($"a[href='/bookings/{booking.Id}']")).ToBeVisibleAsync();
    }
}
