using System.Text.RegularExpressions;

namespace ViajantesTurismo.Admin.E2ETests.Tests;

public class BookingDeleteAndDialogTests(E2EFixture fixture) : E2ETestBase(fixture)
{
    [Fact]
    public async Task Can_Cancel_Booking_Via_Confirm_Dialog()
    {
        // Create own tour, customer, and confirmed booking via API
        var api = Fixture.ApiClient;
        var tour = await ApiTestHelper.CreateTourAsync(api);
        var customer = await ApiTestHelper.CreateCustomerAsync(api);
        var booking = await ApiTestHelper.CreateBookingAsync(api, tour.Id, customer.Id);
        await ApiTestHelper.ConfirmBookingAsync(api, booking.Id);

        // Navigate directly to the created booking's edit page
        await NavigateToAsync($"/bookings/{booking.Id}/edit");
        await Expect(Page).ToHaveTitleAsync("Edit Booking");

        // Cancel Booking button should be visible for confirmed bookings
        var cancelButton = Page.GetButton("Cancel Booking");
        await Expect(cancelButton).ToBeVisibleAsync();

        // Click Cancel Booking — confirm dialogue appears
        await cancelButton.ClickAsync();

        // Verify dialog content
        var dialog = Page.Locator(".modal.show");
        await Expect(dialog).ToBeVisibleAsync();
        await Expect(dialog.Locator(".modal-title")).ToContainTextAsync("Cancel Booking");
        await Expect(dialog.GetButton("Yes, Cancel")).ToBeVisibleAsync();
        await Expect(dialog.GetButton("No")).ToBeVisibleAsync();

        // Click "No" — booking should remain unchanged
        await dialog.GetButton("No").ClickAsync();
        await Expect(dialog).Not.ToBeVisibleAsync();

        // Verify status is still Confirmed
        var statusSelect = Page.Locator("#status");
        await Expect(statusSelect).ToBeEnabledAsync();

        // Now actually cancel: click Cancel Booking again and confirm
        await cancelButton.ClickAsync();
        var dialog2 = Page.Locator(".modal.show");
        await Expect(dialog2).ToBeVisibleAsync();
        await dialog2.GetButton("Yes, Cancel").ClickAsync();

        // After cancellation, the warning alert should appear
        var cancelledWarning = Page.Locator(".alert-warning");
        await Expect(cancelledWarning).ToContainTextAsync("cancelled");

        // Form fields should be disabled after cancellation
        await Expect(Page.Locator("#status")).ToBeDisabledAsync();
        await Expect(Page.Locator("#notes")).ToBeDisabledAsync();

        // Cancel Booking button should no longer be visible
        await Expect(Page.GetButton("Cancel Booking")).Not.ToBeVisibleAsync();

        // Delete button should still be visible
        await Expect(Page.GetButton("Delete Booking")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Can_Delete_Booking_Via_Confirm_Dialog()
    {
        // Create own tour, customer, and pending booking via API
        var api = Fixture.ApiClient;
        var tour = await ApiTestHelper.CreateTourAsync(api);
        var customer = await ApiTestHelper.CreateCustomerAsync(api);
        var booking = await ApiTestHelper.CreateBookingAsync(api, tour.Id, customer.Id);

        // Navigate directly to the created booking's edit page
        await NavigateToAsync($"/bookings/{booking.Id}/edit");
        await Expect(Page).ToHaveTitleAsync("Edit Booking");

        // Delete button should be visible
        var deleteButton = Page.GetButton("Delete Booking");
        await Expect(deleteButton).ToBeVisibleAsync();

        // Click Delete — confirm dialogue appears
        await deleteButton.ClickAsync();
        var dialog = Page.Locator(".modal.show");
        await Expect(dialog).ToBeVisibleAsync();
        await Expect(dialog.Locator(".modal-title")).ToContainTextAsync("Delete Booking");
        await Expect(dialog.GetByText("cannot be undone")).ToBeVisibleAsync();

        // Click "No" — booking should remain
        await dialog.GetButton("No").ClickAsync();
        await Expect(dialog).Not.ToBeVisibleAsync();
        await Expect(Page.GetButton("Delete Booking")).ToBeVisibleAsync();

        // Click Delete again and confirm this time
        await deleteButton.ClickAsync();
        var dialog2 = Page.Locator(".modal.show");
        await Expect(dialog2).ToBeVisibleAsync();
        await dialog2.GetButton("Yes, Delete").ClickAsync();

        // Should redirect to /bookings
        await Expect(Page).ToHaveURLAsync(new Regex("/bookings$"));
        await Expect(Page).ToHaveTitleAsync("Bookings");

        // Deleted booking should not appear in the list
        var deletedLink = Page.Locator($"a[href='/bookings/{booking.Id}']");
        await Expect(deletedLink).ToHaveCountAsync(0);
    }
}
