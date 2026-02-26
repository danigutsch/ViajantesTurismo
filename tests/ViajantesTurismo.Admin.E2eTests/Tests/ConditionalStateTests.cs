using Microsoft.Playwright;

namespace ViajantesTurismo.Admin.E2ETests.Tests;

public class ConditionalStateTests(E2EFixture fixture) : E2ETestBase(fixture)
{
    [Fact]
    public async Task Tour_Edit_Disables_Locked_Fields_When_Bookings_Exist()
    {
        // Navigate to tours list and find a tour that has bookings (City Highlights = tours[0], has bookings)
        await NavigateToAsync("/tours");
        var tourRow = Page.Locator("table tbody tr")
            .Filter(new LocatorFilterOptions { HasText = "City Highlights" });
        await tourRow.GetLink("Edit").ClickAsync();
        await Expect(Page).ToHaveTitleAsync("Edit Tour");

        // Identifier and Currency should be disabled
        var identifier = Page.Locator("#identifier");
        await Expect(identifier).ToBeDisabledAsync();

        var currency = Page.Locator("#currency");
        await Expect(currency).ToBeDisabledAsync();

        // Warning alert should be visible
        await Expect(Page.GetByRole(AriaRole.Alert)).ToContainTextAsync("existing bookings");

        // Non-locked fields should remain editable
        await Expect(Page.Locator("#name")).ToBeEnabledAsync();
        await Expect(Page.Locator("#startDate")).ToBeEnabledAsync();
        await Expect(Page.Locator("#endDate")).ToBeEnabledAsync();
        await Expect(Page.Locator("#price")).ToBeEnabledAsync();
        await Expect(Page.Locator("#services")).ToBeEnabledAsync();
    }

    [Fact]
    public async Task Booking_Edit_Disables_All_Fields_For_Terminal_States()
    {
        // === Cancelled booking: all form fields disabled ===
        await NavigateToAsync("/bookings");
        var cancelledRow = Page.Locator("table tbody tr")
            .Filter(new LocatorFilterOptions { Has = Page.Locator(".badge:has-text('Cancelled')") }).First;
        await cancelledRow.GetLink("Edit").ClickAsync();
        await Expect(Page).ToHaveTitleAsync("Edit Booking");

        // Warning alert visible
        var cancelledWarning = Page.Locator(".alert-warning");
        await Expect(cancelledWarning).ToContainTextAsync("cancelled");

        // Form inputs disabled
        await Expect(Page.Locator("#status")).ToBeDisabledAsync();
        await Expect(Page.Locator("#notes")).ToBeDisabledAsync();
        await Expect(Page.Locator("#discountType")).ToBeDisabledAsync();

        // Update button disabled
        await Expect(Page.GetButton("Update Booking")).ToBeDisabledAsync();

        // Delete button still enabled
        await Expect(Page.GetButton("Delete Booking")).ToBeEnabledAsync();

        // Cancel and Confirm action buttons should NOT be visible
        await Expect(Page.GetButton("Cancel Booking")).Not.ToBeVisibleAsync();
        await Expect(Page.GetButton("Confirm Booking")).Not.ToBeVisibleAsync();

        // === Completed booking: same disabled behavior ===
        await NavigateToAsync("/bookings");
        var completedRow = Page.Locator("table tbody tr")
            .Filter(new LocatorFilterOptions { Has = Page.Locator(".badge:has-text('Completed')") }).First;
        await completedRow.GetLink("Edit").ClickAsync();
        await Expect(Page).ToHaveTitleAsync("Edit Booking");

        // Warning alert visible
        var completedWarning = Page.Locator(".alert-warning");
        await Expect(completedWarning).ToContainTextAsync("completed");

        // Form inputs disabled
        await Expect(Page.Locator("#status")).ToBeDisabledAsync();
        await Expect(Page.Locator("#notes")).ToBeDisabledAsync();
        await Expect(Page.Locator("#discountType")).ToBeDisabledAsync();

        // Update button disabled
        await Expect(Page.GetButton("Update Booking")).ToBeDisabledAsync();

        // Delete button still enabled
        await Expect(Page.GetButton("Delete Booking")).ToBeEnabledAsync();

        // === Pending booking: fields should be ENABLED ===
        await NavigateToAsync("/bookings");
        var pendingRow = Page.Locator("table tbody tr")
            .Filter(new LocatorFilterOptions { Has = Page.Locator(".badge.bg-warning") }).First;
        await pendingRow.GetLink("Edit").ClickAsync();
        await Expect(Page).ToHaveTitleAsync("Edit Booking");

        await Expect(Page.Locator("#status")).ToBeEnabledAsync();
        await Expect(Page.Locator("#notes")).ToBeEnabledAsync();
        await Expect(Page.Locator("#discountType")).ToBeEnabledAsync();
        await Expect(Page.GetButton("Update Booking")).ToBeEnabledAsync();
    }
}
