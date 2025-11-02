using System.Globalization;
using Reqnroll;
using ViajantesTurismo.Admin.Domain.Tours;
using ViajantesTurismo.Common.Monies;
using ViajantesTurismo.Common.Results;

namespace ViajantesTurismo.Admin.BehaviorTests.Steps;

[Binding]
public sealed class TourManagementSteps(ScenarioContext scenarioContext)
{
    private decimal _basePrice = 2000.00m;
    private decimal _eBikePrice = 200.00m;
    private DateTime _endDate;
    private string _identifier = "TEST2024";
    private string _name = "Test Tour";
    private decimal _regularBikePrice = 100.00m;
    private object? _result;
    private decimal _singleRoomSupplementPrice = 500.00m;
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

    [Given(@"I have tour details with identifier ""(.*)"" and name ""(.*)""")]
    public void GivenIHaveTourDetailsWithIdentifierAndName(string identifier, string name)
    {
        _identifier = identifier;
        _name = name;
        _startDate = DateTime.UtcNow.AddMonths(1);
        _endDate = DateTime.UtcNow.AddMonths(1).AddDays(7);
    }

    [Given(@"I have tour details with identifier longer than 128 characters")]
    public void GivenIHaveTourDetailsWithIdentifierLongerThan128Characters()
    {
        _identifier = new string('A', 129);
        _name = "Valid Tour Name";
        _startDate = DateTime.UtcNow.AddMonths(1);
        _endDate = DateTime.UtcNow.AddMonths(1).AddDays(7);
    }

    [Given(@"I have tour details with name longer than 128 characters")]
    public void GivenIHaveTourDetailsWithNameLongerThan128Characters()
    {
        _identifier = "VALID2024";
        _name = new string('A', 129);
        _startDate = DateTime.UtcNow.AddMonths(1);
        _endDate = DateTime.UtcNow.AddMonths(1).AddDays(7);
    }

    [Given(@"I have tour details with base price (.*)")]
    public void GivenIHaveTourDetailsWithBasePrice(decimal price)
    {
        _identifier = "TEST2024";
        _name = "Test Tour";
        _startDate = DateTime.UtcNow.AddMonths(1);
        _endDate = DateTime.UtcNow.AddMonths(1).AddDays(7);
        _basePrice = price;
        _singleRoomSupplementPrice = 500.00m;
        _regularBikePrice = 100.00m;
        _eBikePrice = 200.00m;
    }

    [Given(@"I have tour details with single room supplement (.*)")]
    public void GivenIHaveTourDetailsWithSingleRoomSupplement(decimal price)
    {
        _identifier = "TEST2024";
        _name = "Test Tour";
        _startDate = DateTime.UtcNow.AddMonths(1);
        _endDate = DateTime.UtcNow.AddMonths(1).AddDays(7);
        _singleRoomSupplementPrice = price;
    }

    [Given(@"I have tour details with regular bike price (.*)")]
    public void GivenIHaveTourDetailsWithRegularBikePrice(decimal price)
    {
        _identifier = "TEST2024";
        _name = "Test Tour";
        _startDate = DateTime.UtcNow.AddMonths(1);
        _endDate = DateTime.UtcNow.AddMonths(1).AddDays(7);
        _regularBikePrice = price;
    }

    [Given(@"I have tour details with e-bike price (.*)")]
    public void GivenIHaveTourDetailsWithEBikePrice(decimal price)
    {
        _identifier = "TEST2024";
        _name = "Test Tour";
        _startDate = DateTime.UtcNow.AddMonths(1);
        _endDate = DateTime.UtcNow.AddMonths(1).AddDays(7);
        _eBikePrice = price;
    }

    [Given(@"I have tour details with multiple invalid values")]
    public void GivenIHaveTourDetailsWithMultipleInvalidValues()
    {
        _identifier = "";
        _name = "";
        _startDate = DateTime.UtcNow.AddMonths(1);
        _endDate = _startDate.AddDays(2); // Too short duration
        _basePrice = -100m;
        _singleRoomSupplementPrice = -50m;
        _regularBikePrice = -30m;
        _eBikePrice = -40m;
    }

    [When(@"I create the tour")]
    public void WhenICreateTheTour()
    {
        _tour = TestHelpers.CreateTestTourWithDates(_startDate, _endDate);
    }

    [When(@"I try to create the tour")]
    public void WhenITryToCreateTheTour()
    {
        var services = scenarioContext.ContainsKey("Services")
            ? scenarioContext.Get<string[]>("Services")
            : ["Hotel", "Breakfast"];

        _result = Tour.Create(
            identifier: _identifier,
            name: _name,
            startDate: _startDate,
            endDate: _endDate,
            price: _basePrice,
            singleRoomSupplementPrice: _singleRoomSupplementPrice,
            regularBikePrice: _regularBikePrice,
            eBikePrice: _eBikePrice,
            currency: Currency.UsDollar,
            includedServices: services);

        _tour = _result is Result<Tour> { IsSuccess: true } result ? result.Value : null;

        scenarioContext["Result"] = _result;
        if (_tour != null)
        {
            scenarioContext["Tour"] = _tour;
        }
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
        _result = _tour!.UpdatePricing(singleRoom, regularBike, eBike, Currency.UsDollar);
    }

