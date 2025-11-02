using System.Globalization;
using Reqnroll;
using ViajantesTurismo.Admin.BehaviorTests.Context;
using ViajantesTurismo.Admin.Domain.Tours;
using ViajantesTurismo.Common.Monies;
using ViajantesTurismo.Common.Results;

namespace ViajantesTurismo.Admin.BehaviorTests.Steps;

[Binding]
public sealed class TourManagementSteps(TourContext tourContext, ScenarioContext scenarioContext)
{
    [Given(@"I have tour dates from ""(.*)"" to ""(.*)""")]
    public void GivenIHaveTourDatesFromTo(string startDateString, string endDateString)
    {
        tourContext.StartDate = DateTime.Parse(startDateString, CultureInfo.InvariantCulture).ToUniversalTime();
        tourContext.EndDate = DateTime.Parse(endDateString, CultureInfo.InvariantCulture).ToUniversalTime();
    }

    [Given(@"an existing tour with dates from ""(.*)"" to ""(.*)""")]
    public void GivenAnExistingTourWithDatesFromTo(string startDateString, string endDateString)
    {
        tourContext.StartDate = DateTime.Parse(startDateString, CultureInfo.InvariantCulture).ToUniversalTime();
        tourContext.EndDate = DateTime.Parse(endDateString, CultureInfo.InvariantCulture).ToUniversalTime();
        tourContext.Tour = TestHelpers.CreateTestTourWithDates(tourContext.StartDate, tourContext.EndDate);
    }

    [Given(@"an existing tour with identifier ""(.*)"" and name ""(.*)""")]
    public void GivenAnExistingTourWithIdentifierAndName(string identifier, string name)
    {
        tourContext.Tour = TestHelpers.CreateTestTourWithIdentifierAndName(identifier, name);
    }

