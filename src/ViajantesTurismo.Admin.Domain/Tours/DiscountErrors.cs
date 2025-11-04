using ViajantesTurismo.Common.Results;

namespace ViajantesTurismo.Admin.Domain.Tours;

/// <summary>
/// Provides predefined discount-related error results.
/// </summary>
public static class DiscountErrors
{
    /// <summary>
    /// Indicates that an invalid discount type value was provided.
    /// </summary>
    /// <param name="discountType">The invalid discount type value.</param>
    /// <returns>A Result representing the error.</returns>
    public static Result InvalidDiscountType(DiscountType discountType) =>
        Result.Invalid(
            detail: $"Invalid discount type: {discountType}. Valid values are: {string.Join(", ", Enum.GetNames<DiscountType>())}.",
            field: "discountType",
            message: $"Invalid discount type: {discountType}.");

    /// <summary>
    /// Indicates that the discount amount is negative.
    /// </summary>
    /// <param name="amount">The invalid discount amount.</param>
    /// <returns>A Result representing the error.</returns>
    public static Result NegativeDiscountAmount(decimal amount) =>
        Result.Invalid(
            detail: $"Discount amount cannot be negative. Received: {amount}.",
            field: "discountAmount",
            message: "Discount amount cannot be negative.");

    /// <summary>
    /// Indicates that a percentage discount exceeds the maximum allowed value.
    /// </summary>
    /// <param name="amount">The invalid percentage amount.</param>
    /// <param name="maxPercentage">The maximum allowed percentage.</param>
    /// <returns>A Result representing the error.</returns>
    public static Result PercentageExceedsMaximum(decimal amount, decimal maxPercentage) =>
        Result.Invalid(
            detail: $"Percentage discount cannot exceed {maxPercentage}%. Received: {amount}%.",
            field: "discountAmount",
            message: $"Percentage discount cannot exceed {maxPercentage}%.");

    /// <summary>
    /// Indicates that an absolute discount amount exceeds the subtotal.
    /// </summary>
    /// <param name="amount">The discount amount.</param>
    /// <param name="subtotal">The subtotal before discount.</param>
    /// <returns>A Result representing the error.</returns>
    public static Result AbsoluteDiscountExceedsSubtotal(decimal amount, decimal subtotal) =>
        Result.Invalid(
            detail: $"Absolute discount amount ({amount}) cannot exceed subtotal ({subtotal}).",
            field: "discountAmount",
            message: "Discount amount cannot exceed subtotal.");

    /// <summary>
    /// Indicates that the final price after discount would be zero or negative.
    /// </summary>
    /// <param name="finalPrice">The calculated final price.</param>
    /// <returns>A Result representing the error.</returns>
    public static Result FinalPriceNotPositive(decimal finalPrice) =>
        Result.Invalid(
            detail: $"Final price after discount must be greater than zero. Calculated: {finalPrice}.",
            field: "discount",
            message: "Final price after discount must be greater than zero.");
}