using SharedKernel.Testing;
using Microsoft.Playwright;
using ViajantesTurismo.HostedTests.Admin.System.Infrastructure.Bases;
using ViajantesTurismo.HostedTests.Admin.System.Infrastructure.Fixtures;
using ViajantesTurismo.HostedTests.Admin.System.Infrastructure.Helpers;
using ViajantesTurismo.Admin.Testing.Integration.Helpers;
using SharedKernel.Testing.Playwright;
namespace ViajantesTurismo.HostedTests.Admin.System.Shared;

[TestScope(TestScope.System)]
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
        var toastTask = UiFeedback.ExpectToast("Booking confirmed successfully");
        await Page.GetButton("Confirm Booking").ClickAsync();

        // Assert
        await toastTask;
    }

    [Fact]
    public async Task Updating_booking_should_show_redirect_alert_and_allow_cancelling_it()
    {
        // Arrange
        var tour = await ApiClient.CreateTour();
        var customer = await ApiClient.CreateCustomer();
        var booking = await ApiClient.CreateConfirmedBooking(tour.Id, customer.Id);

        await BookingWorkflow.NavigateToEdit(booking.Id);
        var redirectAlert = UiFeedback.GetRedirectAlert("Redirecting to bookings page in 3 seconds...");

        // Act
        var cancelRedirect = redirectAlert.GetByRole(AriaRole.Button,
            new LocatorGetByRoleOptions { Name = "Cancel", Exact = true }).ClickAsync();
        await Task.WhenAll(cancelRedirect, Page.GetButton("Update Booking").ClickAsync());

        // Assert
        await Expect(Page.GetButton("Go to Bookings")).ToBeVisibleAsync();
        await Expect(redirectAlert).ToBeHiddenAsync();
        await Page.ShouldHaveTitle("Edit Booking");
    }
}