    [Given(@"an existing tour with base price (.*)")]
    public void GivenAnExistingTourWithBasePrice(decimal price)
    {
        tourContext.Tour = Tour.Create(
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
        tourContext.Tour = Tour.Create(
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
        tourContext.Tour = Tour.Create(
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
        tourContext.Tour = TestHelpers.CreateTestTour();
    }

    [Given(@"I have tour details with identifier ""(.*)"" and name ""(.*)""")]
    public void GivenIHaveTourDetailsWithIdentifierAndName(string identifier, string name)
    {
        ContextHelpers.SetupValidTour(tourContext);
        tourContext.Identifier = identifier;
        tourContext.Name = name;
    }

    [Given(@"I have tour details with identifier longer than 128 characters")]
    public void GivenIHaveTourDetailsWithIdentifierLongerThan128Characters()
    {
        tourContext.Identifier = new string('A', 129);
        tourContext.Name = "Valid Tour Name";
        tourContext.StartDate = DateTime.UtcNow.AddMonths(1);
        tourContext.EndDate = DateTime.UtcNow.AddMonths(1).AddDays(7);
    }

    [Given(@"I have tour details with name longer than 128 characters")]
    public void GivenIHaveTourDetailsWithNameLongerThan128Characters()
    {
        tourContext.Identifier = "VALID2024";
        tourContext.Name = new string('A', 129);
        tourContext.StartDate = DateTime.UtcNow.AddMonths(1);
        tourContext.EndDate = DateTime.UtcNow.AddMonths(1).AddDays(7);
    }

    [Given(@"I have tour details with base price (.*)")]
    public void GivenIHaveTourDetailsWithBasePrice(decimal price)
    {
        tourContext.Identifier = "TEST2024";
        tourContext.Name = "Test Tour";
        tourContext.StartDate = DateTime.UtcNow.AddMonths(1);
        tourContext.EndDate = DateTime.UtcNow.AddMonths(1).AddDays(7);
        tourContext.BasePrice = price;
        tourContext.SingleRoomSupplementPrice = 500.00m;
        tourContext.RegularBikePrice = 100.00m;
        tourContext.EBikePrice = 200.00m;
    }

    [Given(@"I have tour details with single room supplement (.*)")]
    public void GivenIHaveTourDetailsWithSingleRoomSupplement(decimal price)
    {
        tourContext.Identifier = "TEST2024";
        tourContext.Name = "Test Tour";
        tourContext.StartDate = DateTime.UtcNow.AddMonths(1);
        tourContext.EndDate = DateTime.UtcNow.AddMonths(1).AddDays(7);
        tourContext.SingleRoomSupplementPrice = price;
    }

    [Given(@"I have tour details with regular bike price (.*)")]
    public void GivenIHaveTourDetailsWithRegularBikePrice(decimal price)
    {
        tourContext.Identifier = "TEST2024";
        tourContext.Name = "Test Tour";
        tourContext.StartDate = DateTime.UtcNow.AddMonths(1);
        tourContext.EndDate = DateTime.UtcNow.AddMonths(1).AddDays(7);
        tourContext.RegularBikePrice = price;
    }

    [Given(@"I have tour details with e-bike price (.*)")]
    public void GivenIHaveTourDetailsWithEBikePrice(decimal price)
    {
        tourContext.Identifier = "TEST2024";
        tourContext.Name = "Test Tour";
        tourContext.StartDate = DateTime.UtcNow.AddMonths(1);
        tourContext.EndDate = DateTime.UtcNow.AddMonths(1).AddDays(7);
        tourContext.EBikePrice = price;
    }

    [Given(@"I have tour details with multiple invalid values")]
    public void GivenIHaveTourDetailsWithMultipleInvalidValues()
    {
        tourContext.Identifier = "";
        tourContext.Name = "";
        tourContext.StartDate = DateTime.UtcNow.AddMonths(1);
        tourContext.EndDate = tourContext.StartDate.AddDays(2); // Too short duration
        tourContext.BasePrice = -100m;
        tourContext.SingleRoomSupplementPrice = -50m;
        tourContext.RegularBikePrice = -30m;
        tourContext.EBikePrice = -40m;
    }

    [When(@"I create the tour")]
    public void WhenICreateTheTour()
    {
        tourContext.Tour = TestHelpers.CreateTestTourWithDates(tourContext.StartDate, tourContext.EndDate);
    }

    [When(@"I try to create the tour")]
    public void WhenITryToCreateTheTour()
    {
        var services = scenarioContext.ContainsKey("Services")
            ? scenarioContext.Get<string[]>("Services")
            : ["Hotel", "Breakfast"];

        tourContext.Result = Tour.Create(
            identifier: tourContext.Identifier,
            name: tourContext.Name,
            startDate: tourContext.StartDate,
            endDate: tourContext.EndDate,
            price: tourContext.BasePrice,
            singleRoomSupplementPrice: tourContext.SingleRoomSupplementPrice,
            regularBikePrice: tourContext.RegularBikePrice,
            eBikePrice: tourContext.EBikePrice,
            currency: Currency.UsDollar,
            includedServices: services);

        if (tourContext.Result is Result<Tour> { IsSuccess: true } result)
        {
            tourContext.Tour = result.Value;
        }

        scenarioContext["Result"] = tourContext.Result;
        if (tourContext.Result is Result<Tour> { IsSuccess: true })
        {
            scenarioContext["Tour"] = tourContext.Tour;
        }
    }

    [When(@"I update the schedule to ""(.*)"" to ""(.*)""")]
    public void WhenIUpdateTheScheduleToTo(string startDateString, string endDateString)
    {
        var newStartDate = DateTime.Parse(startDateString, CultureInfo.InvariantCulture).ToUniversalTime();
        var newEndDate = DateTime.Parse(endDateString, CultureInfo.InvariantCulture).ToUniversalTime();

        tourContext.Result = tourContext.Tour.UpdateSchedule(newStartDate, newEndDate);

        (tourContext.StartDate, tourContext.EndDate) = tourContext.Result is Result { IsSuccess: true } ? (newStartDate, newEndDate) : (tourContext.StartDate, tourContext.EndDate);
    }

    [When(@"I try to update the schedule to ""(.*)"" to ""(.*)""")]
    public void WhenITryToUpdateTheScheduleToTo(string startDateString, string endDateString)
    {
        var newStartDate = DateTime.Parse(startDateString, CultureInfo.InvariantCulture).ToUniversalTime();
        var newEndDate = DateTime.Parse(endDateString, CultureInfo.InvariantCulture).ToUniversalTime();

        tourContext.Result = tourContext.Tour.UpdateSchedule(newStartDate, newEndDate);
    }

    [When(@"I update the identifier to ""(.*)"" and name to ""(.*)""")]
    public void WhenIUpdateTheIdentifierToAndNameTo(string newIdentifier, string newName)
    {
        tourContext.Tour.UpdateBasicInfo(newIdentifier, newName);
    }

    [When(@"I update the base price to (.*)")]
    public void WhenIUpdateTheBasePriceTo(decimal newPrice)
    {
        tourContext.Tour.UpdateBasePrice(newPrice);
    }

    [When(@"I update the services to ""(.*)""")]
    public void WhenIUpdateTheServicesTo(string servicesString)
    {
        var services = servicesString.Split(", ");
        tourContext.Tour.UpdateIncludedServices(services);
    }

    [When(@"I update the currency to ""(.*)""")]
    public void WhenIUpdateTheCurrencyTo(string currencyCode)
    {
        var currency = TestHelpers.ParseCurrency(currencyCode);
        tourContext.Tour.UpdateCurrency(currency);
    }

    [When(@"I update pricing with single room (.*), regular bike (.*), e-bike (.*)")]
    public void WhenIUpdatePricingWithBaseSingleRoomRegularBikeEBike(
        decimal singleRoom,
        decimal regularBike,
        decimal eBike)
    {
        tourContext.Result = tourContext.Tour.UpdatePricing(singleRoom, regularBike, eBike, Currency.UsDollar);
    }

    [When(@"I try to update the base price to (.*)")]
    public void WhenITryToUpdateTheBasePriceTo(decimal newPrice)
    {
        tourContext.Result = tourContext.Tour.UpdateBasePrice(newPrice);
    }

    [When(@"I try to update the identifier to ""(.*)"" and name to ""(.*)""")]
    public void WhenITryToUpdateTheIdentifierToAndNameTo(string newIdentifier, string newName)
    {
        tourContext.Result = tourContext.Tour.UpdateBasicInfo(newIdentifier, newName);
    }

    [When(@"I try to update the identifier to a string longer than 128 characters")]
    public void WhenITryToUpdateTheIdentifierToAStringLongerThan128Characters()
    {
        var longIdentifier = new string('A', 129);
        tourContext.Result = tourContext.Tour.UpdateBasicInfo(longIdentifier, "Valid Name");
    }

    [When(@"I try to update the name to a string longer than 128 characters")]
    public void WhenITryToUpdateTheNameToAStringLongerThan128Characters()
    {
        var longName = new string('A', 129);
        tourContext.Result = tourContext.Tour.UpdateBasicInfo("VALID2024", longName);
    }

    [When(@"I try to update pricing with single room (.*)")]
    public void WhenITryToUpdatePricingWithSingleRoom(decimal singleRoom)
    {
        tourContext.Result = tourContext.Tour.UpdatePricing(singleRoom, 100.00m, 200.00m, Currency.UsDollar);
    }

    [When(@"I try to update pricing with regular bike (.*)")]
    public void WhenITryToUpdatePricingWithRegularBike(decimal regularBike)
    {
        tourContext.Result = tourContext.Tour.UpdatePricing(500.00m, regularBike, 200.00m, Currency.UsDollar);
    }

    [When(@"I try to update pricing with e-bike (.*)")]
    public void WhenITryToUpdatePricingWithEBike(decimal eBike)
    {
        tourContext.Result = tourContext.Tour.UpdatePricing(500.00m, 100.00m, eBike, Currency.UsDollar);
    }

    [Then(@"the tour should be created successfully")]
    public void ThenTheTourShouldBeCreatedSuccessfully()
    {
        Assert.NotNull(tourContext.Tour);
    }

    [Then(@"the tour creation should fail with message ""(.*)""")]
    public static void ThenTheTourCreationShouldFailWithMessage(string expectedMessage)
    {
        Assert.Fail("This assertion step needs to be updated to work with Result pattern");
    }

    [Then(@"the tour creation should fail with argument exception ""(.*)""")]
    public void ThenTheTourCreationShouldFailWithArgumentException(string expectedMessage)
    {
        Assert.NotNull(tourContext.Result);

        var (isSuccess, errorDetail, validationErrors) = tourContext.Result switch
        {
            Result<Tour> tr => (tr.IsSuccess, tr.ErrorDetails?.Detail, tr.ErrorDetails?.ValidationErrors),
            Result r => (r.IsSuccess, r.ErrorDetails?.Detail, r.ErrorDetails?.ValidationErrors),
            _ => throw new InvalidOperationException("Unexpected result type")
        };

        Assert.False(isSuccess);

        // Check if message is in the detail or in validation errors
        var messageFound = errorDetail?.Contains(expectedMessage, StringComparison.Ordinal) ?? false;
        if (!messageFound && validationErrors != null)
        {
            messageFound = validationErrors.Values
                .SelectMany(errors => errors)
                .Any(error => error.Contains(expectedMessage, StringComparison.Ordinal));
        }

        Assert.True(messageFound, $"Expected message '{expectedMessage}' not found in error details or validation errors.");
    }

    [Then(@"the tour dates should be ""(.*)"" to ""(.*)""")]
    public void ThenTheTourDatesShouldBeTo(string expectedStartString, string expectedEndString)
    {
        var expectedStart = DateTime.Parse(expectedStartString, CultureInfo.InvariantCulture).ToUniversalTime();
        var expectedEnd = DateTime.Parse(expectedEndString, CultureInfo.InvariantCulture).ToUniversalTime();

        Assert.Equal(expectedStart, tourContext.Tour.StartDate);
        Assert.Equal(expectedEnd, tourContext.Tour.EndDate);
    }

    [Then(@"the tour identifier should be ""(.*)""")]
    public void ThenTheTourIdentifierShouldBe(string expectedIdentifier)
    {
        Assert.Equal(expectedIdentifier, tourContext.Tour.Identifier);
    }

    [Then(@"the tour name should be ""(.*)""")]
    public void ThenTheTourNameShouldBe(string expectedName)
    {
        Assert.Equal(expectedName, tourContext.Tour.Name);
    }

    [Then(@"the tour base price should be (.*)")]
    public void ThenTheTourBasePriceShouldBe(decimal expectedPrice)
    {
        Assert.Equal(expectedPrice, tourContext.Tour.Price);
    }

    [Then(@"the tour should include services ""(.*)""")]
    public void ThenTheTourShouldIncludeServices(string servicesString)
    {
        var expectedServices = servicesString.Split(", ");
        Assert.Equal(expectedServices.Length, tourContext.Tour.IncludedServices.Count);

        foreach (var service in expectedServices)
        {
            Assert.Contains(service, tourContext.Tour.IncludedServices);
        }
    }

    [Then(@"the tour currency should be ""(.*)""")]
    public void ThenTheTourCurrencyShouldBe(string currencyCode)
    {
        var expectedCurrency = TestHelpers.ParseCurrency(currencyCode);
        Assert.Equal(expectedCurrency, tourContext.Tour.Currency);
    }

    [Then(@"the tour pricing should reflect all updates")]
    public void ThenTheTourPricingShouldReflectAllUpdates()
    {
        Assert.Equal(600.00m, tourContext.Tour.SingleRoomSupplementPrice);
        Assert.Equal(150.00m, tourContext.Tour.RegularBikePrice);
        Assert.Equal(250.00m, tourContext.Tour.EBikePrice);
    }

    [Then(@"the tour creation should fail with validation error for ""(.*)""")]
    public void ThenTheTourCreationShouldFailWithValidationErrorFor(string fieldName)
    {
        Assert.NotNull(tourContext.Result);
        Assert.IsType<Result<Tour>>(tourContext.Result);
        var result = (Result<Tour>)tourContext.Result;
        Assert.False(result.IsSuccess);
        Assert.True(result.ErrorDetails?.ValidationErrors?.ContainsKey(fieldName) ?? false);
    }

    [Then(@"the schedule update should fail with validation error for ""(.*)""")]
    public void ThenTheScheduleUpdateShouldFailWithValidationErrorFor(string fieldName)
    {
        Assert.NotNull(tourContext.Result);
        Assert.IsType<Result>(tourContext.Result);
        var result = (Result)tourContext.Result;
        Assert.False(result.IsSuccess);
        Assert.True(result.ErrorDetails?.ValidationErrors?.ContainsKey(fieldName) ?? false);
    }

    [Then(@"the price update should fail")]
    public void ThenThePriceUpdateShouldFail()
    {
        Assert.NotNull(tourContext.Result);
        Assert.IsType<Result>(tourContext.Result);
        var result = (Result)tourContext.Result;
        Assert.False(result.IsSuccess);
    }

    [Then(@"the basic info update should fail with validation error for ""(.*)""")]
    public void ThenTheBasicInfoUpdateShouldFailWithValidationErrorFor(string fieldName)
    {
        Assert.NotNull(tourContext.Result);
        Assert.IsType<Result>(tourContext.Result);
        var result = (Result)tourContext.Result;
        Assert.False(result.IsSuccess);
        Assert.True(result.ErrorDetails?.ValidationErrors?.ContainsKey(fieldName) ?? false);
    }

    [Then(@"the pricing update should fail with validation error for ""(.*)""")]
    public void ThenThePricingUpdateShouldFailWithValidationErrorFor(string fieldName)
    {
        Assert.NotNull(tourContext.Result);
        Assert.IsType<Result>(tourContext.Result);
        var result = (Result)tourContext.Result;
        Assert.False(result.IsSuccess);
        Assert.True(result.ErrorDetails?.ValidationErrors?.ContainsKey(fieldName) ?? false);
    }

    [Then(@"the tour creation should fail with multiple validation errors")]
    public void ThenTheTourCreationShouldFailWithMultipleValidationErrors()
    {
        Assert.NotNull(tourContext.Result);
        Assert.IsType<Result<Tour>>(tourContext.Result);
        var result = (Result<Tour>)tourContext.Result;
        Assert.False(result.IsSuccess);
        Assert.NotNull(result.ErrorDetails?.ValidationErrors);
        Assert.True(result.ErrorDetails.ValidationErrors.Count > 1, "Expected multiple validation errors");
    }
}
