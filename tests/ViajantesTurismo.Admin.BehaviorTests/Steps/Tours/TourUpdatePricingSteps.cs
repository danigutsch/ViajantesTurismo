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
        (tourContext.UpdateResult).ShouldNotBeNull();
        (tourContext.UpdateResult.Value.IsSuccess).ShouldBeTrue($"Expected success but got error: {tourContext.UpdateResult.Value.ErrorDetails?.Detail}");
    }

    [Then("the tour pricing update should fail")]
    public void ThenTheTourPricingUpdateShouldFail()
    {
        (tourContext.UpdateResult).ShouldNotBeNull();
        (tourContext.UpdateResult.Value.IsSuccess).ShouldBeFalse();
    }

    [Then("the tour should have single room supplement (.*)")]
    public void ThenTheTourShouldHaveSingleRoomSupplement(decimal expectedPrice)
    {
        (tourContext.Tour.Pricing.SingleRoomSupplementPrice).ShouldBe(expectedPrice);
    }

    [Then("the tour should have regular bike price (.*)")]
    public void ThenTheTourShouldHaveRegularBikePrice(decimal expectedPrice)
    {
        (tourContext.Tour.Pricing.RegularBikePrice).ShouldBe(expectedPrice);
    }

    [Then("the tour should have e-bike price (.*)")]
    public void ThenTheTourShouldHaveEBikePrice(decimal expectedPrice)
    {
        (tourContext.Tour.Pricing.EBikePrice).ShouldBe(expectedPrice);
    }
}
