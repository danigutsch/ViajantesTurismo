using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Admin.SystemTests.Infrastructure.Pages;

/// <summary>
/// Provides booking-specific access to deterministic global-list rows.
/// </summary>
/// <param name="page">The active Playwright page.</param>
/// <param name="navigateTo">Navigation function that resolves relative application routes.</param>
/// <param name="getAllBookings">Function that retrieves the current ordered bookings list from the API.</param>
internal sealed class BookingsListPage(
    IPage page,
    Func<string, Task> navigateTo,
    Func<Task<GetBookingDto[]>> getAllBookings)
{
    private readonly PagedEntityListPage<GetBookingDto> bookings = new(
        page,
        navigateTo,
        getAllBookings,
        static booking => booking.Id,
        "/bookings",
        "/bookings",
        "Bookings");

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
        return await bookings.GetRow(bookingId);
    }
}
