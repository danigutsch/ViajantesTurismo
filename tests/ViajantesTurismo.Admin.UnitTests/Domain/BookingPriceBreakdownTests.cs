using ViajantesTurismo.Admin.Domain;
using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.Admin.UnitTests.Domain;

/// <summary>Protects booking price composition and discount acceptance without extra monetary rounding.</summary>
public sealed class BookingPriceBreakdownTests
{
    [Theory]
    [InlineData(DiscountType.None, 0, 0, 160)]
    [InlineData(DiscountType.Percentage, 12.5, 20, 140)]
    [InlineData(DiscountType.Absolute, 25, 25, 135)]
    public void Composes_base_room_and_both_bikes_before_applying_the_discount(DiscountType type, decimal amount, decimal expectedDiscount, decimal expectedTotal)
    {
        // Arrange
        var discount = Discount.Create(type, amount, "Seasonal booking offer").Value;

        // Act
        var result = BookingPriceBreakdown.Create(100, 10, 20, 30, discount);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var breakdown = result.Value;
        breakdown.Subtotal.ShouldBe(160m);
        breakdown.DiscountAmount.ShouldBe(expectedDiscount);
        breakdown.TotalPrice.ShouldBe(expectedTotal);
    }

    [Fact]
    public void Keeps_fractional_discount_precision_until_the_existing_financial_boundary()
    {
        // Arrange
        var discount = Discount.Create(DiscountType.Percentage, 12.5m, "Seasonal booking offer").Value;

        // Act
        var result = BookingPriceBreakdown.Create(100.01m, 0, 0, 0, discount);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var breakdown = result.Value;
        breakdown.DiscountAmount.ShouldBe(12.50125m);
        breakdown.TotalPrice.ShouldBe(87.50875m);
    }

    [Theory]
    [InlineData(100, "Final price after discount must be greater than zero.")]
    [InlineData(101, "Absolute discount amount cannot exceed subtotal.")]
    public void Rejects_a_free_or_overdiscounted_booking_without_clamping(decimal discountAmount, string expectedDetail)
    {
        // Arrange
        var discount = Discount.Create(DiscountType.Absolute, discountAmount, "Seasonal booking offer").Value;

        // Act
        var validation = BookingPriceBreakdown.Create(100, 0, 0, 0, discount);

        // Assert
        validation.IsFailure.ShouldBeTrue();
        validation.ErrorDetails.ShouldNotBeNull().Detail.ShouldBe(expectedDetail);
    }

    [Fact]
    public void Identical_price_compositions_have_value_equality()
    {
        // Arrange
        var discount = Discount.Create(DiscountType.Percentage, 10, "Seasonal booking offer").Value;
        var first = BookingPriceBreakdown.Create(100, 10, 20, 30, discount).Value;

        // Act
        var second = BookingPriceBreakdown.Create(100, 10, 20, 30, discount).Value;

        // Assert
        second.ShouldBe(first);
        new HashSet<BookingPriceBreakdown> { first, second }.Count.ShouldBe(1);
    }

    [Theory]
    [InlineData(0, 0, 0, 0)]
    [InlineData(-1, 0, 0, 0)]
    [InlineData(AdminDomainLimits.MaxPrice + 1, 0, 0, 0)]
    [InlineData(100, -1, 0, 0)]
    [InlineData(100, AdminDomainLimits.MaxPrice + 1, 0, 0)]
    [InlineData(100, 0, -1, 0)]
    [InlineData(100, 0, AdminDomainLimits.MaxPrice + 1, 0)]
    [InlineData(100, 0, 0, -1)]
    [InlineData(100, 0, 0, AdminDomainLimits.MaxPrice + 1)]
    public void Rejects_price_components_outside_existing_booking_limits(decimal basePrice, decimal roomPrice, decimal principalBikePrice, decimal companionBikePrice)
    {
        // Arrange
        var discount = Discount.Create(DiscountType.None, 0, null).Value;

        // Act
        var result = BookingPriceBreakdown.Create(basePrice, roomPrice, principalBikePrice, companionBikePrice, discount);

        // Assert
        result.IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void Accepts_zero_optional_costs_and_a_price_at_the_existing_upper_limit()
    {
        // Arrange
        var discount = Discount.Create(DiscountType.None, 0, null).Value;

        // Act
        var result = BookingPriceBreakdown.Create(AdminDomainLimits.MaxPrice, 0, 0, 0, discount);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.TotalPrice.ShouldBe(AdminDomainLimits.MaxPrice);
    }
}
