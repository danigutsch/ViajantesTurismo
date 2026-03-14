using Microsoft.Playwright;
using ViajantesTurismo.Admin.E2ETests.Infrastructure.Api;
using ViajantesTurismo.Admin.E2ETests.Infrastructure.Bases;
using ViajantesTurismo.Admin.E2ETests.Infrastructure.Fixtures;
using ViajantesTurismo.Admin.E2ETests.Infrastructure.Helpers;

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
        await NavigateTo($"/bookings/{booking.Id}/edit");
        await Expect(Page).ToHaveTitleAsync("Edit Booking");

        // Assert: confirming via the UI shows a toast.
        await Page.GetButton("Confirm Booking").ClickAsync();

        var toast = Page.Locator(".toast.show");
        await Expect(toast).ToBeVisibleAsync();
        await Expect(toast).ToContainTextAsync("Booking confirmed successfully");

        // Assert: updating the now-confirmed booking shows the timed redirect affordance.
        await Page.GetButton("Update Booking").ClickAsync();

        var redirectAlert = Page.Locator(".alert-info").Filter(new LocatorFilterOptions { HasText = "Redirecting" });
        await Expect(redirectAlert).ToContainTextAsync("Redirecting to bookings page in 3 seconds...");

        var cancelButton = redirectAlert.GetButton("Cancel");
        await Expect(cancelButton).ToBeVisibleAsync();

        // Act: cancel the redirect.
        await cancelButton.ClickAsync();

        // Assert: the page remains on the edit route and exposes the manual navigation action.
        await Expect(Page.GetButton("Go to Bookings")).ToBeVisibleAsync();
        await Expect(Page).ToHaveTitleAsync("Edit Booking");
    }
}
