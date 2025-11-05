using Reqnroll;
using ViajantesTurismo.Admin.BehaviorTests.Context;
using ViajantesTurismo.Admin.Domain.Tours;
using ViajantesTurismo.Common.BuildingBlocks;

namespace ViajantesTurismo.Admin.BehaviorTests.Steps;

[Binding]
public sealed class TourUpdateCurrencySteps(TourContext tourContext)
{
    [Given(@"a tour exists with currency ""(.*)""")]
    public void GivenATourExistsWithCurrency(string currencyCode)
    {
        var currency = TestHelpers.ParseCurrency(currencyCode);
        tourContext.Tour = Tour.Create(
            identifier: "TEST2024",
            name: "Test Tour",
            schedule: DateRange.Create(DateTime.UtcNow.AddMonths(1), DateTime.UtcNow.AddMonths(1).AddDays(7)).Value,
            pricing: TourPricing.Create(2000.00m, 500.00m, 100.00m, 200.00m, currency).Value,
            capacity: TourCapacity.Create(4, 12).Value,
            includedServices: ["Hotel", "Breakfast"]).Value;
    }

    [When(@"I update the currency to ""(.*)""")]
    public void WhenIUpdateTheCurrencyTo(string currencyCode)
    {
        var currency = TestHelpers.ParseCurrency(currencyCode);
        tourContext.Tour.UpdateCurrency(currency);
    }

    [Then(@"the tour should have currency ""(.*)""")]
    public void ThenTheTourShouldHaveCurrency(string expectedCurrencyCode)
    {
        var expectedCurrency = TestHelpers.ParseCurrency(expectedCurrencyCode);
        Assert.Equal(expectedCurrency, tourContext.Tour.Currency);
    }
}
