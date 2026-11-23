namespace ViajantesTurismo.Admin.Contracts.Application;

/// <summary>Counts across all bookings visible to the overview.</summary>
/// <param name="Total">The total number of bookings.</param>
/// <param name="Pending">The number of pending bookings.</param>
/// <param name="Confirmed">The number of confirmed bookings.</param>
/// <param name="Completed">The number of completed bookings.</param>
/// <param name="Cancelled">The number of cancelled bookings.</param>
public sealed record BookingOverviewCountsDto(int Total, int Pending, int Confirmed, int Completed, int Cancelled);
