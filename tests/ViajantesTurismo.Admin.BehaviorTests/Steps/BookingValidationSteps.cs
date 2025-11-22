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
    [When("I try to add a booking to tour with invalid room type (.*)")]
    public void WhenITryToAddABookingToTourWithInvalidRoomType(int invalidRoomType)
    {
        bookingContext.BookingCreationResult = tourContext.Tour.AddBooking(Guid.CreateVersion7(), BikeType.Regular, null, null, (RoomType)invalidRoomType,
            DiscountType.None, 0m, null, null);
    }

    [When(@"I try to update the booking notes with (\d+) characters through the tour")]
    public void WhenITryToUpdateTheBookingNotesWithDCharactersThroughTheTour(int characterCount)
    {
        var notes = new string('A', characterCount);
        bookingContext.BookingUpdateResult = tourContext.Tour.UpdateBookingNotes(bookingContext.Booking.Id, notes);
    }

    [When("I try to confirm the booking through the tour")]
    public void WhenITryToConfirmTheBookingThroughTheTour()
    {
        bookingContext.BookingUpdateResult = tourContext.Tour.ConfirmBooking(bookingContext.Booking.Id);
    }

    [When("I try to cancel the booking through the tour")]
    public void WhenITryToCancelTheBookingThroughTheTour()
    {
        bookingContext.BookingUpdateResult = tourContext.Tour.CancelBooking(bookingContext.Booking.Id);
    }

    [When("I try to complete the booking through the tour")]
    public void WhenITryToCompleteTheBookingThroughTheTour()
    {
        bookingContext.BookingUpdateResult = tourContext.Tour.CompleteBooking(bookingContext.Booking.Id);
    }

    [Then("the booking update should fail with validation error")]
    public void ThenTheBookingUpdateShouldFailWithValidationError()
    {
        Assert.NotNull(bookingContext.BookingUpdateResult);
        Assert.False(bookingContext.BookingUpdateResult.Value.IsSuccess);
        Assert.Equal(ResultStatus.Invalid, bookingContext.BookingUpdateResult.Value.Status);
    }

    [Then("the booking should be created successfully")]
    public void ThenTheBookingShouldBeCreatedSuccessfully()
    {
        Assert.NotNull(bookingContext.BookingCreationResult);
        Assert.True(bookingContext.BookingCreationResult.Value.IsSuccess);
        bookingContext.Booking = bookingContext.BookingCreationResult.Value.Value;
    }

    [Then(@"the booking error message should contain ""(.*)""")]
    public void ThenTheBookingErrorMessageShouldContain(string expectedMessage)
    {
        Assert.NotNull(bookingContext.BookingCreationResult);
        var result = bookingContext.BookingCreationResult.Value;
        Assert.True(result.IsFailure);
        Assert.NotNull(result.ErrorDetails);

        var messageFound = result.ErrorDetails.Detail?.Contains(expectedMessage, StringComparison.Ordinal) ?? false;
        if (!messageFound && result.ErrorDetails.ValidationErrors != null)
        {
            messageFound = result.ErrorDetails.ValidationErrors.Values
                .SelectMany(errors => errors)
                .Any(error => error.Contains(expectedMessage, StringComparison.Ordinal));
        }

        Assert.True(messageFound, $"Expected message '{expectedMessage}' not found in error details.");
    }

    [Then("the booking notes should be updated successfully")]
    public void ThenTheBookingNotesShouldBeUpdatedSuccessfully()
    {
        Assert.NotNull(bookingContext.BookingUpdateResult);
        Assert.True(bookingContext.BookingUpdateResult.Value.IsSuccess);
    }

    [Then("the booking notes should be null or empty")]
    public void ThenTheBookingNotesShouldBeNullOrEmpty()
    {
        Assert.True(string.IsNullOrWhiteSpace(bookingContext.Booking.Notes));
    }

    [Then(@"I should be informed that (.+) cannot exceed (\d+) characters")]
    public void ThenIShouldBeInformedThatFieldCannotExceedCharacters(string fieldName, int maxLength)
    {
        Assert.NotNull(bookingContext.BookingCreationResult);
        var result = bookingContext.BookingCreationResult.Value;
        Assert.True(result.IsFailure);

        var errorDetails = result.ErrorDetails;
        var normalizedFieldName = fieldName.Replace(" ", "", StringComparison.Ordinal);
        Assert.True(errorDetails?.ValidationErrors?.Any(kvp =>
                kvp.Key.Equals(normalizedFieldName, StringComparison.OrdinalIgnoreCase)) ?? false,
            $"Expected validation error for {normalizedFieldName}");
    }

    [When(@"I attempt to create a booking with base price (-?\d+)")]
    public void WhenIAttemptToCreateABookingWithBasePrice(decimal basePrice)
    {
        var principal = BookingCustomer.Create(Guid.CreateVersion7(), BikeType.Regular, 100m).Value;
        bookingContext.BookingCreationResult = Booking.Create(Guid.CreateVersion7(), basePrice, RoomType.DoubleRoom, 0m, principal, null,
            Discount.Create(DiscountType.None, 0m, null).Value, null);
    }

    [When(@"I attempt to create a booking with base price (-?\d+) and room cost (-?\d+)")]
    public void WhenIAttemptToCreateABookingWithBasePriceAndRoomCost(decimal basePrice, decimal roomCost)
    {
        var principal = BookingCustomer.Create(Guid.CreateVersion7(), BikeType.Regular, 100m).Value;
        bookingContext.BookingCreationResult = Booking.Create(Guid.CreateVersion7(), basePrice, RoomType.SingleRoom, roomCost, principal, null,
            Discount.Create(DiscountType.None, 0m, null).Value, null);
    }

    [When(@"I attempt to create a booking with invalid room type (-?\d+)")]
    public void WhenIAttemptToCreateABookingWithInvalidRoomType(int invalidRoomType)
    {
        var principal = BookingCustomer.Create(Guid.CreateVersion7(), BikeType.Regular, 100m).Value;
        bookingContext.BookingCreationResult = Booking.Create(Guid.CreateVersion7(), 2000m, (RoomType)invalidRoomType, 0m, principal, null,
            Discount.Create(DiscountType.None, 0m, null).Value, null);
    }

    [When(@"I attempt to create a booking with notes of (\d+) characters")]
    public void WhenIAttemptToCreateABookingWithNotesOfCharacters(int characterCount)
    {
        var principal = BookingCustomer.Create(Guid.CreateVersion7(), BikeType.Regular, 100m).Value;
        var notes = new string('x', characterCount);
        bookingContext.BookingCreationResult = Booking.Create(Guid.CreateVersion7(), 1000m, RoomType.DoubleRoom, 0m, principal, null,
            Discount.Create(DiscountType.None, 0m, null).Value, notes);
    }

    [Then("I should be informed that the room type is invalid")]
    public void ThenIShouldBeInformedThatTheRoomTypeIsInvalid()
    {
        Assert.NotNull(bookingContext.BookingCreationResult);
        var result = bookingContext.BookingCreationResult.Value;
        Assert.True(result.IsFailure);
        Assert.Contains("room", result.ErrorDetails?.Detail ?? string.Empty,
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