    [When(@"I try to update the base price to (.*)")]
    public void WhenITryToUpdateTheBasePriceTo(decimal newPrice)
    {
        _result = _tour!.UpdateBasePrice(newPrice);
    }

    [When(@"I try to update the identifier to ""(.*)"" and name to ""(.*)""")]
    public void WhenITryToUpdateTheIdentifierToAndNameTo(string newIdentifier, string newName)
    {
        _result = _tour!.UpdateBasicInfo(newIdentifier, newName);
    }

    [When(@"I try to update the identifier to a string longer than 128 characters")]
    public void WhenITryToUpdateTheIdentifierToAStringLongerThan128Characters()
    {
        var longIdentifier = new string('A', 129);
        _result = _tour!.UpdateBasicInfo(longIdentifier, "Valid Name");
    }

    [When(@"I try to update the name to a string longer than 128 characters")]
    public void WhenITryToUpdateTheNameToAStringLongerThan128Characters()
    {
        var longName = new string('A', 129);
        _result = _tour!.UpdateBasicInfo("VALID2024", longName);
    }

    [When(@"I try to update pricing with single room (.*)")]
    public void WhenITryToUpdatePricingWithSingleRoom(decimal singleRoom)
    {
        _result = _tour!.UpdatePricing(singleRoom, 100.00m, 200.00m, Currency.UsDollar);
    }

    [When(@"I try to update pricing with regular bike (.*)")]
    public void WhenITryToUpdatePricingWithRegularBike(decimal regularBike)
    {
        _result = _tour!.UpdatePricing(500.00m, regularBike, 200.00m, Currency.UsDollar);
    }

    [When(@"I try to update pricing with e-bike (.*)")]
    public void WhenITryToUpdatePricingWithEBike(decimal eBike)
    {
        _result = _tour!.UpdatePricing(500.00m, 100.00m, eBike, Currency.UsDollar);
    }

    [Then(@"the tour should be created successfully")]
    public void ThenTheTourShouldBeCreatedSuccessfully()
    {
        Assert.NotNull(_tour);
    }

    [Then(@"the tour creation should fail with message ""(.*)""")]
    public static void ThenTheTourCreationShouldFailWithMessage(string expectedMessage)
    {
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

    [Then(@"the tour creation should fail with validation error for ""(.*)""")]
    public void ThenTheTourCreationShouldFailWithValidationErrorFor(string fieldName)
    {
        Assert.NotNull(_result);
        Assert.IsType<Result<Tour>>(_result);
        var result = (Result<Tour>)_result;
        Assert.False(result.IsSuccess);
        Assert.True(result.ErrorDetails?.ValidationErrors?.ContainsKey(fieldName) ?? false);
    }

    [Then(@"the schedule update should fail with validation error for ""(.*)""")]
    public void ThenTheScheduleUpdateShouldFailWithValidationErrorFor(string fieldName)
    {
        Assert.NotNull(_result);
        Assert.IsType<Result>(_result);
        var result = (Result)_result;
        Assert.False(result.IsSuccess);
        Assert.True(result.ErrorDetails?.ValidationErrors?.ContainsKey(fieldName) ?? false);
    }

    [Then(@"the price update should fail")]
    public void ThenThePriceUpdateShouldFail()
    {
        Assert.NotNull(_result);
        Assert.IsType<Result>(_result);
        var result = (Result)_result;
        Assert.False(result.IsSuccess);
    }

    [Then(@"the basic info update should fail with validation error for ""(.*)""")]
    public void ThenTheBasicInfoUpdateShouldFailWithValidationErrorFor(string fieldName)
    {
        Assert.NotNull(_result);
        Assert.IsType<Result>(_result);
        var result = (Result)_result;
        Assert.False(result.IsSuccess);
        Assert.True(result.ErrorDetails?.ValidationErrors?.ContainsKey(fieldName) ?? false);
    }

    [Then(@"the pricing update should fail with validation error for ""(.*)""")]
    public void ThenThePricingUpdateShouldFailWithValidationErrorFor(string fieldName)
    {
        Assert.NotNull(_result);
        Assert.IsType<Result>(_result);
        var result = (Result)_result;
        Assert.False(result.IsSuccess);
        Assert.True(result.ErrorDetails?.ValidationErrors?.ContainsKey(fieldName) ?? false);
    }

    [Then(@"the tour creation should fail with multiple validation errors")]
    public void ThenTheTourCreationShouldFailWithMultipleValidationErrors()
    {
        Assert.NotNull(_result);
        Assert.IsType<Result<Tour>>(_result);
        var result = (Result<Tour>)_result;
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.ErrorDetails?.ValidationErrors);
        Assert.True(result.ErrorDetails.ValidationErrors.Count > 1, "Expected multiple validation errors");
    }
}