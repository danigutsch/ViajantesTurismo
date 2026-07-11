using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.Admin.UnitTests.Domain;

public class DiscountTests
{
    [Fact]
    public void Invalid_discount_type_should_return_invalid_result()
    {
        // Arrange
        const DiscountType invalidType = (DiscountType)999;

        // Act
        var result = DiscountErrors.InvalidDiscountType(invalidType);

        // Assert
        TestAssert.False(result.IsSuccess);
        TestAssert.NotNull(result.ErrorDetails);
        TestAssert.Contains("Invalid discount type", result.ErrorDetails.Detail, StringComparison.Ordinal);
        TestAssert.Contains("999", result.ErrorDetails.Detail, StringComparison.Ordinal);
        TestAssert.Contains("Valid values are:", result.ErrorDetails.Detail, StringComparison.Ordinal);
        TestAssert.NotNull(result.ErrorDetails.ValidationErrors);
        TestAssert.True(result.ErrorDetails.ValidationErrors.ContainsKey("discountType"));
        TestAssert.Contains("Invalid discount type", result.ErrorDetails.ValidationErrors["discountType"][0], StringComparison.Ordinal);
    }

    [Fact]
    public void Negative_discount_amount_should_return_invalid_result()
    {
        // Arrange
        const decimal negativeAmount = -10.50m;

        // Act
        var result = DiscountErrors.NegativeDiscountAmount(negativeAmount);

        // Assert
        TestAssert.False(result.IsSuccess);
        TestAssert.NotNull(result.ErrorDetails);
        TestAssert.Contains("Discount amount cannot be negative", result.ErrorDetails.Detail, StringComparison.Ordinal);
        TestAssert.Contains("-10.50", result.ErrorDetails.Detail, StringComparison.Ordinal);
        TestAssert.NotNull(result.ErrorDetails.ValidationErrors);
        TestAssert.True(result.ErrorDetails.ValidationErrors.ContainsKey("discountAmount"));
        TestAssert.Contains("Discount amount cannot be negative.", result.ErrorDetails.ValidationErrors["discountAmount"][0], StringComparison.Ordinal);
    }

    [Fact]
    public void Percentage_exceeds_maximum_should_return_invalid_result()
    {
        // Arrange
        const decimal amount = 150m;
        const decimal maxPercentage = 100m;

        // Act
        var result = DiscountErrors.PercentageExceedsMaximum(amount, maxPercentage);

        // Assert
        TestAssert.False(result.IsSuccess);
        TestAssert.NotNull(result.ErrorDetails);
        TestAssert.Contains("Percentage discount cannot exceed", result.ErrorDetails.Detail, StringComparison.Ordinal);
        TestAssert.Contains("100%", result.ErrorDetails.Detail, StringComparison.Ordinal);
        TestAssert.Contains("150%", result.ErrorDetails.Detail, StringComparison.Ordinal);
        TestAssert.NotNull(result.ErrorDetails.ValidationErrors);
        TestAssert.True(result.ErrorDetails.ValidationErrors.ContainsKey("discountAmount"));
        TestAssert.Contains("cannot exceed 100%", result.ErrorDetails.ValidationErrors["discountAmount"][0], StringComparison.Ordinal);
    }

    [Fact]
    public void Absolute_discount_exceeds_subtotal_should_return_invalid_result()
    {
        // Arrange
        const decimal amount = 1000m;
        const decimal subtotal = 800m;

        // Act
        var result = DiscountErrors.AbsoluteDiscountExceedsSubtotal(amount, subtotal);

        // Assert
        TestAssert.False(result.IsSuccess);
        TestAssert.NotNull(result.ErrorDetails);
        TestAssert.Contains("Absolute discount amount", result.ErrorDetails.Detail, StringComparison.Ordinal);
        TestAssert.Contains("1000", result.ErrorDetails.Detail, StringComparison.Ordinal);
        TestAssert.Contains("800", result.ErrorDetails.Detail, StringComparison.Ordinal);
        TestAssert.Contains("cannot exceed subtotal", result.ErrorDetails.Detail, StringComparison.Ordinal);
        TestAssert.NotNull(result.ErrorDetails.ValidationErrors);
        TestAssert.True(result.ErrorDetails.ValidationErrors.ContainsKey("discountAmount"));
        TestAssert.Contains("Discount amount cannot exceed subtotal.", result.ErrorDetails.ValidationErrors["discountAmount"][0], StringComparison.Ordinal);
    }

    [Fact]
    public void Final_price_not_positive_should_return_invalid_result()
    {
        // Arrange
        const decimal finalPrice = -5.00m;

        // Act
        var result = DiscountErrors.FinalPriceNotPositive(finalPrice);

        // Assert
        TestAssert.False(result.IsSuccess);
        TestAssert.NotNull(result.ErrorDetails);
        TestAssert.Contains("Final price after discount must be greater than zero", result.ErrorDetails.Detail, StringComparison.Ordinal);
        TestAssert.Contains("-5", result.ErrorDetails.Detail, StringComparison.Ordinal);
        TestAssert.NotNull(result.ErrorDetails.ValidationErrors);
        TestAssert.True(result.ErrorDetails.ValidationErrors.ContainsKey("discount"));
        TestAssert.Equal("Final price after discount must be greater than zero.", result.ErrorDetails.ValidationErrors["discount"][0]);
    }

    [Fact]
    public void Reason_too_short_should_return_invalid_result()
    {
        // Arrange
        const int minLength = 10;
        const int actualLength = 5;

        // Act
        var result = DiscountErrors.ReasonTooShort(minLength, actualLength);

        // Assert
        TestAssert.False(result.IsSuccess);
        TestAssert.NotNull(result.ErrorDetails);
        TestAssert.Contains("Discount reason must be at least", result.ErrorDetails.Detail, StringComparison.Ordinal);
        TestAssert.Contains("10", result.ErrorDetails.Detail, StringComparison.Ordinal);
        TestAssert.NotNull(result.ErrorDetails.ValidationErrors);
        TestAssert.True(result.ErrorDetails.ValidationErrors.ContainsKey("reason"));
        TestAssert.Equal("Reason must be at least 10 characters.", result.ErrorDetails.ValidationErrors["reason"][0]);
    }

    [Fact]
    public void Reason_too_long_should_return_invalid_result()
    {
        // Arrange
        const int maxLength = 500;
        const int actualLength = 501;

        // Act
        var result = DiscountErrors.ReasonTooLong(maxLength, actualLength);

        // Assert
        TestAssert.False(result.IsSuccess);
        TestAssert.NotNull(result.ErrorDetails);
        TestAssert.Contains("Discount reason cannot exceed", result.ErrorDetails.Detail, StringComparison.Ordinal);
        TestAssert.Contains("500", result.ErrorDetails.Detail, StringComparison.Ordinal);
        TestAssert.NotNull(result.ErrorDetails.ValidationErrors);
        TestAssert.True(result.ErrorDetails.ValidationErrors.ContainsKey("reason"));
        TestAssert.Equal("Reason cannot exceed 500 characters.", result.ErrorDetails.ValidationErrors["reason"][0]);
    }
}
