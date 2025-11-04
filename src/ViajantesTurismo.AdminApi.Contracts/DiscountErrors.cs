using System.ComponentModel.DataAnnotations;

namespace ViajantesTurismo.AdminApi.Contracts;

/// <summary>
/// Provides centralised discount validation errors for DTO validation.
/// </summary>
public static class DiscountErrors
{
    private const string AmountMustBePositiveMessage = "Discount amount must be greater than 0 when a discount is applied.";
    private const string ReasonRequiredMessage = "Discount reason is required when applying a discount.";

    private static readonly string[] DiscountAmountMembers = ["DiscountAmount"];
    private static readonly string[] DiscountReasonMembers = ["DiscountReason"];

    /// <summary>
    /// Gets the validation result for when the discount amount is not positive.
    /// </summary>
    public static ValidationResult AmountMustBePositive() =>
        new(AmountMustBePositiveMessage, DiscountAmountMembers);

    /// <summary>
    /// Gets the validation result for when a discount reason is not provided.
    /// </summary>
    public static ValidationResult ReasonRequired() =>
        new(ReasonRequiredMessage, DiscountReasonMembers);

    /// <summary>
    /// Gets the validation result for when the percentage discount exceeds maximum.
    /// </summary>
    /// <param name="maxPercentage">The maximum allowed percentage.</param>
    public static ValidationResult PercentageTooHigh(int maxPercentage) =>
        new($"Percentage discount cannot exceed {maxPercentage}%.", DiscountAmountMembers);

    /// <summary>
    /// Gets the validation result for when the discount reason is too short.
    /// </summary>
    /// <param name="minLength">The minimum required length.</param>
    public static ValidationResult ReasonTooShort(int minLength) =>
        new($"Discount reason must be at least {minLength} characters.", DiscountReasonMembers);
}