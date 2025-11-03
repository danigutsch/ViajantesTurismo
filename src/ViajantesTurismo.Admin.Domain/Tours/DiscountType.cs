namespace ViajantesTurismo.Admin.Domain.Tours;

/// <summary>
/// Represents the type of discount applied to a booking.
/// </summary>
public enum DiscountType
{
    /// <summary>
    /// Discount is a percentage of the subtotal (0-100%).
    /// </summary>
    Percentage = 0,

    /// <summary>
    /// Discount is an absolute amount in the booking currency.
    /// </summary>
    Absolute = 1
}