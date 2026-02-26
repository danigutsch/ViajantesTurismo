using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace ViajantesTurismo.Admin.E2ETests.Tests;

public class BookingDeleteAndDialogTests(E2EFixture fixture) : E2ETestBase(fixture)
{
    [Fact]
    public async Task Can_Cancel_Booking_Via_Confirm_Dialog()
    {
        // Find a Confirmed booking and navigate to its edit page
        await NavigateToAsync("/bookings");
        var confirmedRow = Page.Locator("table tbody tr")
            .Filter(new LocatorFilterOptions { Has = Page.Locator(".badge.bg-success") }).First;
        await confirmedRow.GetLink("Edit").ClickAsync();
        await Expect(Page).ToHaveTitleAsync("Edit Booking");

        // Cancel Booking button should be visible for confirmed bookings
        var cancelButton = Page.GetButton("Cancel Booking");
        await Expect(cancelButton).ToBeVisibleAsync();

        // Click Cancel Booking — confirm dialog appears
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
        // Find a Pending booking to delete (least impact on other tests)
        await NavigateToAsync("/bookings");
        var pendingRow = Page.Locator("table tbody tr")
            .Filter(new LocatorFilterOptions { Has = Page.Locator(".badge.bg-warning") }).First;

        // Get the booking view link for later verification
        var viewLink = pendingRow.GetLink("View");
        var bookingHref = await viewLink.GetAttributeAsync("href");
        Assert.NotNull(bookingHref);

        await pendingRow.GetLink("Edit").ClickAsync();
        await Expect(Page).ToHaveTitleAsync("Edit Booking");

        // Delete button should be visible
        var deleteButton = Page.GetButton("Delete Booking");
        await Expect(deleteButton).ToBeVisibleAsync();

        // Click Delete — confirm dialog appears
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
        var deletedLink = Page.Locator($"a[href='{bookingHref}']");
        await Expect(deletedLink).ToHaveCountAsync(0);
    }
}
