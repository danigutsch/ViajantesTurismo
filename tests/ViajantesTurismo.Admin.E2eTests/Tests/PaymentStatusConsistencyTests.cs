namespace ViajantesTurismo.Admin.E2ETests.Tests;

public class PaymentStatusConsistencyTests(E2EFixture fixture) : E2ETestBase(fixture)
{
    [Fact]
    public async Task Bookings_List_Payment_Status_Matches_Booking_Details()
    {
        // Arrange
        var bookingsListPage = new BookingsListPage(Page, NavigateToAsync);
        var tour = await ApiTestHelper.CreateTourAsync(ApiClient);
        var customerUnpaid = await ApiTestHelper.CreateCustomerAsync(ApiClient);
        var customerPartiallyPaid = await ApiTestHelper.CreateCustomerAsync(ApiClient);

        var unpaidBooking = await ApiTestHelper.CreateBookingAsync(ApiClient, tour.Id, customerUnpaid.Id);
        var partiallyPaidBooking = await ApiTestHelper.CreateBookingAsync(ApiClient, tour.Id, customerPartiallyPaid.Id);
        await ApiTestHelper.RecordPaymentAsync(ApiClient, partiallyPaidBooking.Id, 500m);
        var allBookings = await ApiTestHelper.GetAllBookings(ApiClient);

        // Act
        // Assert
        var unpaidFromList = await bookingsListPage.GetPaymentStatus(unpaidBooking.Id, allBookings);
        var partiallyPaidFromList = await bookingsListPage.GetPaymentStatus(partiallyPaidBooking.Id, allBookings);

        var unpaidFromDetails = await GetPaymentStatusFromDetails(unpaidBooking.Id);
        var partiallyPaidFromDetails = await GetPaymentStatusFromDetails(partiallyPaidBooking.Id);

        Assert.Equal(unpaidFromList, unpaidFromDetails);
        Assert.Equal(partiallyPaidFromList, partiallyPaidFromDetails);
        Assert.NotEqual("Unpaid", partiallyPaidFromList);
    }

    [Fact]
    public async Task Scoped_Bookings_Payment_Status_Matches_Global_List()
    {
        // Arrange
        var bookingsListPage = new BookingsListPage(Page, NavigateToAsync);
        var tour = await ApiTestHelper.CreateTourAsync(ApiClient);
        var customer1 = await ApiTestHelper.CreateCustomerAsync(ApiClient);
        var customer2 = await ApiTestHelper.CreateCustomerAsync(ApiClient);
        var booking1 = await ApiTestHelper.CreateBookingAsync(ApiClient, tour.Id, customer1.Id);
        var booking2 = await ApiTestHelper.CreateBookingAsync(ApiClient, tour.Id, customer2.Id);
        await ApiTestHelper.RecordPaymentAsync(ApiClient, booking2.Id, 300m);
        var allBookings = await ApiTestHelper.GetAllBookings(ApiClient);

        var booking1Href = $"/bookings/{booking1.Id}";
        var booking2Href = $"/bookings/{booking2.Id}";
        var expectedBooking1 = await bookingsListPage.GetPaymentStatus(booking1.Id, allBookings);
        var expectedBooking2 = await bookingsListPage.GetPaymentStatus(booking2.Id, allBookings);

        // Act
        await NavigateToAsync($"/tours/{tour.Id}");
        await Expect(Page).ToHaveTitleAsync("Tour Details");

        var scopedBooking1Row = Page.Locator($".table tbody tr:has(a[href='{booking1Href}'])");
        var scopedBooking2Row = Page.Locator($".table tbody tr:has(a[href='{booking2Href}'])");
        await Expect(scopedBooking1Row).ToHaveCountAsync(1);
        await Expect(scopedBooking2Row).ToHaveCountAsync(1);

        var scopedBooking1Status = (await scopedBooking1Row.First.Locator("td .badge").Last.InnerTextAsync()).Trim();
        var scopedBooking2Status = (await scopedBooking2Row.First.Locator("td .badge").Last.InnerTextAsync()).Trim();

        // Assert
        Assert.Equal(expectedBooking1, scopedBooking1Status);
        Assert.Equal(expectedBooking2, scopedBooking2Status);
    }

    private async Task<string> GetPaymentStatusFromDetails(Guid bookingId)
    {
        await NavigateToAsync($"/bookings/{bookingId}");
        await Expect(Page).ToHaveTitleAsync("Booking Details");

        var badges = Page.Locator("dd .badge");
        await Expect(badges.Nth(1)).ToBeVisibleAsync();
        return (await badges.Nth(1).InnerTextAsync()).Trim();
    }
}
