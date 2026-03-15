namespace ViajantesTurismo.Admin.E2ETests.Bookings;

public class PaymentStatusConsistencyTests(E2EFixture fixture) : E2ETestBase(fixture)
{
    [Fact]
    public async Task Bookings_List_Payment_Status_Matches_Booking_Details()
    {
        // Arrange
        var tour = await ApiClient.CreateTour();
        var customerUnpaid = await ApiClient.CreateCustomer();
        var customerPartiallyPaid = await ApiClient.CreateCustomer();

        var unpaidBooking = await ApiClient.CreateBooking(tour.Id, customerUnpaid.Id);
        var partiallyPaidBooking = await ApiClient.CreatePartiallyPaidBooking(
            tour.Id,
            customerPartiallyPaid.Id,
            500m);

        // Act
        // Assert
        var unpaidFromList = await BookingsList.GetPaymentStatus(unpaidBooking.Id);
        var partiallyPaidFromList = await BookingsList.GetPaymentStatus(partiallyPaidBooking.Id);

        var unpaidFromDetails = await ReadBookingDetailsBadgeText(unpaidBooking.Id, "Payment Status");
        var partiallyPaidFromDetails = await ReadBookingDetailsBadgeText(partiallyPaidBooking.Id, "Payment Status");

        Assert.Equal(unpaidFromList, unpaidFromDetails);
        Assert.Equal(partiallyPaidFromList, partiallyPaidFromDetails);
        Assert.NotEqual("Unpaid", partiallyPaidFromList);
    }

    [Fact]
    public async Task Scoped_Bookings_Payment_Status_Matches_Global_List()
    {
        // Arrange
        var tour = await ApiClient.CreateTour();
        var customer1 = await ApiClient.CreateCustomer();
        var customer2 = await ApiClient.CreateCustomer();
        var booking1 = await ApiClient.CreateBooking(tour.Id, customer1.Id);
        var booking2 = await ApiClient.CreatePartiallyPaidBooking(tour.Id, customer2.Id, 300m);

        var booking1Href = $"/bookings/{booking1.Id}";
        var booking2Href = $"/bookings/{booking2.Id}";
        var expectedBooking1 = await BookingsList.GetPaymentStatus(booking1.Id);
        var expectedBooking2 = await BookingsList.GetPaymentStatus(booking2.Id);

        // Act
        await NavigateTo($"/tours/{tour.Id}");
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
}
