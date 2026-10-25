using Microsoft.Playwright;
using ViajantesTurismo.Admin.SystemTests.Infrastructure.Helpers;
using static Microsoft.Playwright.Assertions;

namespace ViajantesTurismo.Admin.SystemTests.Bookings;

public static class ConditionalStateTestHelpers
{
    public static async Task ExpectTerminalBookingEditState(
        IPage page,
        Func<Guid, Task> navigateToEdit,
        Guid bookingId,
        string terminalStateText,
        string statusBadgeText)
    {
        ArgumentNullException.ThrowIfNull(page);
        ArgumentNullException.ThrowIfNull(navigateToEdit);

        await navigateToEdit(bookingId);

        await Expect(page.Locator(".alert-warning")).ToContainTextAsync(terminalStateText);
        await Expect(page.Locator(".booking-status-badge")).ToContainTextAsync(statusBadgeText);
        await Expect(page.Locator("#notes")).ToBeEnabledAsync();
        await Expect(page.Locator("#discountType")).ToBeDisabledAsync();
        await Expect(page.GetButton("Update Booking")).ToBeEnabledAsync();
        await Expect(page.GetButton("Delete Booking")).Not.ToBeVisibleAsync();
    }
}
