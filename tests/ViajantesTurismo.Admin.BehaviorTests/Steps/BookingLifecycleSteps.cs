using Reqnroll;
using ViajantesTurismo.Admin.BehaviorTests.Context;
using ViajantesTurismo.Admin.Domain.Customers;
using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.Admin.BehaviorTests.Steps;

[Binding]
public sealed class BookingLifecycleSteps(BookingContext bookingContext, TourContext tourContext)
{
    [Given("I am authenticated as a tour operator")]
    public static void GivenIAmAuthenticatedAsATourOperator()
    {
    }

    [Given("a pending booking exists")]
    public void GivenAPendingBookingExists()
    {
        tourContext.Tour = EntityBuilders.BuildTour();
        var result = tourContext.Tour.AddBooking(Guid.CreateVersion7(), BikeType.Regular, null, null, RoomType.SingleRoom,
            DiscountType.None, 0m, null, null);
        Assert.True(result.IsSuccess);
        bookingContext.Booking = result.Value;
        Assert.Equal(BookingStatus.Pending, bookingContext.Booking.Status);
    }

    [Given("a confirmed booking exists")]
    public void GivenAConfirmedBookingExists()
    {
        tourContext.Tour = EntityBuilders.BuildTour();
        var result = tourContext.Tour.AddBooking(Guid.CreateVersion7(), BikeType.Regular, null, null, RoomType.SingleRoom,
            DiscountType.None, 0m, null, null);
        Assert.True(result.IsSuccess);
        bookingContext.Booking = result.Value;
        tourContext.Tour.ConfirmBooking(bookingContext.Booking.Id);
        Assert.Equal(BookingStatus.Confirmed, bookingContext.Booking.Status);
    }

    [Given("a cancelled booking exists")]
    public void GivenACancelledBookingExists()
    {
        tourContext.Tour = EntityBuilders.BuildTour();
        var addResult = tourContext.Tour.AddBooking(Guid.CreateVersion7(), BikeType.Regular, null, null, RoomType.SingleRoom,
            DiscountType.None, 0m, null, null);
        Assert.True(addResult.IsSuccess);
        bookingContext.Booking = addResult.Value;
        var result = tourContext.Tour.CancelBooking(bookingContext.Booking.Id);
        Assert.True(result.IsSuccess);
        Assert.Equal(BookingStatus.Cancelled, bookingContext.Booking.Status);
    }

    [Given("a completed booking exists")]
    public void GivenACompletedBookingExists()
    {
        tourContext.Tour = EntityBuilders.BuildTour();
        var addResult = tourContext.Tour.AddBooking(Guid.CreateVersion7(), BikeType.Regular, null, null, RoomType.SingleRoom,
            DiscountType.None, 0m, null, null);
        Assert.True(addResult.IsSuccess);
        bookingContext.Booking = addResult.Value;
        var confirmResult = tourContext.Tour.ConfirmBooking(bookingContext.Booking.Id);
        Assert.True(confirmResult.IsSuccess);
        var completeResult = tourContext.Tour.CompleteBooking(bookingContext.Booking.Id);
        Assert.True(completeResult.IsSuccess);
        Assert.Equal(BookingStatus.Completed, bookingContext.Booking.Status);
    }

    [When("the operator confirms the booking")]
    public void WhenTheOperatorConfirmsTheBooking()
    {
        var result = tourContext.Tour.ConfirmBooking(bookingContext.Booking.Id);
        Assert.True(result.IsSuccess);
    }

    [When("the operator cancels the booking")]
    public void WhenTheOperatorCancelsTheBooking()
    {
        var result = tourContext.Tour.CancelBooking(bookingContext.Booking.Id);
        Assert.True(result.IsSuccess);
    }

    [When("the operator completes the booking")]
    public void WhenTheOperatorCompletesTheBooking()
    {
        var result = tourContext.Tour.CompleteBooking(bookingContext.Booking.Id);
        Assert.True(result.IsSuccess);
    }

