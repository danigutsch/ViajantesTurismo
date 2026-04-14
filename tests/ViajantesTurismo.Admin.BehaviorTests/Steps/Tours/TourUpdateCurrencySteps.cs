using ViajantesTurismo.Admin.Domain.Shared;

namespace ViajantesTurismo.Admin.BehaviorTests.Steps.Tours;

[Binding]
public sealed class TourUpdateCurrencySteps(TourContext tourContext)
{
    private void UpdateCurrency(string currencyCode)
    {
        var currency = EntityBuilders.ParseCurrency(currencyCode);
        tourContext.UpdateResult = tourContext.Tour.UpdateCurrency(currency);
    }

    [Given(@"a tour exists with currency ""(.*)"" and has (\d+) booking")]
    public void GivenATourExistsWithCurrencyAndHasBooking(string currencyCode, int bookingCount)
    {
        GivenATourExistsWithCurrency(currencyCode);
        for (var i = 0; i < bookingCount; i++)
        {
            tourContext.Tour.AddBooking(new TourBookingRequest(
                Guid.CreateVersion7(),
                BikeType.Regular,
                RoomType.DoubleOccupancy,
                DiscountType.None));
        }
    }

    [Given(@"a tour exists with currency ""(.*)""")]
    public void GivenATourExistsWithCurrency(string currencyCode)
    {
        var currency = EntityBuilders.ParseCurrency(currencyCode);
        tourContext.Tour = Tour.Create(new TourDefinition(
            "TEST2024",
            "Test Tour",
            DateTime.UtcNow.AddMonths(1),
            DateTime.UtcNow.AddMonths(1).AddDays(7),
            2000.00m,
            500.00m,
            100.00m,
            200.00m,
            currency,
            4,
            12,
            ["Hotel", "Breakfast"])).Value;
    }

    [When(@"I update the currency to ""(.*)""")]
    public void WhenIUpdateTheCurrencyTo(string currencyCode)
    {
        UpdateCurrency(currencyCode);
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
        UpdateCurrency(currencyCode);
    }

    [Then("the currency update should fail")]
    public void ThenTheCurrencyUpdateShouldFail()
    {
        Assert.NotNull(tourContext.UpdateResult);
        Assert.False(tourContext.UpdateResult.Value.IsSuccess);
    }
}
