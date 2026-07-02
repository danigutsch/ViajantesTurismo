using ViajantesTurismo.Admin.Domain.Shared;

namespace ViajantesTurismo.Admin.BehaviorTests.Steps.Bookings;

[Binding]
public sealed class BookingValidationSteps(
    BookingContext bookingContext,
    TourContext tourContext,
    CustomerContext customerContext)
{
    private void AssertBookingCreationFailed()
    {
        (bookingContext.BookingCreationResult).ShouldNotBeNull();
        (bookingContext.BookingCreationResult.Value.IsFailure).ShouldBeTrue();
    }

    [When("I try to add a booking to tour with invalid room type (.*)")]
    public void WhenITryToAddABookingToTourWithInvalidRoomType(int invalidRoomType)
    {
        var result = tourContext.Tour.AddBooking(new TourBookingRequest(
            Guid.CreateVersion7(),
            BikeType.Regular,
            (RoomType)invalidRoomType,
            DiscountType.None));
        bookingContext.BookingCreationResult = result;
    }

    [When(@"I try to update the booking notes with (\d+) characters through the tour")]
    public void WhenITryToUpdateTheBookingNotesWithDCharactersThroughTheTour(int characterCount)
    {
        var notes = new string('A', characterCount);
        var result = tourContext.Tour.UpdateBookingNotes(bookingContext.Booking.Id, notes);
        bookingContext.BookingOperationResult = result;
    }

    [When("I try to confirm the booking through the tour")]
    public void WhenITryToConfirmTheBookingThroughTheTour()
    {
        var result = tourContext.Tour.ConfirmBooking(bookingContext.Booking.Id);
        bookingContext.BookingOperationResult = result;
    }

    [When("I try to cancel the booking through the tour")]
    public void WhenITryToCancelTheBookingThroughTheTour()
    {
        var result = tourContext.Tour.CancelBooking(bookingContext.Booking.Id);
        bookingContext.BookingOperationResult = result;
    }

    [When("I try to complete the booking through the tour")]
    public void WhenITryToCompleteTheBookingThroughTheTour()
    {
        var result = tourContext.Tour.CompleteBooking(bookingContext.Booking.Id);
        bookingContext.BookingOperationResult = result;
    }

    [Then("the booking update should fail with validation error")]
    public void ThenTheBookingUpdateShouldFailWithValidationError()
    {
        (bookingContext.BookingOperationResult).ShouldNotBeNull();
        (bookingContext.BookingOperationResult.Value.IsSuccess).ShouldBeFalse();
        (bookingContext.BookingOperationResult.Value.Status).ShouldBe(ResultStatus.Invalid);
    }

    [Then("the booking should be created successfully")]
    public void ThenTheBookingShouldBeCreatedSuccessfully()
    {
        (bookingContext.BookingCreationResult).ShouldNotBeNull();
        (bookingContext.BookingCreationResult.Value.IsSuccess).ShouldBeTrue();
        bookingContext.Booking = bookingContext.BookingCreationResult.Value.Value;
    }

    [Then(@"the error message should contain ""(.*)""")]
    public void ThenTheErrorMessageShouldContain(string expectedMessage)
    {
        ResultError? errorDetails = null;

        // Check typed properties (all paths should lead here with the new code)
        if (bookingContext.BookingCreationResult.HasValue)
        {
            errorDetails = bookingContext.BookingCreationResult.Value.ErrorDetails;
        }
        else if (bookingContext.BookingOperationResult.HasValue)
        {
            errorDetails = bookingContext.BookingOperationResult.Value.ErrorDetails;
        }
        else if (bookingContext.BookingCustomerResult.HasValue)
        {
            errorDetails = bookingContext.BookingCustomerResult.Value.ErrorDetails;
        }
        else if (bookingContext.PaymentResult.HasValue)
        {
            errorDetails = bookingContext.PaymentResult.Value.ErrorDetails;
        }
        else if (tourContext.DeleteResult.HasValue)
        {
            errorDetails = tourContext.DeleteResult.Value.ErrorDetails;
        }
        else if (tourContext.UpdateResult.HasValue)
        {
            errorDetails = tourContext.UpdateResult.Value.ErrorDetails;
        }
        else if (customerContext.CommandResult.HasValue)
        {
            errorDetails = customerContext.CommandResult.Value.ErrorDetails;
        }

        (errorDetails).ShouldNotBeNull();

        var messageFound = errorDetails.Detail.Contains(expectedMessage, StringComparison.Ordinal);
        if (!messageFound && errorDetails.ValidationErrors != null)
        {
            messageFound = errorDetails.ValidationErrors.Values
                .SelectMany(errors => errors)
                .Any(error => error.Contains(expectedMessage, StringComparison.Ordinal));
        }

        (messageFound).ShouldBeTrue($"Expected message '{expectedMessage}' not found in error details.");
    }

    [Then("the booking notes should be updated successfully")]
    public void ThenTheBookingNotesShouldBeUpdatedSuccessfully()
    {
        (bookingContext.BookingOperationResult).ShouldNotBeNull();
        (bookingContext.BookingOperationResult.Value.IsSuccess).ShouldBeTrue();
    }

    [Then("the booking notes should be null or empty")]
    public void ThenTheBookingNotesShouldBeNullOrEmpty()
    {
        (string.IsNullOrWhiteSpace(bookingContext.Booking.Notes)).ShouldBeTrue();
    }

    [Then(@"I should be informed that (.+) cannot exceed (\d+) characters")]
    public void ThenIShouldBeInformedThatFieldCannotExceedCharacters(string fieldName, int maxLength)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fieldName);
        ResultError? errorDetails = null;

        if (bookingContext.BookingCreationResult.HasValue)
        {
            (bookingContext.BookingCreationResult.Value.IsFailure).ShouldBeTrue();
            errorDetails = bookingContext.BookingCreationResult.Value.ErrorDetails;
        }
        else if (bookingContext.BookingOperationResult.HasValue)
        {
            (bookingContext.BookingOperationResult.Value.IsFailure).ShouldBeTrue();
            errorDetails = bookingContext.BookingOperationResult.Value.ErrorDetails;
        }
        else
        {
            false.ShouldBeTrue("Expected either BookingCreationResult or BookingOperationResult to be set");
        }

        var normalizedFieldName = fieldName.Replace(" ", "", StringComparison.Ordinal);
        (errorDetails?.ValidationErrors?.Any(kvp =>
                kvp.Key.Equals(normalizedFieldName, StringComparison.OrdinalIgnoreCase)) ?? false).ShouldBeTrue($"Expected validation error for {normalizedFieldName}");
    }

    [When(@"I attempt to create a booking with base price (-?\d+)")]
    public void WhenIAttemptToCreateABookingWithBasePrice(decimal basePrice)
    {
        bookingContext.BookingCreationResult = BookingStepDataFactory.CreateBookingWithBasePrice(basePrice);
    }

    [When(@"I attempt to create a booking with base price (-?\d+) and room cost (-?\d+)")]
    public void WhenIAttemptToCreateABookingWithBasePriceAndRoomCost(decimal basePrice, decimal roomCost)
    {
        bookingContext.BookingCreationResult = BookingStepDataFactory.CreateBookingWithRoomCost(basePrice, roomCost);
    }

    [When(@"I attempt to create a booking with invalid room type (-?\d+)")]
    public void WhenIAttemptToCreateABookingWithInvalidRoomType(int invalidRoomType)
    {
        bookingContext.BookingCreationResult = BookingStepDataFactory.CreateBookingWithInvalidRoomType(invalidRoomType);
    }

    [When(@"I attempt to create a booking with notes of (\d+) characters")]
    public void WhenIAttemptToCreateABookingWithNotesOfCharacters(int characterCount)
    {
        bookingContext.BookingCreationResult = BookingStepDataFactory.CreateBookingWithNotes(characterCount);
    }

    [Then("I should be informed that the room type is invalid")]
    public void ThenIShouldBeInformedThatTheRoomTypeIsInvalid()
    {
        (bookingContext.BookingCreationResult).ShouldNotBeNull();
        (bookingContext.BookingCreationResult.Value.IsFailure).ShouldBeTrue();
        (bookingContext.BookingCreationResult.Value.ErrorDetails?.Detail ?? string.Empty).ShouldContain("room", StringComparison.OrdinalIgnoreCase);
    }

    [Then("I should be informed that the cost exceeds our maximum rate")]
    public void ThenIShouldBeInformedThatTheCostExceedsOurMaximumRate()
    {
        AssertBookingCreationFailed();
    }

    [Then("I should be informed that the base price must be positive")]
    public void ThenIShouldBeInformedThatTheBasePriceMustBePositive()
    {
        AssertBookingCreationFailed();
    }

    [Then("I should be informed that room costs must be non-negative")]
    public void ThenIShouldBeInformedThatRoomCostsMustBeNonNegative()
    {
        AssertBookingCreationFailed();
    }

    [Then("I should not be able to create the booking")]
    public void ThenIShouldNotBeAbleToCreateTheBooking()
    {
        AssertBookingCreationFailed();
    }
}
