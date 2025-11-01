using System.Globalization;
using Reqnroll;
using ViajantesTurismo.Admin.Domain.Tours;
using ViajantesTurismo.Common.Monies;
using ViajantesTurismo.Common.Results;

namespace ViajantesTurismo.Admin.BehaviorTests.Steps;

[Binding]
public sealed class TourManagementSteps
{
    private DateTime _endDate;
    private object? _result;
    private DateTime _startDate;
    private Tour? _tour;

    [Given(@"I have tour dates from ""(.*)"" to ""(.*)""")]
    public void GivenIHaveTourDatesFromTo(string startDateString, string endDateString)
    {
        _startDate = DateTime.Parse(startDateString, CultureInfo.InvariantCulture).ToUniversalTime();
        _endDate = DateTime.Parse(endDateString, CultureInfo.InvariantCulture).ToUniversalTime();
    }

    [Given(@"an existing tour with dates from ""(.*)"" to ""(.*)""")]
    public void GivenAnExistingTourWithDatesFromTo(string startDateString, string endDateString)
    {
        _startDate = DateTime.Parse(startDateString, CultureInfo.InvariantCulture).ToUniversalTime();
        _endDate = DateTime.Parse(endDateString, CultureInfo.InvariantCulture).ToUniversalTime();
        _tour = TestHelpers.CreateTestTourWithDates(_startDate, _endDate);
    }

    [Given(@"an existing tour with identifier ""(.*)"" and name ""(.*)""")]
    public void GivenAnExistingTourWithIdentifierAndName(string identifier, string name)
    {
        _tour = TestHelpers.CreateTestTourWithIdentifierAndName(identifier, name);
    }

    [Given(@"an existing tour with base price (.*)")]
    public void GivenAnExistingTourWithBasePrice(decimal price)
    {
        _tour = Tour.Create(
            identifier: "TEST2024",
            name: "Test Tour",
            startDate: DateTime.UtcNow.AddMonths(1),
            endDate: DateTime.UtcNow.AddMonths(1).AddDays(7),
            price: price,
            singleRoomSupplementPrice: 500.00m,
            regularBikePrice: 100.00m,
            eBikePrice: 200.00m,
            currency: Currency.UsDollar,
            includedServices: ["Hotel", "Breakfast"]).Value;
    }

    [Given(@"an existing tour with services ""(.*)""")]
    public void GivenAnExistingTourWithServices(string servicesString)
    {
        var services = servicesString.Split(", ");
        _tour = Tour.Create(
            identifier: "TEST2024",
            name: "Test Tour",
            startDate: DateTime.UtcNow.AddMonths(1),
            endDate: DateTime.UtcNow.AddMonths(1).AddDays(7),
            price: 2000.00m,
            singleRoomSupplementPrice: 500.00m,
            regularBikePrice: 100.00m,
            eBikePrice: 200.00m,
            currency: Currency.UsDollar,
            includedServices: services).Value;
    }

    [Given(@"an existing tour with currency ""(.*)""")]
    public void GivenAnExistingTourWithCurrency(string currencyCode)
    {
        var currency = TestHelpers.ParseCurrency(currencyCode);
        _tour = Tour.Create(
            identifier: "TEST2024",
            name: "Test Tour",
            startDate: DateTime.UtcNow.AddMonths(1),
            endDate: DateTime.UtcNow.AddMonths(1).AddDays(7),
            price: 2000.00m,
            singleRoomSupplementPrice: 500.00m,
            regularBikePrice: 100.00m,
            eBikePrice: 200.00m,
            currency: currency,
            includedServices: ["Hotel", "Breakfast"]).Value;
    }

    [Given(@"an existing tour")]
    public void GivenAnExistingTour()
    {
        _tour = TestHelpers.CreateTestTour();
    }

    [When(@"I create the tour")]
    public void WhenICreateTheTour()
    {
        _tour = TestHelpers.CreateTestTourWithDates(_startDate, _endDate);
    }

    [When(@"I try to create the tour")]
    public void WhenITryToCreateTheTour()
    {
        _result = Tour.Create(
            identifier: "TEST2024",
            name: "Test Tour",
            startDate: _startDate,
            endDate: _endDate,
            price: 2000.00m,
            singleRoomSupplementPrice: 500.00m,
            regularBikePrice: 100.00m,
            eBikePrice: 200.00m,
            currency: Currency.UsDollar,
            includedServices: ["Hotel", "Breakfast"]);

        _tour = _result is Result<Tour> { IsSuccess: true } result ? result.Value : null;
    }

    [When(@"I update the schedule to ""(.*)"" to ""(.*)""")]
    public void WhenIUpdateTheScheduleToTo(string startDateString, string endDateString)
    {
        var newStartDate = DateTime.Parse(startDateString, CultureInfo.InvariantCulture).ToUniversalTime();
        var newEndDate = DateTime.Parse(endDateString, CultureInfo.InvariantCulture).ToUniversalTime();

        _result = _tour!.UpdateSchedule(newStartDate, newEndDate);

        (_startDate, _endDate) = _result is Result { IsSuccess: true } ? (newStartDate, newEndDate) : (_startDate, _endDate);
    }

