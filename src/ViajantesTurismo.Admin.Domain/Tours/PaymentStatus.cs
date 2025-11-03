namespace ViajantesTurismo.Admin.Domain.Tours;

/// <summary>
/// Represents the payment status of a booking.
/// </summary>
public enum PaymentStatus
{
    /// <summary>
    /// The booking is unpaid.
    /// </summary>
    Unpaid = 0,

    /// <summary>
    /// The booking is partially paid.
    /// </summary>
    PartiallyPaid = 1,

    /// <summary>
    /// The booking is fully paid.
    /// </summary>
    Paid = 2,

    /// <summary>
    /// The booking has been refunded.
    /// </summary>
    Refunded = 3
}