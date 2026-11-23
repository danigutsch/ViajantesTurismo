namespace ViajantesTurismo.Admin.Contracts.Application;

/// <summary>A bounded page of booking summaries and the overview counts.</summary>
/// <param name="Items">The booking summaries in the requested order.</param>
/// <param name="Counts">The overview counts across all bookings.</param>
public sealed record GetBookingPageDto(IReadOnlyList<GetBookingDto> Items, BookingOverviewCountsDto Counts);
