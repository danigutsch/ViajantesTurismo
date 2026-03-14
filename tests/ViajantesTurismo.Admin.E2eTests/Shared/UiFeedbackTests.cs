using Microsoft.Playwright;
using ViajantesTurismo.Admin.E2ETests.Infrastructure.Bases;
using ViajantesTurismo.Admin.E2ETests.Infrastructure.Fixtures;
using ViajantesTurismo.Admin.E2ETests.Infrastructure.Helpers;

namespace ViajantesTurismo.Admin.E2ETests.Shared;

public class UiFeedbackTests(E2EFixture fixture) : E2ESerialTestBase(fixture)
{
    [Fact]
    public async Task Can_See_Toast_Notifications_And_Timed_Redirects()
    {
        // Navigate to a Pending booking's edit page
        await NavigateTo("/bookings");
        var pendingRow = Page.Locator("table tbody tr")
            .Filter(new LocatorFilterOptions { Has = Page.GetByText("Pending", new PageGetByTextOptions { Exact = true }) }).First;
        await pendingRow.GetLink("Edit").ClickAsync();
        await Expect(Page).ToHaveTitleAsync("Edit Booking");

        // === Toast notification via Confirm Booking ===
        await Page.GetButton("Confirm Booking").ClickAsync();

        // Toast should appear with success message
        var toast = Page.Locator(".toast.show");
        await Expect(toast).ToBeVisibleAsync();
        await Expect(toast).ToContainTextAsync("Booking confirmed successfully");

        // === Redirect countdown via Update Booking ===
        // Now update the (now confirmed) booking to trigger redirect
        await Page.GetButton("Update Booking").ClickAsync();

        // Redirect countdown should appear
        var redirectAlert = Page.Locator(".alert-info").Filter(new LocatorFilterOptions { HasText = "Redirecting" });
        await Expect(redirectAlert).ToContainTextAsync("Redirecting to bookings page in 3 seconds...");

        // Cancel button should be visible in the redirect alert
        var cancelButton = redirectAlert.GetButton("Cancel");
        await Expect(cancelButton).ToBeVisibleAsync();

        // Click Cancel to stop redirect
        await cancelButton.ClickAsync();

        // After cancelling, "Go to Bookings" button should appear
        await Expect(Page.GetButton("Go to Bookings")).ToBeVisibleAsync();

        // Verify we are still on the edit page (redirect was cancelled)
        await Expect(Page).ToHaveTitleAsync("Edit Booking");
    }

    [Fact]
    public async Task Bookings_Overview_Shows_Correct_Summary_Badges()
    {
        // Seed data: 10 bookings — 2 Pending, 6 Confirmed, 1 Cancelled, 1 Completed
        await NavigateTo("/bookings");
        await Expect(Page).ToHaveTitleAsync("Bookings");

        // Badges are in the card-header
        var header = Page.Locator(".card-header");

        // Total badge
        await Expect(header.Locator(".badge.bg-secondary")).ToContainTextAsync("Total: 10");

        // Pending badge
        await Expect(header.Locator(".badge.bg-warning")).ToContainTextAsync("Pending: 2");

        // Confirmed badge
        await Expect(header.Locator(".badge.bg-success")).ToContainTextAsync("Confirmed: 6");
    }
}