    [When("the operator tries to confirm the booking")]
    public void WhenTheOperatorTriesToConfirmTheBooking()
    {
        var result = tourContext.Tour.ConfirmBooking(bookingContext.Booking.Id);
        bookingContext.BookingOperationResult = result;
    }

    [When("the operator tries to cancel the booking")]
    public void WhenTheOperatorTriesToCancelTheBooking()
    {
        var result = tourContext.Tour.CancelBooking(bookingContext.Booking.Id);
        bookingContext.BookingOperationResult = result;
    }

    [When("the operator tries to complete the booking")]
    public void WhenTheOperatorTriesToCompleteTheBooking()
    {
        var result = tourContext.Tour.CompleteBooking(bookingContext.Booking.Id);
        bookingContext.BookingOperationResult = result;
    }

    [When(@"the operator updates the notes to ""(.*)""")]
    public void WhenTheOperatorUpdatesTheNotesTo(string notes)
    {
        var result = tourContext.Tour.UpdateBookingNotes(bookingContext.Booking.Id, notes);
        Assert.True(result.IsSuccess);
    }

    [When("the operator updates the notes to null")]
    public void WhenTheOperatorUpdatesTheNotesToNull()
    {
        var result = tourContext.Tour.UpdateBookingNotes(bookingContext.Booking.Id, null);
        Assert.True(result.IsSuccess);
    }

    [When("the operator tries to update the notes to a string longer than (.*) characters")]
    public void WhenTheOperatorTriesToUpdateTheNotesToAStringLongerThanCharacters(int maxLength)
    {
        var longNotes = new string('A', maxLength + 1);
        var result = tourContext.Tour.UpdateBookingNotes(bookingContext.Booking.Id, longNotes);
        bookingContext.BookingOperationResult = result;
    }

    [When("the operator records a payment for the full amount")]
    public void WhenTheOperatorRecordsAPaymentForTheFullAmount()
    {
        var result = tourContext.Tour.RecordBookingPayment(
            bookingContext.Booking.Id,
            bookingContext.Booking.TotalPrice,
            DateTime.UtcNow,
            PaymentMethod.CreditCard,
            TimeProvider.System);
        Assert.True(result.IsSuccess);
    }

    [When("the operator records a payment for {int} percent of the total")]
    public void WhenTheOperatorRecordsAPaymentForPercentOfTheTotal(int percentage)
    {
        var amount = bookingContext.Booking.TotalPrice * (percentage / 100m);
        var result = tourContext.Tour.RecordBookingPayment(
            bookingContext.Booking.Id,
            amount,
            DateTime.UtcNow,
            PaymentMethod.CreditCard,
            TimeProvider.System);
        Assert.True(result.IsSuccess);
    }

    [When("the operator attempts to remove the booking")]
    public void WhenTheOperatorAttemptsToRemoveTheBooking()
    {
        var result = tourContext.Tour.RemoveBooking(bookingContext.Booking.Id);
        bookingContext.BookingOperationResult = result;
    }

    [When("the operator removes the booking")]
    public void WhenTheOperatorRemovesTheBooking()
    {
        var result = tourContext.Tour.RemoveBooking(bookingContext.Booking.Id);
        Assert.True(result.IsSuccess);
        bookingContext.BookingOperationResult = result;
    }

    [Then("the removal should fail")]
    public void ThenTheRemovalShouldFail()
    {
        Assert.NotNull(bookingContext.BookingOperationResult);
        Assert.False(bookingContext.BookingOperationResult.Value.IsSuccess);
    }

    [Then("the operation should fail")]
    public void ThenTheOperationShouldFail()
    {
        Assert.NotNull(bookingContext.BookingOperationResult);
        Assert.False(bookingContext.BookingOperationResult.Value.IsSuccess);
    }

    [Then("the booking should be removed successfully")]
    public void ThenTheBookingShouldBeRemovedSuccessfully()
    {
        Assert.NotNull(bookingContext.BookingOperationResult);
        Assert.True(bookingContext.BookingOperationResult.Value.IsSuccess);
    }
}
