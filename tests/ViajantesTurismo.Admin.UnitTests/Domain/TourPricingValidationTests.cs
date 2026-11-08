using PandaBear.BuildingBlocks;
using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.Admin.UnitTests.Domain;

/// <summary>Protects the currency and rounding contract of tour pricing values.</summary>
public sealed class TourPricingValidationTests
{
    [Theory]
    [InlineData("XXX")]
    [InlineData("JPY")]
    public void Rejects_currencies_a_tour_cannot_be_priced_in(string code)
    {
        // Arrange
        var currency = Currency.Restore(code);

        // Act
        var result = TourPricing.Create(100, 10, 20, 30, currency);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.ErrorDetails.ShouldNotBeNull().ValidationErrors.ShouldNotBeNull().Keys.ShouldContain("currency");
    }

    [Theory]
    [InlineData("BRL")]
    [InlineData("EUR")]
    [InlineData("USD")]
    public void Preserves_defined_currencies_and_rounds_prices_away_from_zero(string code)
    {
        // Arrange
        var currency = Currency.Create(code).Value;

        // Act
        var result = TourPricing.Create(100.005m, 10.005m, 20.005m, 30.005m, currency);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Currency.ShouldBe(currency);
        result.Value.BasePrice.ShouldBe(100.01m);
        result.Value.SingleRoomSupplementPrice.ShouldBe(10.01m);
        result.Value.RegularBikePrice.ShouldBe(20.01m);
        result.Value.EBikePrice.ShouldBe(30.01m);
    }
}
