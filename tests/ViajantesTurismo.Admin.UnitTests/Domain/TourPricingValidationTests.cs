using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.Admin.UnitTests.Domain;

/// <summary>Protects the currency and rounding contract of tour pricing values.</summary>
public sealed class TourPricingValidationTests
{
    [Theory]
    [InlineData((Currency)(-1))]
    [InlineData((Currency)999)]
    public void Rejects_undefined_currency_values(Currency currency)
    {
        // Act
        var result = TourPricing.Create(100, 10, 20, 30, currency);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.ErrorDetails.ShouldNotBeNull().ValidationErrors.ShouldNotBeNull().Keys.ShouldContain("currency");
    }

    [Theory]
    [InlineData(Currency.Real)]
    [InlineData(Currency.Euro)]
    [InlineData(Currency.UsDollar)]
    public void Preserves_defined_currencies_and_rounds_prices_away_from_zero(Currency currency)
    {
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
