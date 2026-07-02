using ViajantesTurismo.Admin.Application.Tours.DeleteTour;

namespace ViajantesTurismo.Admin.BehaviorTests.Steps.Tours;

[Binding]
public sealed class TourDeletionSteps(TourContext tourContext)
{
    [Given("a tour exists with no bookings")]
    public void GivenATourExistsWithNoBookings()
    {
        tourContext.Tour = EntityBuilders.BuildTour();
        tourContext.TourStore.AddExistingTour(tourContext.Tour);
    }

    [Given("the tour has a pending booking")]
    public void GivenTheTourHasAPendingBooking()
    {
        tourContext.TourStore.AddExistingTour(tourContext.Tour);
        var result = BookingTestHelpers.AddSingleCustomerBooking(tourContext.Tour);
        (result.IsSuccess).ShouldBeTrue();
    }

    [Given("the tour has a confirmed booking")]
    public void GivenTheTourHasAConfirmedBooking()
    {
        if (tourContext.TourStore.GetById(tourContext.Tour.Id, CancellationToken.None).Result is null)
        {
            tourContext.TourStore.AddExistingTour(tourContext.Tour);
        }

        var addResult = BookingTestHelpers.AddSingleCustomerBooking(tourContext.Tour);
        (addResult.IsSuccess).ShouldBeTrue();
        var confirmResult = tourContext.Tour.ConfirmBooking(addResult.Value.Id);
        (confirmResult.IsSuccess).ShouldBeTrue();
    }

    [Given(@"the tour has (\d+) confirmed bookings")]
    public void GivenTheTourHasConfirmedBookings(int count)
    {
        if (tourContext.TourStore.GetById(tourContext.Tour.Id, CancellationToken.None).Result is null)
        {
            tourContext.TourStore.AddExistingTour(tourContext.Tour);
        }

        for (var i = 0; i < count; i++)
        {
            var addResult = BookingTestHelpers.AddSingleCustomerBooking(tourContext.Tour);
            (addResult.IsSuccess).ShouldBeTrue();
            var confirmResult = tourContext.Tour.ConfirmBooking(addResult.Value.Id);
            (confirmResult.IsSuccess).ShouldBeTrue();
        }
    }

    [Given("the tour has a cancelled booking")]
    public void GivenTheTourHasACancelledBooking()
    {
        if (tourContext.TourStore.GetById(tourContext.Tour.Id, CancellationToken.None).Result is null)
        {
            tourContext.TourStore.AddExistingTour(tourContext.Tour);
        }

        var addResult = BookingTestHelpers.AddSingleCustomerBooking(tourContext.Tour);
        (addResult.IsSuccess).ShouldBeTrue();
        var cancelResult = tourContext.Tour.CancelBooking(addResult.Value.Id);
        (cancelResult.IsSuccess).ShouldBeTrue();
    }

    [When("I delete the tour")]
    public async Task WhenIDeleteTheTour()
    {
        var command = new DeleteTourCommand(tourContext.Tour.Id);
        var result = await tourContext.DeleteTourCommandHandler.Handle(command, CancellationToken.None);
        tourContext.DeleteResult = result;
    }

    [When("I attempt to delete the tour")]
    public Task WhenIAttemptToDeleteTheTour()
    {
        return WhenIDeleteTheTour();
    }

    [Then("the tour should be deleted successfully")]
    public void ThenTheTourShouldBeDeletedSuccessfully()
    {
        (tourContext.DeleteResult?.IsSuccess).ShouldBeTrue();
    }

    [Then("the deletion should fail")]
    public void ThenTheDeletionShouldFail()
    {
        (tourContext.DeleteResult?.IsSuccess).ShouldBeFalse();
    }
}
