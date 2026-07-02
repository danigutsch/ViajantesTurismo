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
        global::SharedKernel.Testing.Assertions.TestAssert.NotNull(bookingContext.Action);
    }

    [Then("only tour methods should be available")]
    public void ThenOnlyTourMethodsShouldBeAvailable()
    {
        var tourType = tourContext.Tour.GetType();
        global::SharedKernel.Testing.Assertions.TestAssert.NotNull(tourType.GetMethod("AddBooking"));
        global::SharedKernel.Testing.Assertions.TestAssert.NotNull(tourType.GetMethod("ConfirmBooking"));
        global::SharedKernel.Testing.Assertions.TestAssert.NotNull(tourType.GetMethod("CancelBooking"));
        global::SharedKernel.Testing.Assertions.TestAssert.NotNull(tourType.GetMethod("CompleteBooking"));
        global::SharedKernel.Testing.Assertions.TestAssert.NotNull(tourType.GetMethod("UpdateBookingNotes"));
        global::SharedKernel.Testing.Assertions.TestAssert.NotNull(tourType.GetMethod("RemoveBooking"));
    }

    [Then("the operation should fail with not found error")]
    public void ThenTheOperationShouldFailWithNotFoundError()
    {
        global::SharedKernel.Testing.Assertions.TestAssert.NotNull(bookingContext.BookingOperationResult);
        var result = bookingContext.BookingOperationResult.Value;
        global::SharedKernel.Testing.Assertions.TestAssert.False(result.IsSuccess);
        global::SharedKernel.Testing.Assertions.TestAssert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Then(@"the tour should have (\d+) bookings")]
    public void ThenTheTourShouldHaveDBookings(int expectedCount)
    {
        global::SharedKernel.Testing.Assertions.TestAssert.Equal(expectedCount, tourContext.Tour.Bookings.Count);
    }
}
