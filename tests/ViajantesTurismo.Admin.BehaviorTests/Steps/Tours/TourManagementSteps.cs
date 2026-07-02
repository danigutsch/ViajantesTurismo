using ViajantesTurismo.Admin.Application.Tours.CreateTour;
using ViajantesTurismo.Admin.Contracts;
using ViajantesTurismo.Common.Monies;

namespace ViajantesTurismo.Admin.BehaviorTests.Steps.Tours;

[Binding]
public sealed class TourManagementSteps(TourContext tourContext)
{
    [Given(@"^a tour exists with identifier ""([^""]+)""$")]
    public void GivenATourExistsWithIdentifier(string identifier)
    {
        var tour = Tour.Create(new TourDefinition(
            identifier,
            "Existing Tour",
            DateTime.UtcNow.AddMonths(1),
            DateTime.UtcNow.AddMonths(1).AddDays(7),
            2000.00m,
            500.00m,
            100.00m,
            200.00m,
            Currency.UsDollar,
            4,
            12,
            ["Accommodation", "Meals"])).Value;

        tourContext.TourStore.AddExistingTour(tour);
    }

    [When(@"I attempt to create another tour with identifier ""(.*)""")]
    public async Task WhenIAttemptToCreateAnotherTourWithIdentifier(string identifier)
    {
        var command = new CreateTourCommand(
            identifier,
            "Another Tour",
            DateTime.UtcNow.AddMonths(2),
            DateTime.UtcNow.AddMonths(2).AddDays(7),
            2500.00m,
            600.00m,
            120.00m,
            220.00m,
            CurrencyDto.UsDollar,
            ["Accommodation", "Meals"],
            4,
            12);

        var result = await tourContext.CreateTourCommandHandler.Handle(command, CancellationToken.None);
        tourContext.CommandResult = result;
    }

