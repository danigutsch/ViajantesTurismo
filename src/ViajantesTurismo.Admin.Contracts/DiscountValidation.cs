using System.ComponentModel.DataAnnotations;

namespace ViajantesTurismo.Admin.Contracts;

/// <summary>
/// Provides centralised discount validation for DTO validation.
/// </summary>
public static class DiscountValidation
{
    /// <summary>
    /// Validates all discount fields and returns any validation errors.
    /// </summary>
    /// <param name="discountType">The type of discount.</param>
    /// <param name="discountAmount">The discount amount.</param>
    /// <param name="discountReason">The reason for the discount.</param>
    /// <param name="maxPercentage">The maximum allowed percentage discount.</param>
    /// <param name="minReasonLength">The minimum required length for the reason.</param>
    /// <param name="discountAmountMemberName">The name of the discount amount property.</param>
    /// <param name="discountReasonMemberName">The name of the discount reason property.</param>
    /// <returns>An enumerable of validation results for any invalid fields.</returns>
    public static IEnumerable<ValidationResult> Validate(
        DiscountTypeDto discountType,
        decimal discountAmount,
        string? discountReason,
        int maxPercentage,
        int minReasonLength,
        string discountAmountMemberName,
        string discountReasonMemberName)
    {
        if (discountType == DiscountTypeDto.None)
        {
            yield break;
        }

        if (discountAmount <= 0)
        {
            yield return new ValidationResult(
                "Discount amount must be greater than 0 when a discount is applied.",
                [discountAmountMemberName]);
        }

        if (discountType == DiscountTypeDto.Percentage && discountAmount > maxPercentage)
        {
            yield return new ValidationResult(
                $"Percentage discount cannot exceed {maxPercentage}%.",
                [discountAmountMemberName]);
        }

        if (string.IsNullOrWhiteSpace(discountReason))
        {
            yield return new ValidationResult(
                "Discount reason is required when applying a discount.",
                [discountReasonMemberName]);
        }
        else if (discountReason.Length < minReasonLength)
        {
            yield return new ValidationResult(
                $"Discount reason must be at least {minReasonLength} characters.",
                [discountReasonMemberName]);
        }
    }
}
