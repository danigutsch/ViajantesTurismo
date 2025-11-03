using Reqnroll;
using ViajantesTurismo.Admin.BehaviorTests.Context;
using ViajantesTurismo.Admin.Domain.Bookings;
using ViajantesTurismo.Admin.Domain.Customers;
using ViajantesTurismo.Common.Results;

namespace ViajantesTurismo.Admin.BehaviorTests.Steps;

[Binding]
public sealed class BookingValidationSteps(BookingContext bookingContext, TourContext tourContext)
{
    [Given(@"a tour exists with a cancelled booking")]
    public void GivenATourExistsWithACancelledBooking()
    {
        tourContext.Tour = TestHelpers.CreateTestTour();
        var addResult = tourContext.Tour.AddBooking(1, BikeType.Regular, null, null, RoomType.SingleRoom, null);
        Assert.True(addResult.IsSuccess);
        bookingContext.Booking = addResult.Value;
        var result = tourContext.Tour.CancelBooking(bookingContext.Booking.Id);
        Assert.True(result.IsSuccess);
        Assert.Equal(BookingStatus.Cancelled, bookingContext.Booking.Status);
    }

    [Given(@"a tour exists with a completed booking")]
    public void GivenATourExistsWithACompletedBooking()
    {
        tourContext.Tour = TestHelpers.CreateTestTour();
        var addResult = tourContext.Tour.AddBooking(1, BikeType.Regular, null, null, RoomType.SingleRoom, null);
        Assert.True(addResult.IsSuccess);
        bookingContext.Booking = addResult.Value;
        var result = tourContext.Tour.CompleteBooking(bookingContext.Booking.Id);
        Assert.True(result.IsSuccess);
        Assert.Equal(BookingStatus.Completed, bookingContext.Booking.Status);
    }

    [When(@"I try to add a booking with price (.*)")]
    public void WhenITryToAddABookingWithPrice(decimal price)
    {
        var addResult = tourContext.Tour.AddBooking(1, BikeType.Regular, null, null, RoomType.SingleRoom, null);
        Assert.True(addResult.IsSuccess);
        bookingContext.Booking = addResult.Value;
        bookingContext.Result = tourContext.Tour.UpdateBookingPrice(bookingContext.Booking.Id, price);
    }

    [When(@"I try to update the booking price to (.*) through the tour")]
    public void WhenITryToUpdateTheBookingPriceToThroughTheTour(decimal newPrice)
    {
        bookingContext.Result = tourContext.Tour.UpdateBookingPrice(bookingContext.Booking.Id, newPrice);
    }

    [When(@"I try to update the booking notes with (\d+) characters through the tour")]
    public void WhenITryToUpdateTheBookingNotesWithCharactersThroughTheTour(int characterCount)
    {
        var notes = new string('A', characterCount);
        bookingContext.Result = tourContext.Tour.UpdateBookingNotes(bookingContext.Booking.Id, notes);
    }

    [When(@"I try to confirm the booking through the tour")]
    public void WhenITryToConfirmTheBookingThroughTheTour()
    {
        bookingContext.Result = tourContext.Tour.ConfirmBooking(bookingContext.Booking.Id);
    }

    [When(@"I try to cancel the booking through the tour")]
    public void WhenITryToCancelTheBookingThroughTheTour()
    {
        bookingContext.Result = tourContext.Tour.CancelBooking(bookingContext.Booking.Id);
    }

    [When(@"I try to complete the booking through the tour")]
    public void WhenITryToCompleteTheBookingThroughTheTour()
    {
        bookingContext.Result = tourContext.Tour.CompleteBooking(bookingContext.Booking.Id);
    }

    [When(@"I update the payment status to ""(.*)"" through the tour")]
    public void WhenIUpdateThePaymentStatusToThroughTheTour(string paymentStatusString)
    {
        var paymentStatus = TestHelpers.ParsePaymentStatus(paymentStatusString);
        bookingContext.Result = tourContext.Tour.UpdateBookingPaymentStatus(bookingContext.Booking.Id, paymentStatus);
        var result = (Result)bookingContext.Result;
        Assert.True(result.IsSuccess);
    }

    [Then(@"the booking creation should fail with validation error for ""(.*)""")]
    public void ThenTheBookingCreationShouldFailWithValidationErrorFor(string fieldName)
    {
        var result = (Result)bookingContext.Result;
        Assert.False(result.IsSuccess);
        Assert.True(result.ErrorDetails?.ValidationErrors?.ContainsKey(fieldName) ?? false);
    }

    [Then(@"the booking should be created successfully")]
    public void ThenTheBookingShouldBeCreatedSuccessfully()
    {
        switch (bookingContext.Result)
        {
            case Result<Booking> typedResult:
                Assert.True(typedResult.IsSuccess);
                bookingContext.Booking = typedResult.Value;
                break;
            case Result result:
                Assert.True(result.IsSuccess);
                Assert.NotNull(bookingContext.Booking);
                break;
        }
    }

    [Then(@"the booking update should fail with validation error for ""(.*)""")]
    public void ThenTheBookingUpdateShouldFailWithValidationErrorFor(string fieldName)
    {
        var result = (Result)bookingContext.Result;
        Assert.False(result.IsSuccess);
        Assert.True(result.ErrorDetails?.ValidationErrors?.ContainsKey(fieldName) ?? false);
    }

    [Then(@"the booking update should fail with conflict error")]
    public void ThenTheBookingUpdateShouldFailWithConflictError()
    {
        var result = (Result)bookingContext.Result;
        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Conflict, result.Status);
    }

    [Then(@"the error message should contain ""(.*)""")]
    public void ThenTheErrorMessageShouldContain(string expectedMessage)
    {
        var result = (Result)bookingContext.Result;
        Assert.NotNull(result.ErrorDetails);

        var messageFound = result.ErrorDetails.Detail.Contains(expectedMessage, StringComparison.Ordinal);
        if (!messageFound && result.ErrorDetails.ValidationErrors != null)
        {
            messageFound = result.ErrorDetails.ValidationErrors.Values
                .SelectMany(errors => errors)
                .Any(error => error.Contains(expectedMessage, StringComparison.Ordinal));
        }

        Assert.True(messageFound, $"Expected message '{expectedMessage}' not found in error details.");
    }

    [Then(@"the booking notes should be updated successfully")]
    public void ThenTheBookingNotesShouldBeUpdatedSuccessfully()
    {
        var result = (Result)bookingContext.Result;
        Assert.True(result.IsSuccess);
    }

    [Then(@"the booking notes should be null or empty")]
    public void ThenTheBookingNotesShouldBeNullOrEmpty()
    {
        Assert.True(string.IsNullOrWhiteSpace(bookingContext.Booking.Notes));
    }
}
