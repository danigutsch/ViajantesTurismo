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

    [Given(@"I have UTC tour dates from ""(.*)"" to ""(.*)""")]
    public void GivenIHaveUtcTourDatesFromTo(string startDateString, string endDateString)
    {
        ContextHelpers.SetupValidTour(tourContext);
        tourContext.StartDate =
            DateTime.Parse(startDateString, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal);
        tourContext.EndDate =
            DateTime.Parse(endDateString, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal);
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
            basePrice: 2000.00m,
            doubleRoomSupplementPrice: 500.00m,
            regularBikePrice: 100.00m,
            eBikePrice: 200.00m,
            currency: Currency.UsDollar,
            minCustomers: 4,
            maxCustomers: 12,
            includedServices: services).Value;
    }

    [Given(@"I have tour details with identifier ""(.*)"" and name ""(.*)""")]
    public void GivenIHaveTourDetailsWithIdentifierAndName(string identifier, string name)
    {
        ContextHelpers.SetupValidTour(tourContext);
        tourContext.Identifier = identifier;
        tourContext.Name = name;
    }

    [Given("I have tour details with identifier longer than 128 characters")]
    public void GivenIHaveTourDetailsWithIdentifierLongerThan128Characters()
    {
        ContextHelpers.SetupValidTour(tourContext);
        tourContext.Identifier = new string('A', 129);
        tourContext.Name = "Valid Tour Name";
    }

    [Given("I have tour details with name longer than 128 characters")]
    public void GivenIHaveTourDetailsWithNameLongerThan128Characters()
    {
        ContextHelpers.SetupValidTour(tourContext);
        tourContext.Identifier = "VALID2024";
        tourContext.Name = new string('A', 129);
    }

    [Given("I have tour details with multiple invalid values")]
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

    [Given("I have tour details with (base price|single room supplement|regular bike price|e-bike price) (.*)")]
    public void GivenIHaveTourDetailsWithPriceType(string priceType, decimal amount)
    {
        tourContext.Identifier = "TEST2024";
        tourContext.Name = "Test Tour";
        tourContext.StartDate = DateTime.UtcNow.AddMonths(1);
        tourContext.EndDate = DateTime.UtcNow.AddMonths(1).AddDays(7);

        switch (priceType)
        {
            case "base price":
                tourContext.BasePrice = amount;
                tourContext.DoubleRoomSupplementPrice = 500.00m;
                tourContext.RegularBikePrice = 100.00m;
                tourContext.EBikePrice = 200.00m;
                break;
            case "single room supplement":
                tourContext.DoubleRoomSupplementPrice = amount;
                tourContext.BasePrice = 2000.00m;
                tourContext.RegularBikePrice = 100.00m;
                tourContext.EBikePrice = 200.00m;
                break;
            case "regular bike price":
                tourContext.RegularBikePrice = amount;
                tourContext.BasePrice = 2000.00m;
                tourContext.DoubleRoomSupplementPrice = 500.00m;
                tourContext.EBikePrice = 200.00m;
                break;
            case "e-bike price":
                tourContext.EBikePrice = amount;
                tourContext.BasePrice = 2000.00m;
                tourContext.DoubleRoomSupplementPrice = 500.00m;
                tourContext.RegularBikePrice = 100.00m;
                break;
            default:
                throw new ArgumentException($"Unknown price type: {priceType}");
        }
    }

    [Given(@"I have tour details with services ""(.*)""")]
    public void GivenIHaveTourDetailsWithServices(string servicesString)
    {
        ContextHelpers.SetupValidTour(tourContext);
        tourContext.IncludedServices.Clear();
        foreach (var service in servicesString.Split(", "))
        {
            tourContext.IncludedServices.Add(service);
        }
    }

    [Given("I have tour details with base price (.*), single room (.*), regular bike (.*), e-bike (.*)")]
    public void GivenIHaveTourDetailsWithAllPrices(decimal basePrice, decimal singleRoom, decimal regularBike,
        decimal eBike)
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

    [When("I create the tour")]
    public void WhenICreateTheTour()
    {
        tourContext.Tour = TestHelpers.CreateTestTourWithDates(tourContext.StartDate, tourContext.EndDate);
    }

    [When("I try to create the tour")]
    [When("I attempt to create the tour")]
    public void WhenITryToCreateTheTour()
    {
        var services = tourContext.IncludedServices.Count > 0
            ? tourContext.IncludedServices
            : ["Hotel", "Breakfast"];

        tourContext.Result = Tour.Create(
            identifier: tourContext.Identifier,
            name: tourContext.Name,
            startDate: tourContext.StartDate,
            endDate: tourContext.EndDate,
            basePrice: tourContext.BasePrice,
            doubleRoomSupplementPrice: tourContext.DoubleRoomSupplementPrice,
            regularBikePrice: tourContext.RegularBikePrice,
            eBikePrice: tourContext.EBikePrice,
            currency: Currency.UsDollar,
            minCustomers: 4,
            maxCustomers: 12,
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

    [Then("the tour should be created successfully")]
    public void ThenTheTourShouldBeCreatedSuccessfully()
    {
        Assert.NotNull(tourContext.Tour);
    }

    [Then("I should not be able to create the tour")]
    public void ThenIShouldNotBeAbleToCreateTheTour()
    {
        Assert.NotNull(tourContext.Result);

        var isSuccess = tourContext.Result switch
        {
            Result<Tour> r => r.IsSuccess,
            Result<TourPricing> r => r.IsSuccess,
            Result<DateRange> r => r.IsSuccess,
            Result<TourCapacity> r => r.IsSuccess,
            Result r => r.IsSuccess,
            _ => throw new InvalidOperationException($"Unexpected result type: {tourContext.Result.GetType().Name}")
        };

        Assert.False(isSuccess, "Expected the tour creation to fail, but it succeeded.");
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

        Assert.True(messageFound,
            $"Expected message '{expectedMessage}' not found in error details or validation errors.");
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

    [Then("the tour base price should be (.*)")]
    public void ThenTheTourBasePriceShouldBe(decimal expectedPrice)
    {
        Assert.Equal(expectedPrice, tourContext.Tour.Pricing.BasePrice);
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
        Assert.True(validationErrors?.ContainsKey(fieldName) ?? false,
            $"Expected validation error for field '{fieldName}' but found: {string.Join(", ", validationErrors.Keys)}");
    }

    [Then("the tour creation should fail with multiple validation errors")]
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

    [Then("the tour single room supplement should be (.*)")]
    public void ThenTheTourSingleRoomSupplementShouldBe(decimal expectedPrice)
    {
        Assert.Equal(expectedPrice, tourContext.Tour.Pricing.DoubleRoomSupplementPrice);
    }

    [Then("the tour regular bike price should be (.*)")]
    public void ThenTheTourRegularBikePriceShouldBe(decimal expectedPrice)
    {
        Assert.Equal(expectedPrice, tourContext.Tour.Pricing.RegularBikePrice);
    }

    [Then("the tour e-bike price should be (.*)")]
    public void ThenTheTourEBikePriceShouldBe(decimal expectedPrice)
    {
        Assert.Equal(expectedPrice, tourContext.Tour.Pricing.EBikePrice);
    }

    [Then("the tour should preserve UTC time zone")]
    public void ThenTheTourShouldPreserveUtcTimeZone()
    {
        Assert.NotNull(tourContext.Tour);
        Assert.Equal(DateTimeKind.Utc, tourContext.Tour.Schedule.StartDate.Kind);
        Assert.Equal(DateTimeKind.Utc, tourContext.Tour.Schedule.EndDate.Kind);
    }

    [Then("the tour duration should be greater than (.*) days")]
    public void ThenTheTourDurationShouldBeGreaterThanDays(int days)
    {
        Assert.NotNull(tourContext.Tour);
        var duration = (tourContext.Tour.Schedule.EndDate - tourContext.Tour.Schedule.StartDate).TotalDays;
        Assert.True(duration > days, $"Expected duration greater than {days} days but got {duration:F1}");
    }

    [Then("the tour duration should be (.*) days")]
    public void ThenTheTourDurationShouldBeDays(int expectedDays)
    {
        Assert.NotNull(tourContext.Tour);
        var duration = (tourContext.Tour.Schedule.EndDate - tourContext.Tour.Schedule.StartDate).TotalDays;
        Assert.Equal(expectedDays, duration);
    }

    [Then(@"the tour StartDate should be ""(.*)""")]
    public void ThenTheTourStartDateShouldBe(string expectedDateString)
    {
        Assert.NotNull(tourContext.Tour);
        var expectedDate =
            DateTime.Parse(expectedDateString, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
        Assert.Equal(expectedDate, tourContext.Tour.Schedule.StartDate);
    }

    [Then(@"the tour EndDate should be ""(.*)""")]
    public void ThenTheTourEndDateShouldBe(string expectedDateString)
    {
        Assert.NotNull(tourContext.Tour);
        var expectedDate =
            DateTime.Parse(expectedDateString, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
        Assert.Equal(expectedDate, tourContext.Tour.Schedule.EndDate);
    }

    [Then("I should be informed that the end date must be after the start date")]
    public void ThenIShouldBeInformedThatTheEndDateMustBeAfterTheStartDate()
    {
        ThenTheTourCreationShouldFailWithArgumentException("End date must be after start date.");
    }

    [Then("I should be informed that tours must last at least 5 days")]
    public void ThenIShouldBeInformedThatToursMustLastAtLeast5Days()
    {
        ThenTheTourCreationShouldFailWithValidationErrorFor("schedule");
    }

    [Then("I should be prompted to provide a tour identifier")]
    public void ThenIShouldBePromptedToProvideATourIdentifier()
    {
        ThenTheTourCreationShouldFailWithValidationErrorFor("identifier");
    }

    [Then("I should be informed that the identifier is too long")]
    public void ThenIShouldBeInformedThatTheIdentifierIsTooLong()
    {
        ThenTheTourCreationShouldFailWithValidationErrorFor("identifier");
    }

    [Then("I should be prompted to provide a tour name")]
    public void ThenIShouldBePromptedToProvideATourName()
    {
        ThenTheTourCreationShouldFailWithValidationErrorFor("name");
    }

    [Then("I should be informed that the name is too long")]
    public void ThenIShouldBeInformedThatTheNameIsTooLong()
    {
        ThenTheTourCreationShouldFailWithValidationErrorFor("name");
    }

    [Then("I should be informed that prices must be positive")]
    public void ThenIShouldBeInformedThatPricesMustBePositive()
    {
        ThenTheTourCreationShouldFailWithValidationErrorFor("price");
    }

    [Then("I should be informed that the price exceeds our maximum rate")]
    public void ThenIShouldBeInformedThatThePriceExceedsOurMaximumRate()
    {
        ThenTheTourCreationShouldFailWithValidationErrorFor("price");
    }
}
