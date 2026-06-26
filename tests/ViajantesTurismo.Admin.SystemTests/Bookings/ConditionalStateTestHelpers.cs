using static Microsoft.Playwright.Assertions;

namespace ViajantesTurismo.Admin.SystemTests.Bookings;

public static class ConditionalStateTestHelpers
{
    public static async Task ExpectTerminalBookingEditState(
        IPage page,
        Func<Guid, Task> navigateToEdit,
        Guid bookingId,
        string terminalStateText)
    {
        ArgumentNullException.ThrowIfNull(page);
        ArgumentNullException.ThrowIfNull(navigateToEdit);

        await navigateToEdit(bookingId);

        await Expect(page.Locator(".alert-warning")).ToContainTextAsync(terminalStateText);
        await Expect(page.Locator("#status")).ToBeDisabledAsync();
        await Expect(page.Locator("#notes")).ToBeDisabledAsync();
        await Expect(page.Locator("#discountType")).ToBeDisabledAsync();
        await Expect(page.GetButton("Update Booking")).ToBeDisabledAsync();
        await Expect(page.GetButton("Delete Booking")).ToBeEnabledAsync();
    }
}
