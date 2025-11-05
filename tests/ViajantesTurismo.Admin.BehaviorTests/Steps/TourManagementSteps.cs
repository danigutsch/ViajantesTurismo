using System.Globalization;
using Reqnroll;
using ViajantesTurismo.Admin.BehaviorTests.Context;
using ViajantesTurismo.Admin.Domain.Tours;
using ViajantesTurismo.Common.BuildingBlocks;
using ViajantesTurismo.Common.Monies;
using ViajantesTurismo.Common.Results;

namespace ViajantesTurismo.Admin.BehaviorTests.Steps;

[Binding]
public sealed class TourManagementSteps(TourContext tourContext)
{
    [Given(@"I have tour dates from ""(.*)"" to ""(.*)""")]
    public void GivenIHaveTourDatesFromTo(string startDateString, string endDateString)
    {
        ContextHelpers.SetupValidTour(tourContext);
        tourContext.StartDate = DateTime.Parse(startDateString, CultureInfo.InvariantCulture).ToUniversalTime();
        tourContext.EndDate = DateTime.Parse(endDateString, CultureInfo.InvariantCulture).ToUniversalTime();
    }

    [Given(@"an existing tour with services ""(.*)""")]
    public void GivenAnExistingTourWithServices(string servicesString)
    {
        var services = servicesString.Split(", ");
        tourContext.Tour = Tour.Create(
            identifier: "TEST2024",
            name: "Test Tour",
            schedule: DateRange.Create(DateTime.UtcNow.AddMonths(1), DateTime.UtcNow.AddMonths(1).AddDays(7)).Value,
            pricing: TourPricing.Create(2000.00m, 500.00m, 100.00m, 200.00m, Currency.UsDollar).Value,
            capacity: TourCapacity.Create(4, 12).Value,
            includedServices: services).Value;
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
        ContextHelpers.SetupValidTour(tourContext);
        tourContext.Identifier = new string('A', 129);
        tourContext.Name = "Valid Tour Name";
    }

    [Given(@"I have tour details with name longer than 128 characters")]
    public void GivenIHaveTourDetailsWithNameLongerThan128Characters()
    {
        ContextHelpers.SetupValidTour(tourContext);
        tourContext.Identifier = "VALID2024";
        tourContext.Name = new string('A', 129);
    }

    [Given(@"I have tour details with base price (.*)")]
    public void GivenIHaveTourDetailsWithBasePrice(decimal price)
    {
        tourContext.Identifier = "TEST2024";
        tourContext.Name = "Test Tour";
        tourContext.StartDate = DateTime.UtcNow.AddMonths(1);
        tourContext.EndDate = DateTime.UtcNow.AddMonths(1).AddDays(7);
        tourContext.BasePrice = price;
        tourContext.DoubleRoomSupplementPrice = 500.00m;
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
        tourContext.DoubleRoomSupplementPrice = price;
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
        tourContext.Identifier = "TEST2024";
        tourContext.Name = "Test Tour";
        tourContext.StartDate = DateTime.UtcNow.AddMonths(1);
        tourContext.EndDate = tourContext.StartDate.AddDays(7);
        tourContext.BasePrice = 0m;
        tourContext.DoubleRoomSupplementPrice = 0m;
        tourContext.RegularBikePrice = 0m;
        tourContext.EBikePrice = 0m;
    }

    [Given(@"I have tour details with services ""(.*)""")]
    public void GivenIHaveTourDetailsWithServices(string servicesString)
    {
        ContextHelpers.SetupValidTour(tourContext);
        tourContext.IncludedServices.Clear();
        tourContext.IncludedServices.AddRange(servicesString.Split(", "));
    }

    [Given(@"I have tour details with base price (.*), single room (.*), regular bike (.*), e-bike (.*)")]
    public void GivenIHaveTourDetailsWithAllPrices(decimal basePrice, decimal singleRoom, decimal regularBike, decimal eBike)
    {
        tourContext.Identifier = "TEST2024";
        tourContext.Name = "Test Tour";
        tourContext.StartDate = DateTime.UtcNow.AddMonths(1);
        tourContext.EndDate = DateTime.UtcNow.AddMonths(1).AddDays(7);
        tourContext.BasePrice = basePrice;
        tourContext.DoubleRoomSupplementPrice = singleRoom;
        tourContext.RegularBikePrice = regularBike;
        tourContext.EBikePrice = eBike;
    }

    [When(@"I create the tour")]
    public void WhenICreateTheTour()
    {
        tourContext.Tour = TestHelpers.CreateTestTourWithDates(tourContext.StartDate, tourContext.EndDate);
    }

    [When(@"I try to create the tour")]
    public void WhenITryToCreateTheTour()
    {
        var services = tourContext.IncludedServices.Count > 0
            ? tourContext.IncludedServices
            : ["Hotel", "Breakfast"];

        var scheduleResult = DateRange.Create(tourContext.StartDate, tourContext.EndDate);
        var pricingResult = TourPricing.Create(tourContext.BasePrice, tourContext.DoubleRoomSupplementPrice, tourContext.RegularBikePrice, tourContext.EBikePrice, Currency.UsDollar);
        var capacityResult = TourCapacity.Create(4, 12);

        if (scheduleResult.IsFailure)
        {
            tourContext.Result = scheduleResult;
            return;
        }

        if (pricingResult.IsFailure)
        {
            tourContext.Result = pricingResult;
            return;
        }

        if (capacityResult.IsFailure)
        {
            tourContext.Result = capacityResult;
            return;
        }

        tourContext.Result = Tour.Create(
            identifier: tourContext.Identifier,
            name: tourContext.Name,
            schedule: scheduleResult.Value,
            pricing: pricingResult.Value,
            capacity: capacityResult.Value,
            includedServices: services);

        if (tourContext.Result is Result<Tour> { IsSuccess: true } result)
        {
            tourContext.Tour = result.Value;
        }
    }

