using Reqnroll;
using ViajantesTurismo.Admin.BehaviorTests.Context;
using ViajantesTurismo.Admin.Domain.Customers;
using ViajantesTurismo.Admin.Domain.Tours;
using ViajantesTurismo.Common.Results;

namespace ViajantesTurismo.Admin.BehaviorTests.Steps;

[Binding]
public sealed class TourBookingIntegrationSteps(BookingContext bookingContext, TourContext tourContext)
{
    [Given("a tour exists")]
    public void GivenATourExists()
    {
        tourContext.Tour = TestHelpers.CreateTestTour();
    }

    [Given("a customer exists")]
    public static void GivenACustomerExists()
    {
    }

    [Given("a tour exists with a pending booking")]
    public void GivenATourExistsWithAPendingBooking()
    {
        tourContext.Tour = TestHelpers.CreateTestTour();
        var result = tourContext.Tour.AddBooking(Guid.CreateVersion7(), BikeType.Regular, null, null, RoomType.SingleRoom,
            DiscountType.None, 0m, null, null);
        Assert.True(result.IsSuccess);
        bookingContext.Booking = result.Value;
        Assert.Equal(BookingStatus.Pending, bookingContext.Booking.Status);
    }

    [Given("a tour exists with a confirmed booking")]
    public void GivenATourExistsWithAConfirmedBooking()
    {
        tourContext.Tour = TestHelpers.CreateTestTour();
        var addResult = tourContext.Tour.AddBooking(Guid.CreateVersion7(), BikeType.Regular, null, null, RoomType.SingleRoom,
            DiscountType.None, 0m, null, null);
        Assert.True(addResult.IsSuccess);
        bookingContext.Booking = addResult.Value;
        var result = tourContext.Tour.ConfirmBooking(bookingContext.Booking.Id);
        Assert.True(result.IsSuccess);
        Assert.Equal(BookingStatus.Confirmed, bookingContext.Booking.Status);
    }

    [Given("a tour exists with a cancelled booking")]
    public void GivenATourExistsWithACancelledBooking()
    {
        tourContext.Tour = TestHelpers.CreateTestTour();
        var addResult = tourContext.Tour.AddBooking(Guid.CreateVersion7(), BikeType.Regular, null, null, RoomType.SingleRoom,
            DiscountType.None, 0m, null, null);
        Assert.True(addResult.IsSuccess);
        bookingContext.Booking = addResult.Value;
        var result = tourContext.Tour.CancelBooking(bookingContext.Booking.Id);
        Assert.True(result.IsSuccess);
        Assert.Equal(BookingStatus.Cancelled, bookingContext.Booking.Status);
    }

    [Given("a tour exists with a completed booking")]
    public void GivenATourExistsWithACompletedBooking()
    {
        tourContext.Tour = TestHelpers.CreateTestTour();
        var addResult = tourContext.Tour.AddBooking(Guid.CreateVersion7(), BikeType.Regular, null, null, RoomType.SingleRoom,
            DiscountType.None, 0m, null, null);
        Assert.True(addResult.IsSuccess);
        bookingContext.Booking = addResult.Value;

        var confirmResult = tourContext.Tour.ConfirmBooking(bookingContext.Booking.Id);
        Assert.True(confirmResult.IsSuccess);
        Assert.Equal(BookingStatus.Confirmed, bookingContext.Booking.Status);

        var result = tourContext.Tour.CompleteBooking(bookingContext.Booking.Id);
        Assert.True(result.IsSuccess);
        Assert.Equal(BookingStatus.Completed, bookingContext.Booking.Status);
    }

    [Given("a tour exists with a booking")]
    public void GivenATourExistsWithABooking()
    {
        tourContext.Tour = TestHelpers.CreateTestTour();
        var result = tourContext.Tour.AddBooking(Guid.CreateVersion7(), BikeType.Regular, null, null, RoomType.SingleRoom,
            DiscountType.None, 0m, null, null);
        Assert.True(result.IsSuccess);
        bookingContext.Booking = result.Value;
    }

    [Given(@"a tour exists with a booking priced at (.*) and notes ""(.*)""")]
    public void GivenATourExistsWithABookingPricedAtAndNotes(decimal price, string notes)
    {
        tourContext.Tour = TestHelpers.CreateTestTour();
        var result = tourContext.Tour.AddBooking(Guid.CreateVersion7(), BikeType.Regular, null, null, RoomType.SingleRoom,
            DiscountType.None, 0m, null, notes);
        Assert.True(result.IsSuccess);
        bookingContext.Booking = result.Value;
    }