    [When(@"I try to update the schedule to ""(.*)"" to ""(.*)""")]
    public void WhenITryToUpdateTheScheduleToTo(string startDateString, string endDateString)
    {
        var newStartDate = DateTime.Parse(startDateString, CultureInfo.InvariantCulture).ToUniversalTime();
        var newEndDate = DateTime.Parse(endDateString, CultureInfo.InvariantCulture).ToUniversalTime();

        _result = _tour!.UpdateSchedule(newStartDate, newEndDate);
    }

    [When(@"I update the identifier to ""(.*)"" and name to ""(.*)""")]
    public void WhenIUpdateTheIdentifierToAndNameTo(string newIdentifier, string newName)
    {
        _tour!.UpdateBasicInfo(newIdentifier, newName);
    }

    [When(@"I update the base price to (.*)")]
    public void WhenIUpdateTheBasePriceTo(decimal newPrice)
    {
        _tour!.UpdateBasePrice(newPrice);
    }

    [When(@"I update the services to ""(.*)""")]
    public void WhenIUpdateTheServicesTo(string servicesString)
    {
        var services = servicesString.Split(", ");
        _tour!.UpdateIncludedServices(services);
    }

    [When(@"I update the currency to ""(.*)""")]
    public void WhenIUpdateTheCurrencyTo(string currencyCode)
    {
        var currency = TestHelpers.ParseCurrency(currencyCode);
        _tour!.UpdateCurrency(currency);
    }

    [When(@"I update pricing with single room (.*), regular bike (.*), e-bike (.*)")]
    public void WhenIUpdatePricingWithBaseSingleRoomRegularBikeEBike(
        decimal singleRoom,
        decimal regularBike,
        decimal eBike)
    {
        _tour!.UpdatePricing(singleRoom, regularBike, eBike, Currency.UsDollar);
    }

    [Then(@"the tour should be created successfully")]
    public void ThenTheTourShouldBeCreatedSuccessfully()
    {
        Assert.NotNull(_tour);
    }

    [Then(@"the tour creation should fail with message ""(.*)""")]
    public static void ThenTheTourCreationShouldFailWithMessage(string expectedMessage)
    {
        // This scenario is not currently used but keeping for potential future use
        Assert.Fail("This assertion step needs to be updated to work with Result pattern");
    }

    [Then(@"the tour creation should fail with argument exception ""(.*)""")]
    public void ThenTheTourCreationShouldFailWithArgumentException(string expectedMessage)
    {
        Assert.NotNull(_result);

        var (isSuccess, errorDetail) = _result switch
        {
            Result<Tour> tr => (tr.IsSuccess, tr.ErrorDetails?.Detail),
            Result r => (r.IsSuccess, r.ErrorDetails?.Detail),
            _ => throw new InvalidOperationException("Unexpected result type")
        };

        Assert.False(isSuccess);
        Assert.Contains(expectedMessage, errorDetail!, StringComparison.Ordinal);
    }

    [Then(@"the tour dates should be ""(.*)"" to ""(.*)""")]
    public void ThenTheTourDatesShouldBeTo(string expectedStartString, string expectedEndString)
    {
        var expectedStart = DateTime.Parse(expectedStartString, CultureInfo.InvariantCulture).ToUniversalTime();
        var expectedEnd = DateTime.Parse(expectedEndString, CultureInfo.InvariantCulture).ToUniversalTime();

        Assert.Equal(expectedStart, _tour!.StartDate);
        Assert.Equal(expectedEnd, _tour.EndDate);
    }

    [Then(@"the tour identifier should be ""(.*)""")]
    public void ThenTheTourIdentifierShouldBe(string expectedIdentifier)
    {
        Assert.Equal(expectedIdentifier, _tour!.Identifier);
    }

    [Then(@"the tour name should be ""(.*)""")]
    public void ThenTheTourNameShouldBe(string expectedName)
    {
        Assert.Equal(expectedName, _tour!.Name);
    }

    [Then(@"the tour base price should be (.*)")]
    public void ThenTheTourBasePriceShouldBe(decimal expectedPrice)
    {
        Assert.Equal(expectedPrice, _tour!.Price);
    }

    [Then(@"the tour should include services ""(.*)""")]
    public void ThenTheTourShouldIncludeServices(string servicesString)
    {
        var expectedServices = servicesString.Split(", ");
        Assert.Equal(expectedServices.Length, _tour!.IncludedServices.Count);

        foreach (var service in expectedServices)
        {
            Assert.Contains(service, _tour.IncludedServices);
        }
    }

    [Then(@"the tour currency should be ""(.*)""")]
    public void ThenTheTourCurrencyShouldBe(string currencyCode)
    {
        var expectedCurrency = TestHelpers.ParseCurrency(currencyCode);
        Assert.Equal(expectedCurrency, _tour!.Currency);
    }

    [Then(@"the tour pricing should reflect all updates")]
    public void ThenTheTourPricingShouldReflectAllUpdates()
    {
        Assert.Equal(600.00m, _tour!.SingleRoomSupplementPrice);
        Assert.Equal(150.00m, _tour.RegularBikePrice);
        Assert.Equal(250.00m, _tour.EBikePrice);
    }
}
