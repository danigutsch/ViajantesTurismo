namespace ViajantesTurismo.Admin.E2ETests.Shared;

public class UiFeedbackTests(E2EFixture fixture) : E2ETestBase(fixture)
{
    [Fact]
    public async Task Confirm_Booking_Should_Show_Success_Toast()
    {
        // Arrange
        var tour = await ApiClient.CreateTour();
        var customer = await ApiClient.CreateCustomer();
        var booking = await ApiClient.CreateBooking(tour.Id, customer.Id);

        // Act
        await NavigateToBookingEdit(booking.Id);
        await Page.GetButton("Confirm Booking").ClickAsync();

        // Assert
        await UiFeedback.ExpectToast("Booking confirmed successfully");
    }

    [Fact]
    public async Task Updating_Booking_Should_Show_Redirect_Alert_And_Allow_Cancelling_It()
    {
        // Arrange
        var tour = await ApiClient.CreateTour();
        var customer = await ApiClient.CreateCustomer();
        var booking = await ApiClient.CreateConfirmedBooking(tour.Id, customer.Id);

        // Act
        await NavigateToBookingEdit(booking.Id);
        await Page.GetButton("Update Booking").ClickAsync();

        var redirectAlert = Page.Locator(".alert-info").Filter(new LocatorFilterOptions { HasText = "Redirecting" });
        await UiFeedback.ExpectRedirectAlert();
        await redirectAlert.GetButton("Cancel").ClickAsync();

        // Assert
        await Expect(Page.GetButton("Go to Bookings")).ToBeVisibleAsync();
        await Expect(Page).ToHaveTitleAsync("Edit Booking");
    }

    private async Task NavigateToBookingEdit(Guid bookingId)
    {
        await NavigateTo($"/bookings/{bookingId}/edit");
        await Expect(Page).ToHaveTitleAsync("Edit Booking");
    }
}
