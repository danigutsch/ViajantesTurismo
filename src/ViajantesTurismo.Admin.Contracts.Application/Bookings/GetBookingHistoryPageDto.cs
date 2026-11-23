namespace ViajantesTurismo.Admin.Contracts.Application;

/// <summary>A window of bookings for one tour or customer.</summary>
/// <param name="Items">The bookings in the requested window.</param>
/// <param name="Total">The number of bookings matching that owner.</param>
public sealed record GetBookingHistoryPageDto(IReadOnlyList<GetBookingDto> Items, int Total);