    [When(@"I update the services to ""(.*)""")]
    public void WhenIUpdateTheServicesTo(string servicesString)
    {
        var services = servicesString.Split(", ");
        tourContext.Tour.UpdateIncludedServices(services);
    }

    [Then(@"the tour should be created successfully")]
    public void ThenTheTourShouldBeCreatedSuccessfully()
    {
        Assert.NotNull(tourContext.Tour);
    }

    [Then(@"the tour creation should fail with argument exception ""(.*)""")]
    public void ThenTheTourCreationShouldFailWithArgumentException(string expectedMessage)
    {
        Assert.NotNull(tourContext.Result);

        var (isSuccess, errorDetail, validationErrors) = tourContext.Result switch
        {
            Result<Tour> tr => (tr.IsSuccess, tr.ErrorDetails?.Detail, tr.ErrorDetails?.ValidationErrors),
            Result<DateRange> dr => (dr.IsSuccess, dr.ErrorDetails?.Detail, dr.ErrorDetails?.ValidationErrors),
            Result<TourPricing> pr => (pr.IsSuccess, pr.ErrorDetails?.Detail, pr.ErrorDetails?.ValidationErrors),
            Result<TourCapacity> cr => (cr.IsSuccess, cr.ErrorDetails?.Detail, cr.ErrorDetails?.ValidationErrors),
            Result r => (r.IsSuccess, r.ErrorDetails?.Detail, r.ErrorDetails?.ValidationErrors),
            _ => throw new InvalidOperationException("Unexpected result type")
        };

        Assert.False(isSuccess);

        var messageFound = errorDetail?.Contains(expectedMessage, StringComparison.Ordinal) ?? false;
        if (!messageFound && validationErrors != null)
        {
            messageFound = validationErrors.Values
                .SelectMany(errors => errors)
                .Any(error => error.Contains(expectedMessage, StringComparison.Ordinal));
        }

        Assert.True(messageFound, $"Expected message '{expectedMessage}' not found in error details or validation errors.");
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

    [Then(@"the tour creation should fail with validation error for ""(.*)""")]
    public void ThenTheTourCreationShouldFailWithValidationErrorFor(string fieldName)
    {
        Assert.NotNull(tourContext.Result);

        var (isSuccess, validationErrors) = tourContext.Result switch
        {
            Result<Tour> r => (r.IsSuccess, r.ErrorDetails?.ValidationErrors),
            Result<TourPricing> r => (r.IsSuccess, r.ErrorDetails?.ValidationErrors),
            Result<DateRange> r => (r.IsSuccess, r.ErrorDetails?.ValidationErrors),
            Result<TourCapacity> r => (r.IsSuccess, r.ErrorDetails?.ValidationErrors),
            Result r => (r.IsSuccess, r.ErrorDetails?.ValidationErrors),
            _ => throw new InvalidOperationException($"Unexpected result type: {tourContext.Result.GetType().Name}")
        };

        Assert.False(isSuccess);
        Assert.True(validationErrors?.ContainsKey(fieldName) ?? false, $"Expected validation error for field '{fieldName}' but found: {string.Join(", ", validationErrors.Keys)}");
    }

    [Then(@"the tour creation should fail with multiple validation errors")]
    public void ThenTheTourCreationShouldFailWithMultipleValidationErrors()
    {
        Assert.NotNull(tourContext.Result);

        var (isSuccess, validationErrors) = tourContext.Result switch
        {
            Result<Tour> r => (r.IsSuccess, r.ErrorDetails?.ValidationErrors),
            Result<TourPricing> r => (r.IsSuccess, r.ErrorDetails?.ValidationErrors),
            Result<DateRange> r => (r.IsSuccess, r.ErrorDetails?.ValidationErrors),
            Result<TourCapacity> r => (r.IsSuccess, r.ErrorDetails?.ValidationErrors),
            Result r => (r.IsSuccess, r.ErrorDetails?.ValidationErrors),
            _ => throw new InvalidOperationException($"Unexpected result type: {tourContext.Result.GetType().Name}")
        };

        Assert.False(isSuccess);
        Assert.NotNull(validationErrors);

        var totalErrors = validationErrors.Values.SelectMany(e => e).Count();
        Assert.True(totalErrors > 1, $"Expected multiple validation errors but found {totalErrors}");
    }

    [Then(@"the tour single room supplement should be (.*)")]
    public void ThenTheTourSingleRoomSupplementShouldBe(decimal expectedPrice)
    {
        Assert.Equal(expectedPrice, tourContext.Tour.DoubleRoomSupplementPrice);
    }

    [Then(@"the tour regular bike price should be (.*)")]
    public void ThenTheTourRegularBikePriceShouldBe(decimal expectedPrice)
    {
        Assert.Equal(expectedPrice, tourContext.Tour.RegularBikePrice);
    }

    [Then(@"the tour e-bike price should be (.*)")]
    public void ThenTheTourEBikePriceShouldBe(decimal expectedPrice)
    {
        Assert.Equal(expectedPrice, tourContext.Tour.EBikePrice);
    }
}
