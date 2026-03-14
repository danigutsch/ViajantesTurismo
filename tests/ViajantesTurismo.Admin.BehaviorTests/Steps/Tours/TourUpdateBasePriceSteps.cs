using ViajantesTurismo.Common.Monies;

namespace ViajantesTurismo.Admin.BehaviorTests.Steps.Tours;

[Binding]
public sealed class TourUpdateBasePriceSteps(TourContext tourContext)
{
    [Given("a tour exists with base price (.*)")]
    public void GivenATourExistsWithBasePrice(decimal basePrice)
    {
        tourContext.Tour = Tour.Create(
            identifier: "TEST2024",
            name: "Test Tour",
            startDate: DateTime.UtcNow.AddMonths(1),
            endDate: DateTime.UtcNow.AddMonths(1).AddDays(7),
            basePrice: basePrice,
            singleRoomSupplementPrice: 500.00m,
            regularBikePrice: 100.00m,
            eBikePrice: 200.00m,
            currency: Currency.UsDollar,
            minCustomers: 4,
            maxCustomers: 12,
            includedServices: ["Hotel", "Breakfast"]).Value;
    }

    [When("I update the base price to (.*)")]
    public void WhenIUpdateTheBasePriceTo(decimal newPrice)
    {
        tourContext.UpdateResult = tourContext.Tour.UpdateBasePrice(newPrice);
    }

    [Then("the tour base price update should succeed")]
    public void ThenTheTourBasePriceUpdateShouldSucceed()
    {
        Assert.NotNull(tourContext.UpdateResult);
        Assert.True(tourContext.UpdateResult.Value.IsSuccess,
            $"Expected success but got error: {tourContext.UpdateResult.Value.ErrorDetails?.Detail}");
    }

    [Then("the tour base price update should fail")]
    public void ThenTheTourBasePriceUpdateShouldFail()
    {
        Assert.NotNull(tourContext.UpdateResult);
        Assert.False(tourContext.UpdateResult.Value.IsSuccess);
    }

    [Then("the tour should have base price (.*)")]
    public void ThenTheTourShouldHaveBasePrice(decimal expectedPrice)
    {
        Assert.Equal(expectedPrice, tourContext.Tour.Pricing.BasePrice);
    }
}
