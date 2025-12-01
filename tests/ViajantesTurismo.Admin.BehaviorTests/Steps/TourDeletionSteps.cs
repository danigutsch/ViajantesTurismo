using Reqnroll;
using ViajantesTurismo.Admin.Application.Tours.DeleteTour;
using ViajantesTurismo.Admin.BehaviorTests.Context;

namespace ViajantesTurismo.Admin.BehaviorTests.Steps;

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
        Assert.True(result.IsSuccess);
    }

    [Given("the tour has a confirmed booking")]
    public void GivenTheTourHasAConfirmedBooking()
    {
        if (tourContext.TourStore.GetById(tourContext.Tour.Id, CancellationToken.None).Result is null)
        {
            tourContext.TourStore.AddExistingTour(tourContext.Tour);
        }

        var addResult = BookingTestHelpers.AddSingleCustomerBooking(tourContext.Tour);
        Assert.True(addResult.IsSuccess);
        var confirmResult = tourContext.Tour.ConfirmBooking(addResult.Value.Id);
        Assert.True(confirmResult.IsSuccess);
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
            Assert.True(addResult.IsSuccess);
            var confirmResult = tourContext.Tour.ConfirmBooking(addResult.Value.Id);
            Assert.True(confirmResult.IsSuccess);
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
        Assert.True(addResult.IsSuccess);
        var cancelResult = tourContext.Tour.CancelBooking(addResult.Value.Id);
        Assert.True(cancelResult.IsSuccess);
    }

    [When("I delete the tour")]
    public async Task WhenIDeleteTheTour()
    {
        var command = new DeleteTourCommand(tourContext.Tour.Id);
        var result = await tourContext.DeleteTourCommandHandler.Handle(command, CancellationToken.None);
        tourContext.DeleteResult = result;
    }

    [When("I attempt to delete the tour")]
    public async Task WhenIAttemptToDeleteTheTour()
    {
        var command = new DeleteTourCommand(tourContext.Tour.Id);
        var result = await tourContext.DeleteTourCommandHandler.Handle(command, CancellationToken.None);
        tourContext.DeleteResult = result;
    }

    [Then("the tour should be deleted successfully")]
    public void ThenTheTourShouldBeDeletedSuccessfully()
    {
        Assert.True(tourContext.DeleteResult?.IsSuccess);
    }

    [Then("the deletion should fail")]
    public void ThenTheDeletionShouldFail()
    {
        Assert.False(tourContext.DeleteResult?.IsSuccess);
    }
}
