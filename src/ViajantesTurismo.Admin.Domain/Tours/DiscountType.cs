namespace ViajantesTurismo.Admin.Domain.Tours;

/// <summary>
/// Represents the type of discount applied to a booking.
/// </summary>
public enum DiscountType
{
    /// <summary>
    /// No discount applied.
    /// </summary>
    None = 0,

    /// <summary>
    /// Discount is a percentage of the subtotal (0-100%).
    /// </summary>
    Percentage = 1,

    /// <summary>
    /// Discount is an absolute amount in the booking currency.
    /// </summary>
    Absolute = 2
}
