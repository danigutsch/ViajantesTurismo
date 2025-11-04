using Reqnroll;
using ViajantesTurismo.Admin.BehaviorTests.Context;
using ViajantesTurismo.Admin.Domain.Tours;
using ViajantesTurismo.Common.Monies;
using ViajantesTurismo.Common.Results;

namespace ViajantesTurismo.Admin.BehaviorTests.Steps;

[Binding]
public sealed class TourUpdateBasePriceSteps(TourContext tourContext)
{
    [Given(@"a tour exists with base price (.*)")]
    public void GivenATourExistsWithBasePrice(decimal basePrice)
    {
        tourContext.Tour = Tour.Create(
            identifier: "TEST2024",
            name: "Test Tour",
            startDate: DateTime.UtcNow.AddMonths(1),
            endDate: DateTime.UtcNow.AddMonths(1).AddDays(7),
            price: basePrice,
            doubleRoomSupplementPrice: 500.00m,
            regularBikePrice: 100.00m,
            eBikePrice: 200.00m,
            currency: Currency.UsDollar,
            includedServices: ["Hotel", "Breakfast"],
            minCustomers: 4,
            maxCustomers: 12).Value;
    }

    [When(@"I update the base price to (.*)")]
    public void WhenIUpdateTheBasePriceTo(decimal newPrice)
    {
        tourContext.Result = tourContext.Tour.UpdateBasePrice(newPrice);
    }

    [Then(@"the tour base price update should succeed")]
    public void ThenTheTourBasePriceUpdateShouldSucceed()
    {
        var result = (Result)tourContext.Result;
        Assert.True(result.IsSuccess, $"Expected success but got error: {result.ErrorDetails?.Detail}");
    }

    [Then(@"the tour base price update should fail")]
    public void ThenTheTourBasePriceUpdateShouldFail()
    {
        var result = (Result)tourContext.Result;
        Assert.False(result.IsSuccess);
    }

    [Then(@"the tour should have base price (.*)")]
    public void ThenTheTourShouldHaveBasePrice(decimal expectedPrice)
    {
        Assert.Equal(expectedPrice, tourContext.Tour.Price);
    }
}