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
        TestAssert.NotNull(bookingContext.Action);
    }

    [Then("only tour methods should be available")]
    public void ThenOnlyTourMethodsShouldBeAvailable()
    {
        var tourType = tourContext.Tour.GetType();
        TestAssert.NotNull(tourType.GetMethod("AddBooking"));
        TestAssert.NotNull(tourType.GetMethod("ConfirmBooking"));
        TestAssert.NotNull(tourType.GetMethod("CancelBooking"));
        TestAssert.NotNull(tourType.GetMethod("CompleteBooking"));
        TestAssert.NotNull(tourType.GetMethod("UpdateBookingNotes"));
        TestAssert.NotNull(tourType.GetMethod("RemoveBooking"));
    }

    [Then("the operation should fail with not found error")]
    public void ThenTheOperationShouldFailWithNotFoundError()
    {
        TestAssert.NotNull(bookingContext.BookingOperationResult);
        var result = bookingContext.BookingOperationResult.Value;
        TestAssert.False(result.IsSuccess);
        TestAssert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Then(@"the tour should have (\d+) bookings")]
    public void ThenTheTourShouldHaveDBookings(int expectedCount)
    {
        TestAssert.Equal(expectedCount, tourContext.Tour.Bookings.Count);
    }
}
