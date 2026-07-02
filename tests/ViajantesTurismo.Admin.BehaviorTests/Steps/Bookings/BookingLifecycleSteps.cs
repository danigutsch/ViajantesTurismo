using ViajantesTurismo.Admin.Domain.Shared;

namespace ViajantesTurismo.Admin.BehaviorTests.Steps.Bookings;

[Binding]
public sealed class BookingLifecycleSteps(BookingContext bookingContext, TourContext tourContext)
{
    [Given("I am authenticated as a tour operator")]
    public static void GivenIAmAuthenticatedAsATourOperator()
    {
        TestAssert.True(true);
    }

    [Given("a pending booking exists")]
    public void GivenAPendingBookingExists()
    {
        tourContext.Tour = EntityBuilders.BuildTour();
        var result = tourContext.Tour.AddBooking(new TourBookingRequest(
            Guid.CreateVersion7(),
            BikeType.Regular,
            RoomType.DoubleOccupancy,
            DiscountType.None));
        TestAssert.True(result.IsSuccess);
        bookingContext.Booking = result.Value;
        TestAssert.Equal(BookingStatus.Pending, bookingContext.Booking.Status);
    }

    [Given("a confirmed booking exists")]
    public void GivenAConfirmedBookingExists()
    {
        tourContext.Tour = EntityBuilders.BuildTour();
        var result = tourContext.Tour.AddBooking(new TourBookingRequest(
            Guid.CreateVersion7(),
            BikeType.Regular,
            RoomType.DoubleOccupancy,
            DiscountType.None));
        TestAssert.True(result.IsSuccess);
        bookingContext.Booking = result.Value;
        tourContext.Tour.ConfirmBooking(bookingContext.Booking.Id);
        TestAssert.Equal(BookingStatus.Confirmed, bookingContext.Booking.Status);
    }

    [Given("a cancelled booking exists")]
    public void GivenACancelledBookingExists()
    {
        tourContext.Tour = EntityBuilders.BuildTour();
        var addResult = tourContext.Tour.AddBooking(new TourBookingRequest(
            Guid.CreateVersion7(),
            BikeType.Regular,
            RoomType.DoubleOccupancy,
            DiscountType.None));
        TestAssert.True(addResult.IsSuccess);
        bookingContext.Booking = addResult.Value;
        var result = tourContext.Tour.CancelBooking(bookingContext.Booking.Id);
        TestAssert.True(result.IsSuccess);
        TestAssert.Equal(BookingStatus.Cancelled, bookingContext.Booking.Status);
    }

    [Given("a completed booking exists")]
    public void GivenACompletedBookingExists()
    {
        tourContext.Tour = EntityBuilders.BuildTour();
        var addResult = tourContext.Tour.AddBooking(new TourBookingRequest(
            Guid.CreateVersion7(),
            BikeType.Regular,
            RoomType.DoubleOccupancy,
            DiscountType.None));
        TestAssert.True(addResult.IsSuccess);
        bookingContext.Booking = addResult.Value;
        var confirmResult = tourContext.Tour.ConfirmBooking(bookingContext.Booking.Id);
        TestAssert.True(confirmResult.IsSuccess);
        var completeResult = tourContext.Tour.CompleteBooking(bookingContext.Booking.Id);
        TestAssert.True(completeResult.IsSuccess);
        TestAssert.Equal(BookingStatus.Completed, bookingContext.Booking.Status);
    }

    [When("the operator confirms the booking")]
    public void WhenTheOperatorConfirmsTheBooking()
    {
        var result = tourContext.Tour.ConfirmBooking(bookingContext.Booking.Id);
        TestAssert.True(result.IsSuccess);
    }

    [When("the operator cancels the booking")]
    public void WhenTheOperatorCancelsTheBooking()
    {
        var result = tourContext.Tour.CancelBooking(bookingContext.Booking.Id);
        TestAssert.True(result.IsSuccess);
    }

    [When("the operator completes the booking")]
    public void WhenTheOperatorCompletesTheBooking()
    {
        var result = tourContext.Tour.CompleteBooking(bookingContext.Booking.Id);
        TestAssert.True(result.IsSuccess);
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
        TestAssert.True(result.IsSuccess);
    }

    [When("the operator updates the notes to null")]
    public void WhenTheOperatorUpdatesTheNotesToNull()
    {
        var result = tourContext.Tour.UpdateBookingNotes(bookingContext.Booking.Id, null);
        TestAssert.True(result.IsSuccess);
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
        TestAssert.True(result.IsSuccess);
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
        TestAssert.True(result.IsSuccess);
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
        TestAssert.True(result.IsSuccess);
        bookingContext.BookingOperationResult = result;
    }

    [Then("the removal should fail")]
    public void ThenTheRemovalShouldFail()
    {
        TestAssert.NotNull(bookingContext.BookingOperationResult);
        TestAssert.False(bookingContext.BookingOperationResult.Value.IsSuccess);
    }

    [Then("the operation should fail")]
    public void ThenTheOperationShouldFail()
    {
        ThenTheRemovalShouldFail();
    }

    [Then("the booking should be removed successfully")]
    public void ThenTheBookingShouldBeRemovedSuccessfully()
    {
        TestAssert.NotNull(bookingContext.BookingOperationResult);
        TestAssert.True(bookingContext.BookingOperationResult.Value.IsSuccess);
    }
}
