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
    CustomerContext customerContext)
{
    [When("I try to add a booking to tour with invalid room type (.*)")]
    public void WhenITryToAddABookingToTourWithInvalidRoomType(int invalidRoomType)
    {
        var result = tourContext.Tour.AddBooking(Guid.CreateVersion7(), BikeType.Regular, null, null, (RoomType)invalidRoomType,
            DiscountType.None, 0m, null, null);
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
        Assert.NotNull(bookingContext.BookingOperationResult);
        Assert.False(bookingContext.BookingOperationResult.Value.IsSuccess);
        Assert.Equal(ResultStatus.Invalid, bookingContext.BookingOperationResult.Value.Status);
    }

    [Then("the booking should be created successfully")]
    public void ThenTheBookingShouldBeCreatedSuccessfully()
    {
        Assert.NotNull(bookingContext.BookingCreationResult);
        Assert.True(bookingContext.BookingCreationResult.Value.IsSuccess);
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

    [Then("the booking notes should be updated successfully")]
    public void ThenTheBookingNotesShouldBeUpdatedSuccessfully()
    {
        Assert.NotNull(bookingContext.BookingOperationResult);
        Assert.True(bookingContext.BookingOperationResult.Value.IsSuccess);
    }

    [Then("the booking notes should be null or empty")]
    public void ThenTheBookingNotesShouldBeNullOrEmpty()
    {
        Assert.True(string.IsNullOrWhiteSpace(bookingContext.Booking.Notes));
    }

    [Then(@"I should be informed that (.+) cannot exceed (\d+) characters")]
    public void ThenIShouldBeInformedThatFieldCannotExceedCharacters(string fieldName, int maxLength)
    {
        ResultError? errorDetails = null;

        if (bookingContext.BookingCreationResult.HasValue)
        {
            Assert.True(bookingContext.BookingCreationResult.Value.IsFailure);
            errorDetails = bookingContext.BookingCreationResult.Value.ErrorDetails;
        }
        else if (bookingContext.BookingOperationResult.HasValue)
        {
            Assert.True(bookingContext.BookingOperationResult.Value.IsFailure);
            errorDetails = bookingContext.BookingOperationResult.Value.ErrorDetails;
        }
        else
        {
            Assert.Fail("Expected either BookingCreationResult or BookingOperationResult to be set");
        }

        var normalizedFieldName = fieldName.Replace(" ", "", StringComparison.Ordinal);
        Assert.True(errorDetails?.ValidationErrors?.Any(kvp =>
                kvp.Key.Equals(normalizedFieldName, StringComparison.OrdinalIgnoreCase)) ?? false,
            $"Expected validation error for {normalizedFieldName}");
    }

    [When(@"I attempt to create a booking with base price (-?\d+)")]
    public void WhenIAttemptToCreateABookingWithBasePrice(decimal basePrice)
    {
        var principal = BookingCustomer.Create(Guid.CreateVersion7(), BikeType.Regular, 100m).Value;
        var result = Booking.Create(Guid.CreateVersion7(), basePrice, RoomType.SingleOccupancy, 0m, principal, null,
            Discount.Create(DiscountType.None, 0m, null).Value, null);
        bookingContext.BookingCreationResult = result;
    }

    [When(@"I attempt to create a booking with base price (-?\d+) and room cost (-?\d+)")]
    public void WhenIAttemptToCreateABookingWithBasePriceAndRoomCost(decimal basePrice, decimal roomCost)
    {
        var principal = BookingCustomer.Create(Guid.CreateVersion7(), BikeType.Regular, 100m).Value;
        var result = Booking.Create(Guid.CreateVersion7(), basePrice, RoomType.DoubleOccupancy, roomCost, principal, null,
            Discount.Create(DiscountType.None, 0m, null).Value, null);
        bookingContext.BookingCreationResult = result;
    }

    [When(@"I attempt to create a booking with invalid room type (-?\d+)")]
    public void WhenIAttemptToCreateABookingWithInvalidRoomType(int invalidRoomType)
    {
        var principal = BookingCustomer.Create(Guid.CreateVersion7(), BikeType.Regular, 100m).Value;
        var result = Booking.Create(Guid.CreateVersion7(), 2000m, (RoomType)invalidRoomType, 0m, principal, null,
            Discount.Create(DiscountType.None, 0m, null).Value, null);
        bookingContext.BookingCreationResult = result;
    }

    [When(@"I attempt to create a booking with notes of (\d+) characters")]
    public void WhenIAttemptToCreateABookingWithNotesOfCharacters(int characterCount)
    {
        var principal = BookingCustomer.Create(Guid.CreateVersion7(), BikeType.Regular, 100m).Value;
        var notes = new string('x', characterCount);
        var result = Booking.Create(Guid.CreateVersion7(), 1000m, RoomType.SingleOccupancy, 0m, principal, null,
            Discount.Create(DiscountType.None, 0m, null).Value, notes);
        bookingContext.BookingCreationResult = result;
    }

    [Then("I should be informed that the room type is invalid")]
    public void ThenIShouldBeInformedThatTheRoomTypeIsInvalid()
    {
        Assert.NotNull(bookingContext.BookingCreationResult);
        Assert.True(bookingContext.BookingCreationResult.Value.IsFailure);
        Assert.Contains("room", bookingContext.BookingCreationResult.Value.ErrorDetails?.Detail ?? string.Empty,
            StringComparison.OrdinalIgnoreCase);
    }

    [Then("I should be informed that the cost exceeds our maximum rate")]
    public void ThenIShouldBeInformedThatTheCostExceedsOurMaximumRate()
    {
        Assert.NotNull(bookingContext.BookingCreationResult);
        Assert.True(bookingContext.BookingCreationResult.Value.IsFailure);
    }

    [Then("I should be informed that the base price must be positive")]
    public void ThenIShouldBeInformedThatTheBasePriceMustBePositive()
    {
        Assert.NotNull(bookingContext.BookingCreationResult);
        Assert.True(bookingContext.BookingCreationResult.Value.IsFailure);
    }

    [Then("I should be informed that room costs must be non-negative")]
    public void ThenIShouldBeInformedThatRoomCostsMustBeNonNegative()
    {
        Assert.NotNull(bookingContext.BookingCreationResult);
        Assert.True(bookingContext.BookingCreationResult.Value.IsFailure);
    }

    [Then("I should not be able to create the booking")]
    public void ThenIShouldNotBeAbleToCreateTheBooking()
    {
        Assert.NotNull(bookingContext.BookingCreationResult);
        Assert.True(bookingContext.BookingCreationResult.Value.IsFailure, "Expected booking creation to fail, but it succeeded.");
    }
}
