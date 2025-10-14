namespace ViajantesTurismo.Admin.Domain.Bookings;

/// <summary>
/// Represents the status of a booking.
/// </summary>
public enum BookingStatus
{
    /// <summary>
    /// The booking is pending confirmation.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// The booking is confirmed.
    /// </summary>
    Confirmed = 1,

    /// <summary>
    /// The booking is cancelled.
    /// </summary>
    Cancelled = 2,

    /// <summary>
    /// The booking is completed.
    /// </summary>
    Completed = 3
}