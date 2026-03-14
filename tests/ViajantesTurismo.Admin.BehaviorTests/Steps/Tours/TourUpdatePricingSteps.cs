using Reqnroll;
using ViajantesTurismo.Admin.BehaviorTests.Context;
using ViajantesTurismo.Admin.Tests.Shared.Behavior;
using ViajantesTurismo.Common.Monies;

namespace ViajantesTurismo.Admin.BehaviorTests.Steps.Tours;

[Binding]
public sealed class TourUpdatePricingSteps(TourContext tourContext)
{
    [Given("a tour exists with pricing setup")]
    public void GivenATourExistsWithPricingSetup()
    {
        tourContext.Tour = EntityBuilders.BuildTour();
    }

    [When(@"I update the pricing to single room supplement (.*), regular bike (.*), e-bike (.*), and currency ""(.*)""")]
    public void WhenIUpdateThePricingToSingleRoomSupplementRegularBikeEBikeAndCurrency(
        decimal singleRoomSupplement,
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

        tourContext.UpdateResult = tourContext.Tour.UpdatePricing(
            singleRoomSupplement,
            regularBike,
            eBike,
            currency);
    }

    [Then("the tour pricing update should succeed")]
    public void ThenTheTourPricingUpdateShouldSucceed()
    {
        Assert.NotNull(tourContext.UpdateResult);
        Assert.True(tourContext.UpdateResult.Value.IsSuccess,
            $"Expected success but got error: {tourContext.UpdateResult.Value.ErrorDetails?.Detail}");
    }

    [Then("the tour pricing update should fail")]
    public void ThenTheTourPricingUpdateShouldFail()
    {
        Assert.NotNull(tourContext.UpdateResult);
        Assert.False(tourContext.UpdateResult.Value.IsSuccess);
    }

    [Then("the tour should have single room supplement (.*)")]
    public void ThenTheTourShouldHaveSingleRoomSupplement(decimal expectedPrice)
    {
        Assert.Equal(expectedPrice, tourContext.Tour.Pricing.SingleRoomSupplementPrice);
    }

    [Then("the tour should have regular bike price (.*)")]
    public void ThenTheTourShouldHaveRegularBikePrice(decimal expectedPrice)
    {
        Assert.Equal(expectedPrice, tourContext.Tour.Pricing.RegularBikePrice);
    }

    [Then("the tour should have e-bike price (.*)")]
    public void ThenTheTourShouldHaveEBikePrice(decimal expectedPrice)
    {
        Assert.Equal(expectedPrice, tourContext.Tour.Pricing.EBikePrice);
    }
}
