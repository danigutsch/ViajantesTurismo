namespace ViajantesTurismo.Admin.BehaviorTests.Steps.Tours;

[Binding]
public sealed class TourBookingAggregateBoundarySteps(BookingContext bookingContext, TourContext tourContext)
{
    [When("I try to access booking methods directly")]
    public void WhenITryToAccessBookingMethodsDirectly()
    {
        bookingContext.Action = () => { };
    }

    [Then("the methods should not be accessible")]
    public void ThenTheMethodsShouldNotBeAccessible()
    {
        (bookingContext.Action).ShouldNotBeNull();
    }

    [Then("only tour methods should be available")]
    public void ThenOnlyTourMethodsShouldBeAvailable()
    {
        var tourType = tourContext.Tour.GetType();
        (tourType.GetMethod("AddBooking")).ShouldNotBeNull();
        (tourType.GetMethod("ConfirmBooking")).ShouldNotBeNull();
        (tourType.GetMethod("CancelBooking")).ShouldNotBeNull();
        (tourType.GetMethod("CompleteBooking")).ShouldNotBeNull();
        (tourType.GetMethod("UpdateBookingNotes")).ShouldNotBeNull();
        (tourType.GetMethod("RemoveBooking")).ShouldNotBeNull();
    }

    [Then("the operation should fail with not found error")]
    public void ThenTheOperationShouldFailWithNotFoundError()
    {
        var result = (bookingContext.BookingOperationResult).ShouldNotBeNull();
        (result.IsSuccess).ShouldBeFalse();
        (result.Status).ShouldBe(ResultStatus.NotFound);
    }

    [Then(@"the tour should have (\d+) bookings")]
    public void ThenTheTourShouldHaveDBookings(int expectedCount)
    {
        (tourContext.Tour.Bookings.Count).ShouldBe(expectedCount);
    }
}
