namespace ViajantesTurismo.Admin.SystemTests.Bookings;

public class BookingEditStateTests(AspireSystemTestFixture fixture) : AspireSystemTestBase<AspireSystemTestFixture>(fixture)
{
    [Fact]
    public async Task Cancelled_booking_edit_hides_payment_and_action_buttons()
    {
        // Arrange
        var tour = await ApiClient.CreateTour(new CreateTourOptions { MinCustomers = 1, MaxCustomers = 20 });
        var customer = await ApiClient.CreateCustomer();
        var booking = await ApiClient.CreateCancelledBooking(tour.Id, customer.Id);

        // Act
        await BookingWorkflow.NavigateToEdit(booking.Id);

        // Assert
        await BookingEditStateTestHelpers.ExpectWarningContains(Page, "cancelled", "cannot be modified");
        await Expect(Page.GetButton("Record Payment")).Not.ToBeVisibleAsync();
        await BookingEditStateTestHelpers.ExpectPaymentsSummaryVisible(Page);
        await Expect(Page.GetButton("Confirm Booking")).Not.ToBeVisibleAsync();
        await Expect(Page.GetButton("Complete Booking")).Not.ToBeVisibleAsync();
        await Expect(Page.GetButton("Cancel Booking")).Not.ToBeVisibleAsync();
        await Expect(Page.GetButton("Delete Booking")).ToBeEnabledAsync();
    }

    [Fact]
    public async Task Confirmed_booking_edit_shows_action_buttons_and_payment_section()
    {
        // Arrange
        var tour = await ApiClient.CreateTour(new CreateTourOptions { MinCustomers = 1, MaxCustomers = 20 });
        var customer = await ApiClient.CreateCustomer();
        var booking = await ApiClient.CreateConfirmedBooking(tour.Id, customer.Id);

        // Act
        await BookingWorkflow.NavigateToEdit(booking.Id);

        // Assert
        await Expect(Page.Locator(".alert-warning")).Not.ToBeVisibleAsync();
        await Expect(Page.Locator("#status")).ToBeEnabledAsync();
        await Expect(Page.Locator("#notes")).ToBeEnabledAsync();
        await Expect(Page.GetButton("Update Booking")).ToBeEnabledAsync();
        await Expect(Page.GetButton("Complete Booking")).ToBeVisibleAsync();
        await Expect(Page.GetButton("Cancel Booking")).ToBeVisibleAsync();
        await Expect(Page.GetButton("Delete Booking")).ToBeVisibleAsync();
        await Expect(Page.GetButton("Confirm Booking")).Not.ToBeVisibleAsync();
        await BookingEditStateTestHelpers.ExpectPaymentsSummaryVisible(Page);
    }
}
