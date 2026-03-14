using Reqnroll;
using ViajantesTurismo.Admin.BehaviorTests.Context;
using ViajantesTurismo.Admin.Domain.Customers;
using ViajantesTurismo.Admin.Domain.Tours;
using ViajantesTurismo.Admin.Tests.Shared.Behavior;

namespace ViajantesTurismo.Admin.BehaviorTests.Steps.Tours;

[Binding]
public sealed class TourUpdateCurrencySteps(TourContext tourContext)
{
    [Given(@"a tour exists with currency ""(.*)"" and has (\d+) booking")]
    public void GivenATourExistsWithCurrencyAndHasBooking(string currencyCode, int bookingCount)
    {
        GivenATourExistsWithCurrency(currencyCode);
        for (var i = 0; i < bookingCount; i++)
        {
            tourContext.Tour.AddBooking(
                Guid.CreateVersion7(), BikeType.Regular, null, null,
                RoomType.DoubleOccupancy, DiscountType.None, 0m, null, null);
        }
    }

    [Given(@"a tour exists with currency ""(.*)""")]
    public void GivenATourExistsWithCurrency(string currencyCode)
    {
        var currency = EntityBuilders.ParseCurrency(currencyCode);
        tourContext.Tour = Tour.Create(
            identifier: "TEST2024",
            name: "Test Tour",
            startDate: DateTime.UtcNow.AddMonths(1),
            endDate: DateTime.UtcNow.AddMonths(1).AddDays(7),
            basePrice: 2000.00m,
            singleRoomSupplementPrice: 500.00m,
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
        var currency = EntityBuilders.ParseCurrency(currencyCode);
        tourContext.UpdateResult = tourContext.Tour.UpdateCurrency(currency);
    }

    [Then(@"the tour should have currency ""(.*)""")]
    public void ThenTheTourShouldHaveCurrency(string expectedCurrencyCode)
    {
        var expectedCurrency = EntityBuilders.ParseCurrency(expectedCurrencyCode);
        Assert.Equal(expectedCurrency, tourContext.Tour.Pricing.Currency);
    }

    [When(@"I try to update the currency to ""(.*)""")]
    public void WhenITryToUpdateTheCurrencyTo(string currencyCode)
    {
        var currency = EntityBuilders.ParseCurrency(currencyCode);
        tourContext.UpdateResult = tourContext.Tour.UpdateCurrency(currency);
    }

    [Then("the currency update should fail")]
    public void ThenTheCurrencyUpdateShouldFail()
    {
        Assert.NotNull(tourContext.UpdateResult);
        Assert.False(tourContext.UpdateResult.Value.IsSuccess);
    }
}
