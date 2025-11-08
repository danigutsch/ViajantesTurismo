using Reqnroll;
using ViajantesTurismo.Admin.BehaviorTests.Context;
using ViajantesTurismo.Admin.Domain.Tours;

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
            startDate: DateTime.UtcNow.AddMonths(1),
            endDate: DateTime.UtcNow.AddMonths(1).AddDays(7),
            basePrice: 2000.00m,
            doubleRoomSupplementPrice: 500.00m,
            regularBikePrice: 100.00m,
            eBikePrice: 200.00m,
            currency: currency,
            minCustomers: 4,
            maxCustomers: 12,
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
        Assert.Equal(expectedCurrency, tourContext.Tour.Pricing.Currency);
    }
}