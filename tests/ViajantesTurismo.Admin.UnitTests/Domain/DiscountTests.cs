using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.Admin.UnitTests.Domain;

public class DiscountTests
{
    [Fact]
    public void Invalid_Discount_Type_Should_Return_Invalid_Result()
    {
        // Arrange
        const DiscountType invalidType = (DiscountType)999;

        // Act
        var result = DiscountErrors.InvalidDiscountType(invalidType);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.ErrorDetails);
        Assert.Contains("Invalid discount type", result.ErrorDetails.Detail);
        Assert.Contains("999", result.ErrorDetails.Detail);
        Assert.Contains("Valid values are:", result.ErrorDetails.Detail);
        Assert.NotNull(result.ErrorDetails.ValidationErrors);
        Assert.True(result.ErrorDetails.ValidationErrors.ContainsKey("discountType"));
        Assert.Contains("Invalid discount type", result.ErrorDetails.ValidationErrors["discountType"][0]);
    }

    [Fact]
    public void Invalid_Discount_Type_Should_Include_All_Valid_Values()
    {
        // Arrange
        const DiscountType invalidType = (DiscountType)999;

        // Act
        var result = DiscountErrors.InvalidDiscountType(invalidType);

        // Assert
        Assert.NotNull(result.ErrorDetails);
        var allValidTypes = Enum.GetNames<DiscountType>();
        foreach (var validType in allValidTypes)
        {
            Assert.Contains(validType, result.ErrorDetails.Detail);
        }
    }

    [Fact]
    public void Negative_Discount_Amount_Should_Return_Invalid_Result()
    {
        // Arrange
        const decimal negativeAmount = -10.50m;

        // Act
        var result = DiscountErrors.NegativeDiscountAmount(negativeAmount);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.ErrorDetails);
        Assert.Contains("Discount amount cannot be negative", result.ErrorDetails.Detail);
        Assert.Contains("-10.50", result.ErrorDetails.Detail);
        Assert.NotNull(result.ErrorDetails.ValidationErrors);
        Assert.True(result.ErrorDetails.ValidationErrors.ContainsKey("discountAmount"));
        Assert.Equal("Discount amount cannot be negative.", result.ErrorDetails.ValidationErrors["discountAmount"][0]);
    }

    [Fact]
    public void Percentage_Exceeds_Maximum_Should_Return_Invalid_Result()
    {
        // Arrange
        const decimal amount = 150m;
        const decimal maxPercentage = 100m;

        // Act
        var result = DiscountErrors.PercentageExceedsMaximum(amount, maxPercentage);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.ErrorDetails);
        Assert.Contains("Percentage discount cannot exceed", result.ErrorDetails.Detail);
        Assert.Contains("100%", result.ErrorDetails.Detail);
        Assert.Contains("150%", result.ErrorDetails.Detail);
        Assert.NotNull(result.ErrorDetails.ValidationErrors);
        Assert.True(result.ErrorDetails.ValidationErrors.ContainsKey("discountAmount"));
        Assert.Contains("cannot exceed 100%", result.ErrorDetails.ValidationErrors["discountAmount"][0]);
    }

    [Fact]
    public void Absolute_Discount_Exceeds_Subtotal_Should_Return_Invalid_Result()
    {
        // Arrange
        const decimal amount = 1000m;
        const decimal subtotal = 800m;

        // Act
        var result = DiscountErrors.AbsoluteDiscountExceedsSubtotal(amount, subtotal);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.ErrorDetails);
        Assert.Contains("Absolute discount amount", result.ErrorDetails.Detail);
        Assert.Contains("1000", result.ErrorDetails.Detail);
        Assert.Contains("800", result.ErrorDetails.Detail);
        Assert.Contains("cannot exceed subtotal", result.ErrorDetails.Detail);
        Assert.NotNull(result.ErrorDetails.ValidationErrors);
        Assert.True(result.ErrorDetails.ValidationErrors.ContainsKey("discountAmount"));
        Assert.Equal("Discount amount cannot exceed subtotal.", result.ErrorDetails.ValidationErrors["discountAmount"][0]);
    }

    [Fact]
    public void Final_Price_Not_Positive_Should_Return_Invalid_Result()
    {
        // Arrange
        const decimal finalPrice = -5.00m;

        // Act
        var result = DiscountErrors.FinalPriceNotPositive(finalPrice);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.ErrorDetails);
        Assert.Contains("Final price after discount must be greater than zero", result.ErrorDetails.Detail);
        Assert.Contains("-5", result.ErrorDetails.Detail);
        Assert.NotNull(result.ErrorDetails.ValidationErrors);
        Assert.True(result.ErrorDetails.ValidationErrors.ContainsKey("discount"));
        Assert.Equal("Final price after discount must be greater than zero.", result.ErrorDetails.ValidationErrors["discount"][0]);
    }

    [Fact]
    public void Final_Price_Not_Positive_With_Zero_Should_Return_Invalid_Result()
    {
        // Arrange
        const decimal finalPrice = 0m;

        // Act
        var result = DiscountErrors.FinalPriceNotPositive(finalPrice);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.ErrorDetails);
        Assert.Contains("Final price after discount must be greater than zero", result.ErrorDetails.Detail);
        Assert.Contains("0", result.ErrorDetails.Detail);
    }

    [Fact]
    public void Reason_Too_Short_Should_Return_Invalid_Result()
    {
        // Arrange
        const int minLength = 10;
        const int actualLength = 5;

        // Act
        var result = DiscountErrors.ReasonTooShort(minLength, actualLength);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.ErrorDetails);
        Assert.Contains("Discount reason must be at least", result.ErrorDetails.Detail);
        Assert.Contains("10", result.ErrorDetails.Detail);
        Assert.NotNull(result.ErrorDetails.ValidationErrors);
        Assert.True(result.ErrorDetails.ValidationErrors.ContainsKey("reason"));
        Assert.Equal("Reason must be at least 10 characters.", result.ErrorDetails.ValidationErrors["reason"][0]);
    }

    [Fact]
    public void Reason_Too_Long_Should_Return_Invalid_Result()
    {
        // Arrange
        const int maxLength = 500;
        const int actualLength = 501;

        // Act
        var result = DiscountErrors.ReasonTooLong(maxLength, actualLength);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.ErrorDetails);
        Assert.Contains("Discount reason cannot exceed", result.ErrorDetails.Detail);
        Assert.Contains("500", result.ErrorDetails.Detail);
        Assert.NotNull(result.ErrorDetails.ValidationErrors);
        Assert.True(result.ErrorDetails.ValidationErrors.ContainsKey("reason"));
        Assert.Equal("Reason cannot exceed 500 characters.", result.ErrorDetails.ValidationErrors["reason"][0]);
    }
}
