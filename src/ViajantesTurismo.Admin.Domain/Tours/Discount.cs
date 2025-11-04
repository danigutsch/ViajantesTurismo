using JetBrains.Annotations;
using ViajantesTurismo.Common.Results;
using ViajantesTurismo.Common.Sanitizers;

namespace ViajantesTurismo.Admin.Domain.Tours;

/// <summary>
/// Represents a discount applied to a booking.
/// Value object that encapsulates discount type, amount, and optional reason.
/// </summary>
public sealed class Discount
{
    /// <summary>
    /// The maximum percentage discount allowed (100%).
    /// </summary>
    public const decimal MaxPercentageDiscount = 100m;

    private Discount(DiscountType type, decimal amount, string? reason)
    {
        Type = type;
        Amount = amount;
        Reason = reason;
    }

    [UsedImplicitly]
#pragma warning disable CS8618
    private Discount()
    {
    }
#pragma warning restore CS8618

    /// <summary>
    /// Gets an empty discount (0% discount) to represent no discount.
    /// </summary>
    public static Discount Empty => new(DiscountType.Percentage, 0m, null);

    /// <summary>
    /// Gets the type of discount (Percentage or Absolute).
    /// </summary>
    public DiscountType Type { get; private init; }

    /// <summary>
    /// Gets the discount amount.
    /// For Percentage: value between 0 and 100 (e.g., 15 = 15%).
    /// For Absolute: the fixed amount in the booking currency.
    /// </summary>
    public decimal Amount { get; private init; }

    /// <summary>
    /// Gets the optional reason or notes for the discount (e.g., "Early bird discount", "VIP customer").
    /// </summary>
    public string? Reason { get; private init; }

    /// <summary>
    /// Calculates the discount amount based on the subtotal and discount type.
    /// </summary>
    /// <param name="subtotal">The subtotal before discount.</param>
    /// <returns>The calculated discount amount to subtract from the subtotal.</returns>
    public decimal CalculateDiscountAmount(decimal subtotal)
    {
        return Type switch
        {
            DiscountType.Percentage => subtotal * (Amount / 100m),
            DiscountType.Absolute => Amount,
            _ => throw new InvalidOperationException($"Invalid discount type: {Type}")
        };
    }

    /// <summary>
    /// Creates a new discount with validation.
    /// If type is None, returns Discount.Empty regardless of amount.
    /// </summary>
    /// <param name="type">The discount type (None, Percentage, or Absolute).</param>
    /// <param name="amount">The discount amount.</param>
    /// <param name="reason">Optional reason for the discount.</param>
    /// <returns>A Result containing the Discount if validation succeeds, or errors if validation fails.</returns>
    public static Result<Discount> Create(
        DiscountType type,
        decimal amount,
        string? reason)
    {
        if (type == DiscountType.None)
        {
            return Empty;
        }

        var sanitizedAmount = NumericSanitizer.SanitizePrice(amount);
        var sanitizedReason = StringSanitizer.SanitizeNotes(reason);

        var errors = new ValidationErrors();

        if (!Enum.IsDefined(type))
        {
            errors.Add(DiscountErrors.InvalidDiscountType(type));
        }

        if (sanitizedAmount < 0)
        {
            errors.Add(DiscountErrors.NegativeDiscountAmount(sanitizedAmount));
        }

        if (type == DiscountType.Percentage && sanitizedAmount > MaxPercentageDiscount)
        {
            errors.Add(DiscountErrors.PercentageExceedsMaximum(sanitizedAmount, MaxPercentageDiscount));
        }

        if (errors.HasErrors)
        {
            return errors.ToResult<Discount>();
        }

        return new Discount(type, sanitizedAmount, sanitizedReason);
    }
}