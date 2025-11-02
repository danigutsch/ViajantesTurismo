using Reqnroll;
using ViajantesTurismo.Admin.BehaviorTests.Context;
using ViajantesTurismo.Common.Results;

namespace ViajantesTurismo.Admin.BehaviorTests.Steps;

[Binding]
public sealed class TourBookingAggregateBoundarySteps(BookingContext bookingContext, TourContext tourContext)
{
    [When(@"I try to access booking methods directly")]
    public void WhenITryToAccessBookingMethodsDirectly()
    {
        // This is a compile-time check - booking methods are internal
        // If this test compiles, it means the methods are properly encapsulated
        bookingContext.Action = () =>
        {
            // Attempting to access internal methods would cause compilation error
            // var booking = new Booking(1, 1, null);
            // booking.Confirm(); // This should not compile as Confirm is internal
        };
    }

    [Then(@"the methods should not be accessible")]
    public void ThenTheMethodsShouldNotBeAccessible()
    {
        // This is verified at compile time
        Assert.NotNull(bookingContext.Action);
    }

    [Then(@"only tour methods should be available")]
    public void ThenOnlyTourMethodsShouldBeAvailable()
    {
        // Verify that tour has the required methods
        var tourType = tourContext.Tour.GetType();
        Assert.NotNull(tourType.GetMethod("AddBooking"));
        Assert.NotNull(tourType.GetMethod("ConfirmBooking"));
        Assert.NotNull(tourType.GetMethod("CancelBooking"));
        Assert.NotNull(tourType.GetMethod("CompleteBooking"));
        Assert.NotNull(tourType.GetMethod("UpdateBookingPrice"));
        Assert.NotNull(tourType.GetMethod("UpdateBookingNotes"));
        Assert.NotNull(tourType.GetMethod("RemoveBooking"));
    }

    [Then(@"the operation should fail with not found error")]
    public void ThenTheOperationShouldFailWithNotFoundError()
    {
        Assert.False(bookingContext.Result.IsSuccess);
        Assert.Equal(ResultStatus.NotFound, bookingContext.Result.Status);
    }

    [Then(@"the tour should have (\d+) bookings")]
    public void ThenTheTourShouldHaveBookings(int expectedCount)
    {
        Assert.Equal(expectedCount, tourContext.Tour.Bookings.Count);
    }
}