    [Then("I should be informed that the tour identifier must be unique")]
    public void ThenIShouldBeInformedThatTheTourIdentifierMustBeUnique()
    {
        (tourContext.CommandResult).ShouldNotBeNull();
        (tourContext.CommandResult.Value.IsSuccess).ShouldBeFalse();
        (tourContext.CommandResult.Value.ErrorDetails).ShouldNotBeNull();
        (tourContext.CommandResult.Value.ErrorDetails.Detail).ShouldContain("already exists", StringComparison.OrdinalIgnoreCase);
    }

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
        ArgumentException.ThrowIfNullOrWhiteSpace(servicesString);
        var services = servicesString.Split(", ");
        tourContext.Tour = Tour.Create(new TourDefinition(
            "TEST2024",
            "Test Tour",
            DateTime.UtcNow.AddMonths(1),
            DateTime.UtcNow.AddMonths(1).AddDays(7),
            2000.00m,
            500.00m,
            100.00m,
            200.00m,
            Currency.UsDollar,
            4,
            12,
            services)).Value;
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
        tourContext.SingleRoomSupplementPrice = 0m;
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
                tourContext.SingleRoomSupplementPrice = 500.00m;
                tourContext.RegularBikePrice = 100.00m;
                tourContext.EBikePrice = 200.00m;
                break;
            case "single room supplement":
                tourContext.SingleRoomSupplementPrice = amount;
                tourContext.BasePrice = 2000.00m;
                tourContext.RegularBikePrice = 100.00m;
                tourContext.EBikePrice = 200.00m;
                break;
            case "regular bike price":
                tourContext.RegularBikePrice = amount;
                tourContext.BasePrice = 2000.00m;
                tourContext.SingleRoomSupplementPrice = 500.00m;
                tourContext.EBikePrice = 200.00m;
                break;
            case "e-bike price":
                tourContext.EBikePrice = amount;
                tourContext.BasePrice = 2000.00m;
                tourContext.SingleRoomSupplementPrice = 500.00m;
                tourContext.RegularBikePrice = 100.00m;
                break;
            default:
                throw new ArgumentException($"Unknown price type: {priceType}");
        }
    }

    [Given(@"I have tour details with services ""(.*)""")]
    public void GivenIHaveTourDetailsWithServices(string servicesString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(servicesString);
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
        tourContext.SingleRoomSupplementPrice = singleRoom;
        tourContext.RegularBikePrice = regularBike;
        tourContext.EBikePrice = eBike;
    }

    [When("I create the tour")]
    public void WhenICreateTheTour()
    {
        tourContext.Tour = EntityBuilders.BuildTour(new TourOptions(
            Schedule: new TourScheduleOptions(
                StartDate: tourContext.StartDate,
                EndDate: tourContext.EndDate)));
    }

    [When("I try to create the tour")]
    [When("I attempt to create the tour")]
    public void WhenITryToCreateTheTour()
    {
        var services = tourContext.IncludedServices.Count > 0
            ? tourContext.IncludedServices
            : ["Hotel", "Breakfast"];

        var result = Tour.Create(new TourDefinition(
            tourContext.Identifier,
            tourContext.Name,
            tourContext.StartDate,
            tourContext.EndDate,
            tourContext.BasePrice,
            tourContext.SingleRoomSupplementPrice,
            tourContext.RegularBikePrice,
            tourContext.EBikePrice,
            Currency.UsDollar,
            4,
            12,
            [.. services]));

        tourContext.CreationResult = result;

        if (result.IsSuccess)
        {
            tourContext.Tour = result.Value;
        }
    }

    [When(@"I update the services to ""(.*)""")]
    public void WhenIUpdateTheServicesTo(string servicesString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(servicesString);
        var services = servicesString.Split(", ");
        tourContext.Tour.UpdateIncludedServices(services);
    }

    [Then("the tour should be created successfully")]
    public void ThenTheTourShouldBeCreatedSuccessfully()
    {
        (tourContext.Tour).ShouldNotBeNull();
    }

    [Then("I should not be able to create the tour")]
    public void ThenIShouldNotBeAbleToCreateTheTour()
    {
        // Check either CreationResult (domain call) or CommandResult (command handler call)
        if (tourContext.CreationResult.HasValue)
        {
            (tourContext.CreationResult.Value.IsFailure).ShouldBeTrue("Expected the tour creation to fail, but it succeeded.");
        }
        else if (tourContext.CommandResult.HasValue)
        {
            (tourContext.CommandResult.Value.IsFailure).ShouldBeTrue("Expected the tour creation to fail, but it succeeded.");
        }
        else
        {
            false.ShouldBeTrue("No creation result found. Ensure the When step sets either CreationResult or CommandResult.");
        }
    }

    [Then(@"the tour creation should fail with argument exception ""(.*)""")]
    public void ThenTheTourCreationShouldFailWithArgumentException(string expectedMessage)
    {
        (tourContext.CreationResult).ShouldNotBeNull();
        (tourContext.CreationResult.Value.IsFailure).ShouldBeTrue();

        var errorDetails = tourContext.CreationResult.Value.ErrorDetails;
        var messageFound = errorDetails?.Detail.Contains(expectedMessage, StringComparison.Ordinal) ?? false;

        if (!messageFound && errorDetails?.ValidationErrors != null)
        {
            messageFound = errorDetails.ValidationErrors.Values
                .SelectMany(errors => errors)
                .Any(error => error.Contains(expectedMessage, StringComparison.Ordinal));
        }

        (messageFound).ShouldBeTrue($"Expected message '{expectedMessage}' not found in error details or validation errors.");
    }

    [Then(@"the tour identifier should be ""(.*)""")]
    public void ThenTheTourIdentifierShouldBe(string expectedIdentifier)
    {
        (tourContext.Tour.Identifier).ShouldBe(expectedIdentifier);
    }

    [Then(@"the tour name should be ""(.*)""")]
    public void ThenTheTourNameShouldBe(string expectedName)
    {
        (tourContext.Tour.Name).ShouldBe(expectedName);
    }

    [Then("the tour base price should be (.*)")]
    public void ThenTheTourBasePriceShouldBe(decimal expectedPrice)
    {
        (tourContext.Tour.Pricing.BasePrice).ShouldBe(expectedPrice);
    }

    [Then(@"the tour should include services ""(.*)""")]
    public void ThenTheTourShouldIncludeServices(string servicesString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(servicesString);
        var expectedServices = servicesString.Split(", ");
        (tourContext.Tour.IncludedServices.Count).ShouldBe(expectedServices.Length);

        foreach (var service in expectedServices)
        {
            (tourContext.Tour.IncludedServices).ShouldContain(service);
        }
    }

    [Then(@"the tour creation should fail with validation error for ""(.*)""")]
    public void ThenTheTourCreationShouldFailWithValidationErrorFor(string fieldName)
    {
        (tourContext.CreationResult).ShouldNotBeNull();
        (tourContext.CreationResult.Value.IsFailure).ShouldBeTrue();

        var validationErrors = tourContext.CreationResult.Value.ErrorDetails?.ValidationErrors;
        var foundKeys = validationErrors?.Keys.ToArray() ?? [];
        (validationErrors?.ContainsKey(fieldName) ?? false).ShouldBeTrue($"Expected validation error for field '{fieldName}' but found: {string.Join(", ", foundKeys)}");
    }

    [Then("the tour creation should fail with multiple validation errors")]
    public void ThenTheTourCreationShouldFailWithMultipleValidationErrors()
    {
        (tourContext.CreationResult).ShouldNotBeNull();
        (tourContext.CreationResult.Value.IsFailure).ShouldBeTrue();

        var validationErrors = tourContext.CreationResult.Value.ErrorDetails?.ValidationErrors;
        (validationErrors).ShouldNotBeNull();

        var totalErrors = validationErrors.Values.SelectMany(e => e).Count();
        (totalErrors > 1).ShouldBeTrue($"Expected multiple validation errors but found {totalErrors}");
    }

    [Then("the tour single room supplement should be (.*)")]
    public void ThenTheTourSingleRoomSupplementShouldBe(decimal expectedPrice)
    {
        (tourContext.Tour.Pricing.SingleRoomSupplementPrice).ShouldBe(expectedPrice);
    }

    [Then("the tour regular bike price should be (.*)")]
    public void ThenTheTourRegularBikePriceShouldBe(decimal expectedPrice)
    {
        (tourContext.Tour.Pricing.RegularBikePrice).ShouldBe(expectedPrice);
    }

    [Then("the tour e-bike price should be (.*)")]
    public void ThenTheTourEBikePriceShouldBe(decimal expectedPrice)
    {
        (tourContext.Tour.Pricing.EBikePrice).ShouldBe(expectedPrice);
    }

    [Then("the tour should preserve UTC time zone")]
    public void ThenTheTourShouldPreserveUtcTimeZone()
    {
        (tourContext.Tour).ShouldNotBeNull();
        (tourContext.Tour.Schedule.StartDate.Kind).ShouldBe(DateTimeKind.Utc);
        (tourContext.Tour.Schedule.EndDate.Kind).ShouldBe(DateTimeKind.Utc);
    }

    [Then("the tour duration should be greater than (.*) days")]
    public void ThenTheTourDurationShouldBeGreaterThanDays(int days)
    {
        (tourContext.Tour).ShouldNotBeNull();
        var duration = (tourContext.Tour.Schedule.EndDate - tourContext.Tour.Schedule.StartDate).TotalDays;
        (duration > days).ShouldBeTrue($"Expected duration greater than {days} days but got {duration:F1}");
    }

    [Then("the tour duration should be (.*) days")]
    public void ThenTheTourDurationShouldBeDays(int expectedDays)
    {
        (tourContext.Tour).ShouldNotBeNull();
        var duration = (tourContext.Tour.Schedule.EndDate - tourContext.Tour.Schedule.StartDate).TotalDays;
        (duration).ShouldBe(expectedDays);
    }

    [Then(@"the tour StartDate should be ""(.*)""")]
    public void ThenTheTourStartDateShouldBe(string expectedDateString)
    {
        (tourContext.Tour).ShouldNotBeNull();
        var expectedDate =
            DateTime.Parse(expectedDateString, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
        (tourContext.Tour.Schedule.StartDate).ShouldBe(expectedDate);
    }

    [Then(@"the tour EndDate should be ""(.*)""")]
    public void ThenTheTourEndDateShouldBe(string expectedDateString)
    {
        (tourContext.Tour).ShouldNotBeNull();
        var expectedDate =
            DateTime.Parse(expectedDateString, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
        (tourContext.Tour.Schedule.EndDate).ShouldBe(expectedDate);
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
