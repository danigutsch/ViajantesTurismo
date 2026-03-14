using ViajantesTurismo.Admin.Contracts;

namespace ViajantesTurismo.Admin.E2ETests.Infrastructure.Pages;

/// <summary>
/// Provides deterministic access to rows in the global bookings list without scanning paginator pages.
/// It uses the live API booking order to jump directly to the page that should contain a known booking.
/// This avoids paginator scanning while still tolerating concurrent test inserts that can shift page boundaries.
/// </summary>
/// <param name="page">The active Playwright page.</param>
/// <param name="navigateTo">Navigation function that resolves relative application routes.</param>
/// <param name="getAllBookings">Function that retrieves the current ordered bookings list from the API.</param>
internal sealed class BookingsListPage(
    IPage page,
    Func<string, Task> navigateTo,
    Func<Task<GetBookingDto[]>> getAllBookings)
{
    private const int ItemsPerPage = 10;
    private const int MaxLookupAttempts = 3;

    /// <summary>
    /// Reads the booking status badge for a known booking from the global bookings list.
    /// </summary>
    /// <param name="bookingId">The booking identifier to locate.</param>
    /// <returns>The trimmed booking status text shown in the grid.</returns>
    public async Task<string> GetBookingStatus(Guid bookingId)
    {
        var row = await GetBookingRow(bookingId);
        var statusBadge = row.Locator("td:nth-child(7) .badge");
        await statusBadge.WaitForAsync();
        return (await statusBadge.InnerTextAsync()).Trim();
    }

    /// <summary>
    /// Reads the payment status badge for a known booking from the global bookings list.
    /// </summary>
    /// <param name="bookingId">The booking identifier to locate.</param>
    /// <returns>The trimmed payment status text shown in the grid.</returns>
    public async Task<string> GetPaymentStatus(Guid bookingId)
    {
        var row = await GetBookingRow(bookingId);
        var paymentBadge = row.Locator("td:nth-child(8) .badge");
        await paymentBadge.WaitForAsync();
        return (await paymentBadge.InnerTextAsync()).Trim();
    }

    /// <summary>
    /// Returns the grid row for a known booking after navigating to the page that should contain it.
    /// </summary>
    /// <param name="bookingId">The booking identifier to locate.</param>
    /// <returns>The matching bookings table row.</returns>
    public async Task<ILocator> GetBookingRow(Guid bookingId)
    {
        var href = $"/bookings/{bookingId}";

        for (var attempt = 0; attempt < MaxLookupAttempts; attempt++)
        {
            var allBookings = await getAllBookings();
            var bookingIndex = FindBookingIndex(allBookings, bookingId);

            await navigateTo("/bookings");
            Assert.Equal("Bookings", await page.TitleAsync());
            await NavigateToPageContaining(bookingIndex);

            var row = page.Locator($"table tbody tr:has(a[href='{href}'])");
            if (await row.CountAsync() > 0)
            {
                return row.First;
            }
        }

        throw new InvalidOperationException(
            $"Booking row '{href}' could not be found after {MaxLookupAttempts} deterministic lookup attempt(s).");
    }

    /// <summary>
    /// Finds the zero-based position of a booking in the API-provided ordering used by the bookings page.
    /// </summary>
    /// <param name="allBookings">The ordered bookings collection.</param>
    /// <param name="bookingId">The booking identifier to find.</param>
    /// <returns>The zero-based index of the booking in the ordered list.</returns>
    private static int FindBookingIndex(GetBookingDto[] allBookings, Guid bookingId)
    {
        for (var index = 0; index < allBookings.Length; index++)
        {
            if (allBookings[index].Id == bookingId)
            {
                return index;
            }
        }

        throw new InvalidOperationException($"Booking '{bookingId}' was not found in the API bookings list.");
    }

    /// <summary>
    /// Advances the paginator directly to the page containing the target booking index.
    /// </summary>
    /// <param name="bookingIndex">The zero-based booking index from the API ordering.</param>
    private async Task NavigateToPageContaining(int bookingIndex)
    {
        var targetPageIndex = bookingIndex / ItemsPerPage;
        if (targetPageIndex == 0)
        {
            return;
        }

        var nextButton = page.Locator(".paginator button[aria-label='Go to next page']");
        for (var currentPageIndex = 0; currentPageIndex < targetPageIndex; currentPageIndex++)
        {
            var firstBookingLink = page.Locator("table tbody tr a[href^='/bookings/']").First;
            var previousHref = await firstBookingLink.GetAttributeAsync("href");
            Assert.NotNull(previousHref);

            await nextButton.ClickAsync();
            await page.WaitForFunctionAsync(
                "([selector, href]) => { const element = document.querySelector(selector); return element && element.getAttribute('href') !== href; }",
                new object[] { "table tbody tr a[href^='/bookings/']", previousHref });
        }
    }
}
