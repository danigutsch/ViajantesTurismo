using Microsoft.Playwright;
using ViajantesTurismo.Admin.E2ETests.Infrastructure.Bases;
using ViajantesTurismo.Admin.E2ETests.Infrastructure.Fixtures;
using ViajantesTurismo.Admin.E2ETests.Infrastructure.Helpers;

namespace ViajantesTurismo.Admin.E2ETests.Bookings;

public class BookingEditStateTests(E2EFixture fixture) : E2ETestBase(fixture)
{
    [Fact]
    public async Task Cancelled_Booking_Edit_Hides_Payment_And_Action_Buttons()
    {
        // Navigate to a Cancelled booking's edit page
        await NavigateTo("/bookings");
        var cancelledRow = Page.Locator("table tbody tr")
            .Filter(new LocatorFilterOptions { Has = Page.Locator(".badge:has-text('Cancelled')") }).First;
        await cancelledRow.GetLink("Edit").ClickAsync();
        await Expect(Page).ToHaveTitleAsync("Edit Booking");

        // Warning message displayed
        var warning = Page.Locator(".alert-warning");
        await Expect(warning).ToContainTextAsync("cancelled");
        await Expect(warning).ToContainTextAsync("cannot be modified");

        // Record Payment button should NOT be visible for cancelled bookings
        await Expect(Page.GetButton("Record Payment")).Not.ToBeVisibleAsync();

        // Payment summary section visible (Payments card)
        var paymentsCard = Page.Locator(".card").Filter(new LocatorFilterOptions { HasText = "Payments" });
        await Expect(paymentsCard.GetByText("Total Price", new LocatorGetByTextOptions { Exact = true })).ToBeVisibleAsync();
        await Expect(paymentsCard.GetByText("Amount Paid", new LocatorGetByTextOptions { Exact = true })).ToBeVisibleAsync();
        await Expect(paymentsCard.GetByText("Remaining Balance", new LocatorGetByTextOptions { Exact = true })).ToBeVisibleAsync();

        // Confirm and Complete action buttons hidden
        await Expect(Page.GetButton("Confirm Booking")).Not.ToBeVisibleAsync();
        await Expect(Page.GetButton("Complete Booking")).Not.ToBeVisibleAsync();
        // Cancel Booking button hidden (already cancelled)
        await Expect(Page.GetButton("Cancel Booking")).Not.ToBeVisibleAsync();

        // Delete button still available
        await Expect(Page.GetButton("Delete Booking")).ToBeEnabledAsync();
    }

    [Fact]
    public async Task Confirmed_Booking_Edit_Shows_Action_Buttons_And_Payment_Section()
    {
        // Navigate to a Confirmed booking's edit page
        await NavigateTo("/bookings");
        var confirmedRow = Page.Locator("table tbody tr")
            .Filter(new LocatorFilterOptions { Has = Page.Locator(".booking-status-badge.bg-success") }).First;
        await confirmedRow.GetLink("Edit").ClickAsync();
        await Expect(Page).ToHaveTitleAsync("Edit Booking");

        // No warning for confirmed bookings
        await Expect(Page.Locator(".alert-warning")).Not.ToBeVisibleAsync();

        // Form fields are editable
        await Expect(Page.Locator("#status")).ToBeEnabledAsync();
        await Expect(Page.Locator("#notes")).ToBeEnabledAsync();
        await Expect(Page.GetButton("Update Booking")).ToBeEnabledAsync();

        // Complete and Cancel action buttons visible
        await Expect(Page.GetButton("Complete Booking")).ToBeVisibleAsync();
        await Expect(Page.GetButton("Cancel Booking")).ToBeVisibleAsync();
        await Expect(Page.GetButton("Delete Booking")).ToBeVisibleAsync();

        // Confirm button NOT visible (already confirmed)
        await Expect(Page.GetButton("Confirm Booking")).Not.ToBeVisibleAsync();

        // Payment summary section rendered (Payments card)
        var paymentsCard = Page.Locator(".card").Filter(new LocatorFilterOptions { HasText = "Payments" });
        await Expect(paymentsCard.GetByText("Total Price", new LocatorGetByTextOptions { Exact = true })).ToBeVisibleAsync();
        await Expect(paymentsCard.GetByText("Amount Paid", new LocatorGetByTextOptions { Exact = true })).ToBeVisibleAsync();
        await Expect(paymentsCard.GetByText("Remaining Balance", new LocatorGetByTextOptions { Exact = true })).ToBeVisibleAsync();
    }
}
