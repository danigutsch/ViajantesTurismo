namespace ViajantesTurismo.Admin.E2ETests.Tests;

public class PaymentStatusConsistencyTests(E2EFixture fixture) : E2ETestBase(fixture)
{
    [Fact]
    public async Task Bookings_List_Payment_Status_Matches_Booking_Details()
    {
        // Arrange
        var tour = await ApiClient.CreateTourAsync();
        var customerUnpaid = await ApiClient.CreateCustomerAsync();
        var customerPartiallyPaid = await ApiClient.CreateCustomerAsync();

        var unpaidBooking = await ApiClient.CreateBookingAsync(tour.Id, customerUnpaid.Id);
        var partiallyPaidBooking = await ApiClient.CreateBookingAsync(tour.Id, customerPartiallyPaid.Id);
        await ApiClient.RecordPaymentAsync(partiallyPaidBooking.Id, 500m);

        // Act
        // Assert
        var unpaidFromList = await BookingsList.GetPaymentStatus(unpaidBooking.Id);
        var partiallyPaidFromList = await BookingsList.GetPaymentStatus(partiallyPaidBooking.Id);

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
        var tour = await ApiClient.CreateTourAsync();
        var customer1 = await ApiClient.CreateCustomerAsync();
        var customer2 = await ApiClient.CreateCustomerAsync();
        var booking1 = await ApiClient.CreateBookingAsync(tour.Id, customer1.Id);
        var booking2 = await ApiClient.CreateBookingAsync(tour.Id, customer2.Id);
        await ApiClient.RecordPaymentAsync(booking2.Id, 300m);

        var booking1Href = $"/bookings/{booking1.Id}";
        var booking2Href = $"/bookings/{booking2.Id}";
        var expectedBooking1 = await BookingsList.GetPaymentStatus(booking1.Id);
        var expectedBooking2 = await BookingsList.GetPaymentStatus(booking2.Id);

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
