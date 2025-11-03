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
    [Given(@"a tour exists with a confirmed booking")]
    public void GivenATourExistsWithAConfirmedBooking()
    {
        tourContext.Tour = TestHelpers.CreateTestTour();
        var addResult = tourContext.Tour.AddBooking(1, BikeType.Regular, null, null, RoomType.SingleRoom, null);
        Assert.True(addResult.IsSuccess);
        bookingContext.Booking = addResult.Value;
        var result = tourContext.Tour.ConfirmBooking(bookingContext.Booking.Id);
        Assert.True(result.IsSuccess);
        Assert.Equal(BookingStatus.Confirmed, bookingContext.Booking.Status);
    }

    [When(@"I try to update the booking notes with (\d+) characters through the tour")]
    public void WhenITryToUpdateTheBookingNotesWithCharactersThroughTheTour(int characterCount)
    {
        var notes = new string('A', characterCount);
        bookingContext.Result = tourContext.Tour.UpdateBookingNotes(bookingContext.Booking.Id, notes);
    }

    [When(@"I try to add a booking to tour with invalid room type (.*)")]
    public void WhenITryToAddABookingToTourWithInvalidRoomType(int invalidRoomType)
    {
        var tour = tourContext.Tour;

        var result = tour.AddBooking(
            1,
            BikeType.Regular,
            null,
            null,
            (RoomType)invalidRoomType,
            null);

        bookingContext.Result = result;
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