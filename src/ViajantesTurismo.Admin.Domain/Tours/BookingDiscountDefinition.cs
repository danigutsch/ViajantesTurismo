namespace ViajantesTurismo.Admin.Domain.Tours;

/// <summary>
/// Defines the discount requested for a booking.
/// </summary>
/// <param name="Type">The discount type.</param>
/// <param name="Amount">The discount amount.</param>
/// <param name="Reason">The optional discount reason.</param>
public sealed record BookingDiscountDefinition(
    DiscountType Type,
    decimal Amount = 0m,
    string? Reason = null)
{
    /// <summary>
    /// Gets a discount definition representing no discount.
    /// </summary>
    public static BookingDiscountDefinition None { get; } = new(DiscountType.None);

    /// <summary>
    /// Creates a percentage discount definition.
    /// </summary>
    /// <param name="amount">The percentage amount.</param>
    /// <param name="reason">The optional discount reason.</param>
    /// <returns>A percentage discount definition.</returns>
    public static BookingDiscountDefinition Percentage(decimal amount, string? reason = null)
    {
        return new BookingDiscountDefinition(DiscountType.Percentage, amount, reason);
    }
}
