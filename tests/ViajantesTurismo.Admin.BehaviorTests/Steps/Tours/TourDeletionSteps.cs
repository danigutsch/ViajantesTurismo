using ViajantesTurismo.Admin.Application.Tours.DeleteTour;

namespace ViajantesTurismo.Admin.BehaviorTests.Steps.Tours;

[Binding]
public sealed class TourDeletionSteps(TourContext tourContext)
{
    [Given("a tour exists with no bookings")]
    public void Given_a_tour_exists_with_no_bookings()
    {
        tourContext.Tour = EntityBuilders.BuildTour();
        tourContext.TourStore.AddExistingTour(tourContext.Tour);
    }

    [Given("the tour has a pending booking")]
    public void Given_the_tour_has_a_pending_booking()
    {
        BookingTestHelpers.AddSingleCustomerBooking(tourContext.Tour).IsSuccess.ShouldBeTrue();
    }

    [Given("the tour has a confirmed booking")]
    public void Given_the_tour_has_a_confirmed_booking()
    {
        var booking = BookingTestHelpers.AddSingleCustomerBooking(tourContext.Tour).Value;
        tourContext.Tour.ConfirmBooking(booking.Id).IsSuccess.ShouldBeTrue();
    }

    [Given("the tour has a cancelled booking")]
    public void Given_the_tour_has_a_cancelled_booking()
    {
        var booking = BookingTestHelpers.AddSingleCustomerBooking(tourContext.Tour).Value;
        tourContext.Tour.CancelBooking(booking.Id).IsSuccess.ShouldBeTrue();
    }

    [Given("the tour has a completed booking")]
    public void Given_the_tour_has_a_completed_booking()
    {
        var booking = BookingTestHelpers.AddSingleCustomerBooking(tourContext.Tour).Value;
        tourContext.Tour.ConfirmBooking(booking.Id).IsSuccess.ShouldBeTrue();
        tourContext.Tour.CompleteBooking(booking.Id).IsSuccess.ShouldBeTrue();
    }

    [When("I retire the tour")]
    public async Task When_I_retire_the_tour()
    {
        var command = new DeleteTourCommand(tourContext.Tour.Id, "admin-subject", tourContext.TimeProvider.GetUtcNow());
        tourContext.DeleteResult = await tourContext.DeleteTourCommandHandler.Handle(command, CancellationToken.None);
    }

    [Then("the stored tour should be retired")]
    public async Task Then_the_stored_tour_should_be_retired()
    {
        tourContext.DeleteResult?.IsSuccess.ShouldBeTrue();
        var storedTour = await tourContext.TourStore.GetById(tourContext.Tour.Id, CancellationToken.None);
        storedTour.ShouldNotBeNull();
        storedTour.IsRetired.ShouldBeTrue();
    }

    [Then("retirement should conflict with the active booking")]
    public void Then_retirement_should_conflict_with_the_active_booking()
    {
        tourContext.DeleteResult?.Status.ShouldBe(ResultStatus.Conflict);
        tourContext.Tour.IsRetired.ShouldBeFalse();
        tourContext.Tour.Bookings.Count.ShouldBe(1);
    }

    [Then("the historical booking should be retained")]
    public void Then_the_historical_booking_should_be_retained()
    {
        tourContext.Tour.Bookings.Count.ShouldBe(1);
    }
}
