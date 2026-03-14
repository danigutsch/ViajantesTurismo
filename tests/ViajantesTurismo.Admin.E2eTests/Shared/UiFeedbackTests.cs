namespace ViajantesTurismo.Admin.E2ETests.Shared;

public class UiFeedbackTests(E2EFixture fixture) : E2ETestBase(fixture)
{
    [Fact]
    public async Task Can_See_Toast_Notifications_And_Timed_Redirects()
    {
        // Arrange
        var tour = await ApiClient.CreateTour();
        var customer = await ApiClient.CreateCustomer();
        var booking = await ApiClient.CreateBooking(tour.Id, customer.Id);

        // Act
        await NavigateToBookingEdit(booking.Id);
        await Page.GetButton("Confirm Booking").ClickAsync();

        // Assert
        await ExpectToast("Booking confirmed successfully");

        // Act
        await Page.GetButton("Update Booking").ClickAsync();

        var redirectAlert = Page.Locator(".alert-info").Filter(new LocatorFilterOptions { HasText = "Redirecting" });
        await ExpectRedirectAlert(redirectAlert);
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

    private async Task ExpectToast(string expectedText)
    {
        var toast = Page.Locator(".toast.show");
        await Expect(toast).ToBeVisibleAsync();
        await Expect(toast).ToContainTextAsync(expectedText);
    }

    private async Task ExpectRedirectAlert(ILocator redirectAlert)
    {
        await Expect(redirectAlert).ToContainTextAsync("Redirecting to bookings page in 3 seconds...");
        await Expect(redirectAlert.GetButton("Cancel")).ToBeVisibleAsync();
    }
}