    [When("I add a booking for the customer to the tour with price (.*)")]
    public void WhenIAddABookingForTheCustomerToTheTourWithPrice(decimal price)
    {
        var result = tourContext.Tour.AddBooking(Guid.CreateVersion7(), BikeType.Regular, null, null, RoomType.SingleRoom,
            DiscountType.None, 0m, null, null);
        Assert.True(result.IsSuccess);
        bookingContext.Booking = result.Value;
    }

    [When(@"I add a booking to tour with bike type ""(.*)"" and no companion")]
    public void WhenIAddABookingToTourWithBikeTypeAndNoCompanion(string bikeTypeString)
    {
        var bikeType = Enum.Parse<BikeType>(bikeTypeString);
        var result = tourContext.Tour.AddBooking(Guid.CreateVersion7(), bikeType, null, null, RoomType.SingleRoom, DiscountType.None, 0m,
            null, null);
        Assert.True(result.IsSuccess);
        bookingContext.Booking = result.Value;
    }

    [When(@"I add a booking to tour with room type ""(.*)""")]
    public void WhenIAddABookingToTourWithRoomType(string roomTypeString)
    {
        var roomType = Enum.Parse<RoomType>(roomTypeString);
        var result = tourContext.Tour.AddBooking(Guid.CreateVersion7(), BikeType.Regular, null, null, roomType, DiscountType.None, 0m, null,
            null);
        Assert.True(result.IsSuccess);
        bookingContext.Booking = result.Value;
    }

    [When("I confirm the booking through the tour")]
    public void WhenIConfirmTheBookingThroughTheTour()
    {
        var result = tourContext.Tour.ConfirmBooking(bookingContext.Booking.Id);
        Assert.True(result.IsSuccess);
    }

    [When("I cancel the booking through the tour")]
    public void WhenICancelTheBookingThroughTheTour()
    {
        var result = tourContext.Tour.CancelBooking(bookingContext.Booking.Id);
        Assert.True(result.IsSuccess);
    }

    [When("I complete the booking through the tour")]
    public void WhenICompleteTheBookingThroughTheTour()
    {
        var result = tourContext.Tour.CompleteBooking(bookingContext.Booking.Id);
        Assert.True(result.IsSuccess);
    }

    [When(@"I update the booking notes to ""(.*)"" through the tour")]
    public void WhenIUpdateTheBookingNotesToThroughTheTour(string notes)
    {
        var result = tourContext.Tour.UpdateBookingNotes(bookingContext.Booking.Id, notes);
        Assert.True(result.IsSuccess);
    }

    [When("I remove the booking from the tour")]
    public void WhenIRemoveTheBookingFromTheTour()
    {
        var result = tourContext.Tour.RemoveBooking(bookingContext.Booking.Id);
        Assert.True(result.IsSuccess);
    }

    [When("I try to confirm a non-existent booking")]
    public void WhenITryToConfirmANonExistentBooking()
    {
        bookingContext.Result = tourContext.Tour.ConfirmBooking(Guid.CreateVersion7());
    }

    [When("I try to remove a non-existent booking")]
    public void WhenITryToRemoveANonExistentBooking()
    {
        bookingContext.Result = tourContext.Tour.RemoveBooking(Guid.CreateVersion7());
    }

    [When("I try to cancel a non-existent booking")]
    public void WhenITryToCancelANonExistentBooking()
    {
        bookingContext.Result = tourContext.Tour.CancelBooking(Guid.CreateVersion7());
    }

    [When("I try to complete a non-existent booking")]
    public void WhenITryToCompleteANonExistentBooking()
    {
        bookingContext.Result = tourContext.Tour.CompleteBooking(Guid.CreateVersion7());
    }

    [When("I try to update notes for a non-existent booking")]
    public void WhenITryToUpdateNotesForANonExistentBooking()
    {
        bookingContext.Result = tourContext.Tour.UpdateBookingNotes(Guid.CreateVersion7(), "Some notes");
    }

    [Then("the tour should have the booking")]
    public void ThenTheTourShouldHaveTheBooking()
    {
        Assert.Contains(bookingContext.Booking, tourContext.Tour.Bookings);
    }

    [Then("the booking should be in pending status")]
    public void ThenTheBookingShouldBeInPendingStatus()
    {
        Assert.Equal(BookingStatus.Pending, bookingContext.Booking.Status);
    }

    [Then("the tour should not have the booking")]
    public void ThenTheTourShouldNotHaveTheBooking()
    {
        Assert.DoesNotContain(bookingContext.Booking, tourContext.Tour.Bookings);
    }
}
