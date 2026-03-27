using ViajantesTurismo.Common.Monies;

namespace ViajantesTurismo.Admin.BehaviorTests.Steps.Tours;

[Binding]
public sealed class TourUpdateBasePriceSteps(TourContext tourContext)
{
    [Given("a tour exists with base price (.*)")]
    public void GivenATourExistsWithBasePrice(decimal basePrice)
    {
        tourContext.Tour = Tour.Create(new TourDefinition(
            "TEST2024",
            "Test Tour",
            DateTime.UtcNow.AddMonths(1),
            DateTime.UtcNow.AddMonths(1).AddDays(7),
            basePrice,
            500.00m,
            100.00m,
            200.00m,
            Currency.UsDollar,
            4,
            12,
            ["Hotel", "Breakfast"])).Value;
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
