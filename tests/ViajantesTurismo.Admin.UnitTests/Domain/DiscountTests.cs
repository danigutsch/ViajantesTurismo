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
        (result.IsSuccess).ShouldBeFalse();
        (result.ErrorDetails).ShouldNotBeNull();
        (result.ErrorDetails.Detail).ShouldContain("Invalid discount type", StringComparison.Ordinal);
        (result.ErrorDetails.Detail).ShouldContain("999", StringComparison.Ordinal);
        (result.ErrorDetails.Detail).ShouldContain("Valid values are:", StringComparison.Ordinal);
        (result.ErrorDetails.ValidationErrors).ShouldNotBeNull();
        (result.ErrorDetails.ValidationErrors.ContainsKey("discountType")).ShouldBeTrue();
        (result.ErrorDetails.ValidationErrors["discountType"][0]).ShouldContain("Invalid discount type", StringComparison.Ordinal);
    }

    [Fact]
    public void Negative_discount_amount_should_return_invalid_result()
    {
        // Arrange
        const decimal negativeAmount = -10.50m;

        // Act
        var result = DiscountErrors.NegativeDiscountAmount(negativeAmount);

        // Assert
        (result.IsSuccess).ShouldBeFalse();
        (result.ErrorDetails).ShouldNotBeNull();
        (result.ErrorDetails.Detail).ShouldContain("Discount amount cannot be negative", StringComparison.Ordinal);
        (result.ErrorDetails.ValidationErrors).ShouldNotBeNull();
        (result.ErrorDetails.ValidationErrors.ContainsKey("discountAmount")).ShouldBeTrue();
        (result.ErrorDetails.ValidationErrors["discountAmount"][0]).ShouldContain("Discount amount cannot be negative.", StringComparison.Ordinal);
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
        (result.IsSuccess).ShouldBeFalse();
        (result.ErrorDetails).ShouldNotBeNull();
        (result.ErrorDetails.Detail).ShouldContain("Percentage discount cannot exceed", StringComparison.Ordinal);
        (result.ErrorDetails.ValidationErrors).ShouldNotBeNull();
        (result.ErrorDetails.ValidationErrors.ContainsKey("discountAmount")).ShouldBeTrue();
        (result.ErrorDetails.ValidationErrors["discountAmount"][0]).ShouldContain("cannot exceed", StringComparison.Ordinal);
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
        (result.IsSuccess).ShouldBeFalse();
        (result.ErrorDetails).ShouldNotBeNull();
        (result.ErrorDetails.Detail).ShouldContain("Absolute discount amount", StringComparison.Ordinal);
        (result.ErrorDetails.Detail).ShouldContain("cannot exceed subtotal", StringComparison.Ordinal);
        (result.ErrorDetails.ValidationErrors).ShouldNotBeNull();
        (result.ErrorDetails.ValidationErrors.ContainsKey("discountAmount")).ShouldBeTrue();
        (result.ErrorDetails.ValidationErrors["discountAmount"][0]).ShouldContain("Discount amount cannot exceed subtotal.", StringComparison.Ordinal);
    }

    [Fact]
    public void Final_price_not_positive_should_return_invalid_result()
    {
        // Arrange
        const decimal finalPrice = -5.00m;

        // Act
        var result = DiscountErrors.FinalPriceNotPositive(finalPrice);

        // Assert
        (result.IsSuccess).ShouldBeFalse();
        (result.ErrorDetails).ShouldNotBeNull();
        (result.ErrorDetails.Detail).ShouldContain("Final price after discount must be greater than zero", StringComparison.Ordinal);
        (result.ErrorDetails.ValidationErrors).ShouldNotBeNull();
        (result.ErrorDetails.ValidationErrors.ContainsKey("discount")).ShouldBeTrue();
        (result.ErrorDetails.ValidationErrors["discount"][0]).ShouldBe("Final price after discount must be greater than zero.");
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
        (result.IsSuccess).ShouldBeFalse();
        (result.ErrorDetails).ShouldNotBeNull();
        (result.ErrorDetails.Detail).ShouldContain("Discount reason must be at least", StringComparison.Ordinal);
        (result.ErrorDetails.Detail).ShouldContain("10", StringComparison.Ordinal);
        (result.ErrorDetails.ValidationErrors).ShouldNotBeNull();
        (result.ErrorDetails.ValidationErrors.ContainsKey("reason")).ShouldBeTrue();
        (result.ErrorDetails.ValidationErrors["reason"][0]).ShouldBe("Reason must be at least 10 characters.");
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
        (result.IsSuccess).ShouldBeFalse();
        (result.ErrorDetails).ShouldNotBeNull();
        (result.ErrorDetails.Detail).ShouldContain("Discount reason cannot exceed", StringComparison.Ordinal);
        (result.ErrorDetails.Detail).ShouldContain("500", StringComparison.Ordinal);
        (result.ErrorDetails.ValidationErrors).ShouldNotBeNull();
        (result.ErrorDetails.ValidationErrors.ContainsKey("reason")).ShouldBeTrue();
        (result.ErrorDetails.ValidationErrors["reason"][0]).ShouldBe("Reason cannot exceed 500 characters.");
    }
}
