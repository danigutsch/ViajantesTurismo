using Reqnroll;
using ViajantesTurismo.Admin.BehaviorTests.Context;
using ViajantesTurismo.Admin.Domain.Customers;
using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.Admin.BehaviorTests.Steps;

[Binding]
public sealed class BookingLifecycleSteps(BookingContext bookingContext, TourContext tourContext)
{
    [Given(@"a pending booking exists")]
    public void GivenAPendingBookingExists()
    {
        tourContext.Tour = TestHelpers.CreateTestTour();
        var result = tourContext.Tour.AddBooking(1, BikeType.Regular, null, null, RoomType.SingleRoom, null);
        Assert.True(result.IsSuccess);
        bookingContext.Booking = result.Value;
        Assert.Equal(BookingStatus.Pending, bookingContext.Booking.Status);
    }

    [Given(@"a pending booking exists with price (.*)")]
    public void GivenAPendingBookingExistsWithPrice(decimal price)
    {
        tourContext.Tour = TestHelpers.CreateTestTour();
        var result = tourContext.Tour.AddBooking(1, BikeType.Regular, null, null, RoomType.SingleRoom, null);
        Assert.True(result.IsSuccess);
        bookingContext.Booking = result.Value;
        Assert.Equal(BookingStatus.Pending, bookingContext.Booking.Status);
    }

    [Given(@"a confirmed booking exists")]
    public void GivenAConfirmedBookingExists()
    {
        tourContext.Tour = TestHelpers.CreateTestTour();
        var result = tourContext.Tour.AddBooking(1, BikeType.Regular, null, null, RoomType.SingleRoom, null);
        Assert.True(result.IsSuccess);
        bookingContext.Booking = result.Value;
        tourContext.Tour.ConfirmBooking(bookingContext.Booking.Id);
        Assert.Equal(BookingStatus.Confirmed, bookingContext.Booking.Status);
    }

    [Given(@"a cancelled booking exists")]
    public void GivenACancelledBookingExists()
    {
        tourContext.Tour = TestHelpers.CreateTestTour();
        var addResult = tourContext.Tour.AddBooking(1, BikeType.Regular, null, null, RoomType.SingleRoom, null);
        Assert.True(addResult.IsSuccess);
        bookingContext.Booking = addResult.Value;
        var result = tourContext.Tour.CancelBooking(bookingContext.Booking.Id);
        Assert.True(result.IsSuccess);
        Assert.Equal(BookingStatus.Cancelled, bookingContext.Booking.Status);
    }

    [Given(@"a completed booking exists")]
    public void GivenACompletedBookingExists()
    {
        tourContext.Tour = TestHelpers.CreateTestTour();
        var addResult = tourContext.Tour.AddBooking(1, BikeType.Regular, null, null, RoomType.SingleRoom, null);
        Assert.True(addResult.IsSuccess);
        bookingContext.Booking = addResult.Value;
        var confirmResult = tourContext.Tour.ConfirmBooking(bookingContext.Booking.Id);
        Assert.True(confirmResult.IsSuccess);
        var completeResult = tourContext.Tour.CompleteBooking(bookingContext.Booking.Id);
        Assert.True(completeResult.IsSuccess);
        Assert.Equal(BookingStatus.Completed, bookingContext.Booking.Status);
    }

    [When(@"the operator confirms the booking")]
    public void WhenTheOperatorConfirmsTheBooking()
    {
        var result = tourContext.Tour.ConfirmBooking(bookingContext.Booking.Id);
        Assert.True(result.IsSuccess);
    }

    [When(@"the operator cancels the booking")]
    public void WhenTheOperatorCancelsTheBooking()
    {
        var result = tourContext.Tour.CancelBooking(bookingContext.Booking.Id);
        Assert.True(result.IsSuccess);
    }

    [When(@"the operator completes the booking")]
    public void WhenTheOperatorCompletesTheBooking()
    {
        var result = tourContext.Tour.CompleteBooking(bookingContext.Booking.Id);
        Assert.True(result.IsSuccess);
    }

    [When(@"the operator tries to confirm the booking")]
    public void WhenTheOperatorTriesToConfirmTheBooking()
    {
        bookingContext.Result = tourContext.Tour.ConfirmBooking(bookingContext.Booking.Id);
    }

    [When(@"the operator tries to cancel the booking")]
    public void WhenTheOperatorTriesToCancelTheBooking()
    {
        bookingContext.Result = tourContext.Tour.CancelBooking(bookingContext.Booking.Id);
    }

    [When(@"the operator tries to complete the booking")]
    public void WhenTheOperatorTriesToCompleteTheBooking()
    {
        bookingContext.Result = tourContext.Tour.CompleteBooking(bookingContext.Booking.Id);
    }

    [When(@"the operator updates the price to (.*)")]
    public void WhenTheOperatorUpdatesThePriceTo(decimal newPrice)
    {
        var result = tourContext.Tour.UpdateBookingPrice(bookingContext.Booking.Id, newPrice);
        Assert.True(result.IsSuccess);
    }

    [When(@"the operator tries to update the price to (.*)")]
    public void WhenTheOperatorTriesToUpdateThePriceTo(decimal newPrice)
    {
        bookingContext.Result = tourContext.Tour.UpdateBookingPrice(bookingContext.Booking.Id, newPrice);
    }

    [When(@"the operator updates the notes to ""(.*)""")]
    public void WhenTheOperatorUpdatesTheNotesTo(string notes)
    {
        var result = tourContext.Tour.UpdateBookingNotes(bookingContext.Booking.Id, notes);
        Assert.True(result.IsSuccess);
    }

    [When(@"the operator updates the notes to null")]
    public void WhenTheOperatorUpdatesTheNotesToNull()
    {
        var result = tourContext.Tour.UpdateBookingNotes(bookingContext.Booking.Id, null);
        Assert.True(result.IsSuccess);
    }

    [When(@"the operator tries to update the notes to a string longer than (.*) characters")]
    public void WhenTheOperatorTriesToUpdateTheNotesToAStringLongerThanCharacters(int maxLength)
    {
        var longNotes = new string('A', maxLength + 1);
        bookingContext.Result = tourContext.Tour.UpdateBookingNotes(bookingContext.Booking.Id, longNotes);
    }

    [When(@"the operator updates the payment status to ""(.*)""")]
    public void WhenTheOperatorUpdatesThePaymentStatusTo(string paymentStatusString)
    {
        var paymentStatus = TestHelpers.ParsePaymentStatus(paymentStatusString);
        var result = tourContext.Tour.UpdateBookingPaymentStatus(bookingContext.Booking.Id, paymentStatus);
        Assert.True(result.IsSuccess);
    }
}