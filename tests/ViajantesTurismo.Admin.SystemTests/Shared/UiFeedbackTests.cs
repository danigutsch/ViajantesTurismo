namespace ViajantesTurismo.Admin.SystemTests.Shared;

public class UiFeedbackTests(AspireSystemTestFixture fixture) : AspireSystemTestBase<AspireSystemTestFixture>(fixture)
{
    [Fact]
    public async Task Confirm_booking_should_show_success_toast()
    {
        // Arrange
        var tour = await ApiClient.CreateTour();
        var customer = await ApiClient.CreateCustomer();
        var booking = await ApiClient.CreateBooking(tour.Id, customer.Id);

        // Act
        await BookingWorkflow.NavigateToEdit(booking.Id);
        await Page.GetButton("Confirm Booking").ClickAsync();

        // Assert
        await UiFeedback.ExpectToast("Booking confirmed successfully");
    }

    [Fact]
    public async Task Updating_booking_should_show_redirect_alert_and_allow_cancelling_it()
    {
        // Arrange
        var tour = await ApiClient.CreateTour();
        var customer = await ApiClient.CreateCustomer();
        var booking = await ApiClient.CreateConfirmedBooking(tour.Id, customer.Id);

        // Act
        await BookingWorkflow.NavigateToEdit(booking.Id);
        await Page.GetButton("Update Booking").ClickAsync();

        var redirectAlert = Page.Locator(".alert-info").Filter(new LocatorFilterOptions { HasText = "Redirecting" });
        await UiFeedback.ExpectRedirectAlert();
        await redirectAlert.GetButton("Cancel").ClickAsync();

        // Assert
        await Expect(Page.GetButton("Go to Bookings")).ToBeVisibleAsync();
        await Expect(Page).ToHaveTitleAsync("Edit Booking");
    }
}
