using Reqnroll;
using ViajantesTurismo.Admin.BehaviorTests.Context;
using ViajantesTurismo.Common.Monies;
using ViajantesTurismo.Common.Results;

namespace ViajantesTurismo.Admin.BehaviorTests.Steps;

[Binding]
public sealed class TourUpdatePricingSteps(TourContext tourContext)
{
    [Given(@"a tour exists with pricing setup")]
    public void GivenATourExistsWithPricingSetup()
    {
        tourContext.Tour = TestHelpers.CreateTestTour();
    }

    [When(@"I update the pricing to double room supplement (.*), regular bike (.*), e-bike (.*), and currency ""(.*)""")]
    public void WhenIUpdateThePricingToDoubleRoomSupplementRegularBikeEBikeAndCurrency(
        decimal doubleRoomSupplement,
        decimal regularBike,
        decimal eBike,
        string currencyCode)
    {
        var currency = currencyCode switch
        {
            "USD" => Currency.UsDollar,
            "EUR" => Currency.Euro,
            "BRL" => Currency.Real,
            _ => throw new ArgumentException($"Unknown currency: {currencyCode}")
        };

        tourContext.Result = tourContext.Tour.UpdatePricing(
            doubleRoomSupplement,
            regularBike,
            eBike,
            currency);
    }

    [Then(@"the tour pricing update should succeed")]
    public void ThenTheTourPricingUpdateShouldSucceed()
    {
        var result = (Result)tourContext.Result;
        Assert.True(result.IsSuccess, $"Expected success but got error: {result.ErrorDetails?.Detail}");
    }

    [Then(@"the tour pricing update should fail")]
    public void ThenTheTourPricingUpdateShouldFail()
    {
        var result = (Result)tourContext.Result;
        Assert.False(result.IsSuccess);
    }

    [Then(@"the tour should have double room supplement (.*)")]
    public void ThenTheTourShouldHaveDoubleRoomSupplement(decimal expectedPrice)
    {
        Assert.Equal(expectedPrice, tourContext.Tour.Pricing.DoubleRoomSupplementPrice);
    }

    [Then(@"the tour should have regular bike price (.*)")]
    public void ThenTheTourShouldHaveRegularBikePrice(decimal expectedPrice)
    {
        Assert.Equal(expectedPrice, tourContext.Tour.Pricing.RegularBikePrice);
    }

    [Then(@"the tour should have e-bike price (.*)")]
    public void ThenTheTourShouldHaveEBikePrice(decimal expectedPrice)
    {
        Assert.Equal(expectedPrice, tourContext.Tour.Pricing.EBikePrice);
    }
}
