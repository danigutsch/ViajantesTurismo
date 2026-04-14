using ViajantesTurismo.Admin.Application.Tours.UpdateTour;

using ViajantesTurismo.Admin.Domain.Shared;

namespace ViajantesTurismo.Admin.BehaviorTests.Steps.Tours;

[Binding]
public sealed class TourUpdateDetailsSteps(TourContext tourContext)
{
    [Given(@"a tour exists with identifier ""(.*)"" and has (\d+) booking")]
    public void GivenATourExistsWithIdentifierAndHasBooking(string identifier, int bookingCount)
    {
        tourContext.Tour = EntityBuilders.BuildTour(new TourOptions(Identifier: identifier));
        tourContext.TourStore.AddExistingTour(tourContext.Tour);
        for (var i = 0; i < bookingCount; i++)
        {
            tourContext.Tour.AddBooking(new TourBookingRequest(
                Guid.CreateVersion7(),
                BikeType.Regular,
                RoomType.DoubleOccupancy,
                DiscountType.None));
        }
    }

    [Given(@"a tour exists with identifier ""(.*)"" and name ""(.*)""")]
    public void GivenATourExistsWithIdentifierAndName(string identifier, string name)
    {
        tourContext.Tour = EntityBuilders.BuildTour(new TourOptions(
            Identifier: identifier,
            Name: name));
        tourContext.TourStore.AddExistingTour(tourContext.Tour);
    }

    [Given(@"another tour exists with identifier ""(.*)""")]
    public void GivenAnotherTourExistsWithIdentifier(string identifier)
    {
        var anotherTour = EntityBuilders.BuildTour(new TourOptions(
            Identifier: identifier,
            Name: "Another Tour"));
        tourContext.TourStore.AddExistingTour(anotherTour);
    }

    [When(@"I try to update the tour details to identifier ""(.*)"" and name ""(.*)""")]
    public async Task WhenITryToUpdateTheTourDetailsToIdentifierAndName(string identifier, string name)
    {
        var command = new UpdateTourCommand(
            tourContext.Tour.Id,
            identifier,
            name,
            tourContext.Tour.Schedule.StartDate,
            tourContext.Tour.Schedule.EndDate,
            tourContext.Tour.Pricing.BasePrice,
            tourContext.Tour.Pricing.SingleRoomSupplementPrice,
            tourContext.Tour.Pricing.RegularBikePrice,
            tourContext.Tour.Pricing.EBikePrice,
            tourContext.Tour.Pricing.Currency,
            [.. tourContext.Tour.IncludedServices],
            tourContext.Tour.Capacity.MinCustomers,
            tourContext.Tour.Capacity.MaxCustomers);

        tourContext.UpdateResult = await tourContext.UpdateTourCommandHandler.Handle(command, CancellationToken.None);
    }

    [When(@"I update the tour details to identifier ""(.*)"" and name ""(.*)""")]
    public async Task WhenIUpdateTheTourDetailsToIdentifierAndName(string identifier, string name)
    {
        await WhenITryToUpdateTheTourDetailsToIdentifierAndName(identifier, name);
    }

    [When(@"I update the tour details to identifier with (.*) characters and name ""(.*)""")]
    public async Task WhenIUpdateTheTourDetailsToIdentifierWithCharactersAndName(int length, string name)
    {
        var longIdentifier = new string('X', length);
        var command = new UpdateTourCommand(
            tourContext.Tour.Id,
            longIdentifier,
            name,
            tourContext.Tour.Schedule.StartDate,
            tourContext.Tour.Schedule.EndDate,
            tourContext.Tour.Pricing.BasePrice,
            tourContext.Tour.Pricing.SingleRoomSupplementPrice,
            tourContext.Tour.Pricing.RegularBikePrice,
            tourContext.Tour.Pricing.EBikePrice,
            tourContext.Tour.Pricing.Currency,
            tourContext.Tour.IncludedServices.ToList(),
            tourContext.Tour.Capacity.MinCustomers,
            tourContext.Tour.Capacity.MaxCustomers);

        tourContext.UpdateResult = await tourContext.UpdateTourCommandHandler.Handle(command, CancellationToken.None);
    }

    [When(@"I update the tour details to identifier ""(.*)"" and name with (.*) characters")]
    public async Task WhenIUpdateTheTourDetailsToIdentifierAndNameWithCharacters(string identifier, int length)
    {
        var longName = new string('X', length);
        var command = new UpdateTourCommand(
            tourContext.Tour.Id,
            identifier,
            longName,
            tourContext.Tour.Schedule.StartDate,
            tourContext.Tour.Schedule.EndDate,
            tourContext.Tour.Pricing.BasePrice,
            tourContext.Tour.Pricing.SingleRoomSupplementPrice,
            tourContext.Tour.Pricing.RegularBikePrice,
            tourContext.Tour.Pricing.EBikePrice,
            tourContext.Tour.Pricing.Currency,
            tourContext.Tour.IncludedServices.ToList(),
            tourContext.Tour.Capacity.MinCustomers,
            tourContext.Tour.Capacity.MaxCustomers);

        tourContext.UpdateResult = await tourContext.UpdateTourCommandHandler.Handle(command, CancellationToken.None);
    }

    [Then("the tour details update should succeed")]
    public void ThenTheTourDetailsUpdateShouldSucceed()
    {
        Assert.NotNull(tourContext.UpdateResult);
        Assert.True(tourContext.UpdateResult.Value.IsSuccess,
            $"Expected success but got error: {tourContext.UpdateResult.Value.ErrorDetails}");
    }

    [Then("the tour details update should fail")]
    public void ThenTheTourDetailsUpdateShouldFail()
    {
        Assert.NotNull(tourContext.UpdateResult);
        Assert.False(tourContext.UpdateResult.Value.IsSuccess);
    }

    [Then(@"the tour should have identifier ""(.*)""")]
    public async Task ThenTheTourShouldHaveIdentifier(string expectedIdentifier)
    {
        var tour = await tourContext.TourStore.GetById(tourContext.Tour.Id, CancellationToken.None);
        Assert.NotNull(tour);
        Assert.Equal(expectedIdentifier, tour.Identifier);
    }

    [Then(@"the tour should have name ""(.*)""")]
    public async Task ThenTheTourShouldHaveName(string expectedName)
    {
        var tour = await tourContext.TourStore.GetById(tourContext.Tour.Id, CancellationToken.None);
        Assert.NotNull(tour);
        Assert.Equal(expectedName, tour.Name);
    }

    [Then(@"the error should contain ""(.*)""")]
    public void ThenTheErrorShouldContain(string expectedText)
    {
        Assert.NotNull(tourContext.UpdateResult);
        var errorMessage = tourContext.UpdateResult.Value.ErrorDetails?.Detail ?? string.Empty;
        Assert.Contains(expectedText, errorMessage, StringComparison.OrdinalIgnoreCase);
    }
}
