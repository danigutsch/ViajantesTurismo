namespace ViajantesTurismo.Admin.SystemTests.Bookings;

public class PaymentStatusConsistencyTests(AspireSystemTestFixture fixture) : AspireSystemTestBase<AspireSystemTestFixture>(fixture)
{
    [Fact]
    public async Task Bookings_list_payment_status_matches_booking_details()
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
        var unpaidFromList = await BookingsList.GetPaymentStatus(unpaidBooking.Id);
        var partiallyPaidFromList = await BookingsList.GetPaymentStatus(partiallyPaidBooking.Id);
        var unpaidFromDetails = await ReadBookingDetailsBadgeText(unpaidBooking.Id, "Payment Status");
        var partiallyPaidFromDetails = await ReadBookingDetailsBadgeText(partiallyPaidBooking.Id, "Payment Status");

        // Assert
        unpaidFromList.ShouldBe("Unpaid");
        partiallyPaidFromList.ShouldBe("Partially Paid");
        unpaidFromDetails.ShouldBe("Unpaid");
        partiallyPaidFromDetails.ShouldBe("Partially Paid");
        unpaidFromDetails.ShouldBe(unpaidFromList);
        partiallyPaidFromDetails.ShouldBe(partiallyPaidFromList);
    }

    [Fact]
    public async Task Scoped_bookings_payment_status_matches_global_list()
    {
        // Arrange
        var tour = await ApiClient.CreateTour();
        var customer1 = await ApiClient.CreateCustomer();
        var customer2 = await ApiClient.CreateCustomer();
        var booking1 = await ApiClient.CreateBooking(tour.Id, customer1.Id);
        var booking2 = await ApiClient.CreatePartiallyPaidBooking(tour.Id, customer2.Id, 300m);

        // Act
        var booking1GlobalStatus = await BookingsList.GetPaymentStatus(booking1.Id);
        var booking2GlobalStatus = await BookingsList.GetPaymentStatus(booking2.Id);
        await NavigateTo($"/tours/{tour.Id}");
        await Expect(Page).ToHaveTitleAsync("Tour Details");

        var scopedBooking1Row = Page.Locator($".table tbody tr:has(a[href='/bookings/{booking1.Id}'])");
        var scopedBooking2Row = Page.Locator($".table tbody tr:has(a[href='/bookings/{booking2.Id}'])");
        await Expect(scopedBooking1Row).ToHaveCountAsync(1);
        await Expect(scopedBooking2Row).ToHaveCountAsync(1);

        var scopedBooking1Status = (await scopedBooking1Row.Locator("td .badge").Last.InnerTextAsync()).Trim();
        var scopedBooking2Status = (await scopedBooking2Row.Locator("td .badge").Last.InnerTextAsync()).Trim();

        // Assert
        booking1GlobalStatus.ShouldBe("Unpaid");
        booking2GlobalStatus.ShouldBe("Partially Paid");
        scopedBooking1Status.ShouldBe("Unpaid");
        scopedBooking2Status.ShouldBe("Partially Paid");
        scopedBooking1Status.ShouldBe(booking1GlobalStatus);
        scopedBooking2Status.ShouldBe(booking2GlobalStatus);
    }
}
