namespace ViajantesTurismo.Admin.SystemTests.Bookings;

public class PaymentStatusConsistencyTests(AspireSystemTestFixture fixture) : AspireSystemTestBase<AspireSystemTestFixture>(fixture)
{
    [Fact]
    public async Task Tour_bookings_payment_status_matches_booking_details()
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
        await NavigateTo($"/tours/{tour.Id}");
        await Expect(Page).ToHaveTitleAsync("Tour Details");
        var unpaidRow = Page.Locator($".table tbody tr:has(a[href='/bookings/{unpaidBooking.Id}'])");
        var partiallyPaidRow = Page.Locator($".table tbody tr:has(a[href='/bookings/{partiallyPaidBooking.Id}'])");
        await Expect(unpaidRow).ToHaveCountAsync(1);
        await Expect(partiallyPaidRow).ToHaveCountAsync(1);
        var unpaidFromList = (await unpaidRow.Locator("td .badge").Last.InnerTextAsync()).Trim();
        var partiallyPaidFromList = (await partiallyPaidRow.Locator("td .badge").Last.InnerTextAsync()).Trim();
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
}
