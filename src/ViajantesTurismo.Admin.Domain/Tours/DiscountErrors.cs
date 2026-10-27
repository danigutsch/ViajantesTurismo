using PandaBear.Results;

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
    /// <returns>A Result representing the error.</returns>
    public static Result NegativeDiscountAmount() =>
        Result.Invalid(
            detail: "Discount amount cannot be negative.",
            field: "discountAmount",
            message: "Discount amount cannot be negative.");

    /// <summary>
    /// Indicates that a percentage discount exceeds the maximum allowed value.
    /// </summary>
    /// <returns>A Result representing the error.</returns>
    public static Result PercentageExceedsMaximum() =>
        Result.Invalid(
            detail: "Percentage discount cannot exceed the maximum allowed value.",
            field: "discountAmount",
            message: "Percentage discount cannot exceed the maximum allowed value.");

    /// <summary>
    /// Indicates that an absolute discount amount exceeds the subtotal.
    /// </summary>
    /// <returns>A Result representing the error.</returns>
    public static Result AbsoluteDiscountExceedsSubtotal() =>
        Result.Invalid(
            detail: "Absolute discount amount cannot exceed subtotal.",
            field: "discountAmount",
            message: "Discount amount cannot exceed subtotal.");

    /// <summary>
    /// Indicates that the final price after discount would be zero or negative.
    /// </summary>
    /// <returns>A Result representing the error.</returns>
    public static Result FinalPriceNotPositive() =>
        Result.Invalid(
            detail: "Final price after discount must be greater than zero.",
            field: "discount",
            message: "Final price after discount must be greater than zero.");

    /// <summary>
    /// Indicates that the discount reason is too short.
    /// </summary>
    /// <param name="minLength">The minimum required length.</param>
    /// <param name="actualLength">The actual length provided.</param>
    /// <returns>A Result representing the error.</returns>
    public static Result ReasonTooShort(int minLength, int actualLength) => Result.Invalid(
        detail: $"Discount reason must be at least {minLength} characters long. Received: {actualLength} characters.",
        field: "reason",
        message: $"Reason must be at least {minLength} characters.");

    /// <summary>
    /// Indicates that the discount reason is too long.
    /// </summary>
    /// <param name="maxLength">The maximum allowed length.</param>
    /// <param name="actualLength">The actual length provided.</param>
    /// <returns>A Result representing the error.</returns>
    public static Result ReasonTooLong(int maxLength, int actualLength) => Result.Invalid(
        detail: $"Discount reason cannot exceed {maxLength} characters. Received: {actualLength} characters.",
        field: "reason",
        message: $"Reason cannot exceed {maxLength} characters.");
}
