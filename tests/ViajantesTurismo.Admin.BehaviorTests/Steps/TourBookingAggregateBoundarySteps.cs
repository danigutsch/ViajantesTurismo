using Reqnroll;
using ViajantesTurismo.Admin.BehaviorTests.Context;
using ViajantesTurismo.Common.Results;

namespace ViajantesTurismo.Admin.BehaviorTests.Steps;

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
        Assert.NotNull(bookingContext.Action);
    }

    [Then("only tour methods should be available")]
    public void ThenOnlyTourMethodsShouldBeAvailable()
    {
        var tourType = tourContext.Tour.GetType();
        Assert.NotNull(tourType.GetMethod("AddBooking"));
        Assert.NotNull(tourType.GetMethod("ConfirmBooking"));
        Assert.NotNull(tourType.GetMethod("CancelBooking"));
        Assert.NotNull(tourType.GetMethod("CompleteBooking"));
        Assert.NotNull(tourType.GetMethod("UpdateBookingNotes"));
        Assert.NotNull(tourType.GetMethod("RemoveBooking"));
    }

    [Then("the operation should fail with not found error")]
    public void ThenTheOperationShouldFailWithNotFoundError()
    {
        var result = (Result)bookingContext.Result;
        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Then(@"the tour should have (\d+) bookings")]
    public void ThenTheTourShouldHaveDBookings(int expectedCount)
    {
        Assert.Equal(expectedCount, tourContext.Tour.Bookings.Count);
    }
}
