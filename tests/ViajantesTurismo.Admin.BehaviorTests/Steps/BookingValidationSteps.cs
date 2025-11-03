using Reqnroll;
using ViajantesTurismo.Admin.BehaviorTests.Context;
using ViajantesTurismo.Admin.Domain.Customers;
using ViajantesTurismo.Admin.Domain.Tours;
using ViajantesTurismo.Common.Results;

namespace ViajantesTurismo.Admin.BehaviorTests.Steps;

[Binding]
public sealed class BookingValidationSteps(
    BookingContext bookingContext,
    TourContext tourContext,
    BookingCustomerContext bookingCustomerContext)
{
    [When(@"I try to add a booking to tour with invalid room type (.*)")]
    public void WhenITryToAddABookingToTourWithInvalidRoomType(int invalidRoomType)
    {
        bookingContext.Result = tourContext.Tour.AddBooking(1, BikeType.Regular, null, null, (RoomType)invalidRoomType, null);
    }

    [When(@"I try to update the booking payment status with invalid value (.*) through the tour")]
    public void WhenITryToUpdateTheBookingPaymentStatusWithInvalidValueThroughTheTour(int invalidValue)
    {
        bookingContext.Result = tourContext.Tour.UpdateBookingPaymentStatus(bookingContext.Booking.Id, (PaymentStatus)invalidValue);
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

    [Then(@"the error message should contain ""(.*)""")]
    public void ThenTheErrorMessageShouldContain(string expectedMessage)
    {
        // Try to get error details from any context that has a result
        ResultError? errorDetails = null;

        if (bookingContext.Result != null)
        {
            errorDetails = bookingContext.Result switch
            {
                Result<Booking> typedResult => typedResult.ErrorDetails,
                Result result => result.ErrorDetails,
                _ => null
            };
        }
        else if (bookingCustomerContext.Result != null)
        {
            var result = (Result<BookingCustomer>)bookingCustomerContext.Result;
            errorDetails = result.ErrorDetails;
        }

        Assert.NotNull(errorDetails);

        var messageFound = errorDetails.Detail.Contains(expectedMessage, StringComparison.Ordinal);
        if (!messageFound && errorDetails.ValidationErrors != null)
        {
            messageFound = errorDetails.ValidationErrors.Values
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
