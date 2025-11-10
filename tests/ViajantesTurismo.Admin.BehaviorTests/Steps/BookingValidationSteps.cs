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
        bookingContext.Result = tourContext.Tour.AddBooking(1, BikeType.Regular, null, null, (RoomType)invalidRoomType,
            DiscountType.None, 0m, null, null);
    }

    [When("I try to update the booking payment status with invalid value (.*) through the tour")]
    public void WhenITryToUpdateTheBookingPaymentStatusWithInvalidValueThroughTheTour(int invalidValue)
    {
        bookingContext.Result =
            tourContext.Tour.UpdateBookingPaymentStatus(bookingContext.Booking.Id, (PaymentStatus)invalidValue);
    }

    [When(@"I try to update the booking notes with (\d+) characters through the tour")]
    public void WhenITryToUpdateTheBookingNotesWithDCharactersThroughTheTour(int characterCount)
    {
        var notes = new string('A', characterCount);
        bookingContext.Result = tourContext.Tour.UpdateBookingNotes(bookingContext.Booking.Id, notes);
    }

    [When("I try to confirm the booking through the tour")]
    public void WhenITryToConfirmTheBookingThroughTheTour()
    {
        bookingContext.Result = tourContext.Tour.ConfirmBooking(bookingContext.Booking.Id);
    }

    [When("I try to cancel the booking through the tour")]
    public void WhenITryToCancelTheBookingThroughTheTour()
    {
        bookingContext.Result = tourContext.Tour.CancelBooking(bookingContext.Booking.Id);
    }

    [When("I try to complete the booking through the tour")]
    public void WhenITryToCompleteTheBookingThroughTheTour()
    {
        bookingContext.Result = tourContext.Tour.CompleteBooking(bookingContext.Booking.Id);
    }

    [Then("the booking should be created successfully")]
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

        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (bookingContext.Result != null)
        {
            errorDetails = bookingContext.Result switch
            {
                Result<Booking> typedResult => typedResult.ErrorDetails,
                Result result => result.ErrorDetails,
                _ => null
            };
        }
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
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

    [Then("the booking notes should be updated successfully")]
    public void ThenTheBookingNotesShouldBeUpdatedSuccessfully()
    {
        var result = (Result)bookingContext.Result;
        Assert.True(result.IsSuccess);
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
        switch (bookingContext.Result)
        {
            case Result<Booking> typedResult:
                Assert.True(typedResult.IsFailure);
                errorDetails = typedResult.ErrorDetails;
                break;
            case Result result:
                Assert.True(result.IsFailure);
                errorDetails = result.ErrorDetails;
                break;
        }

        var normalizedFieldName = fieldName.Replace(" ", "", StringComparison.Ordinal);
        Assert.True(errorDetails?.ValidationErrors?.Any(kvp =>
                kvp.Key.Equals(normalizedFieldName, StringComparison.OrdinalIgnoreCase)) ?? false,
            $"Expected validation error for {normalizedFieldName}");
    }

    [When(@"I attempt to create a booking with base price (-?\d+)")]
    public void WhenIAttemptToCreateABookingWithBasePrice(decimal basePrice)
    {
        var principal = BookingCustomer.Create(1, BikeType.Regular, 100m).Value;
        bookingContext.Result = Booking.Create(1, basePrice, RoomType.DoubleRoom, 0m, principal, null,
            Discount.Create(DiscountType.None, 0m, null).Value, null);
    }

    [When(@"I attempt to create a booking with base price (-?\d+) and room cost (-?\d+)")]
    public void WhenIAttemptToCreateABookingWithBasePriceAndRoomCost(decimal basePrice, decimal roomCost)
    {
        var principal = BookingCustomer.Create(1, BikeType.Regular, 100m).Value;
        bookingContext.Result = Booking.Create(1, basePrice, RoomType.SingleRoom, roomCost, principal, null,
            Discount.Create(DiscountType.None, 0m, null).Value, null);
    }

    [When(@"I attempt to create a booking with invalid room type (-?\d+)")]
    public void WhenIAttemptToCreateABookingWithInvalidRoomType(int invalidRoomType)
    {
        var principal = BookingCustomer.Create(1, BikeType.Regular, 100m).Value;
        bookingContext.Result = Booking.Create(1, 2000m, (RoomType)invalidRoomType, 0m, principal, null,
            Discount.Create(DiscountType.None, 0m, null).Value, null);
    }

    [When(@"I attempt to create a booking with notes of (\d+) characters")]
    public void WhenIAttemptToCreateABookingWithNotesOfCharacters(int characterCount)
    {
        var principal = BookingCustomer.Create(1, BikeType.Regular, 100m).Value;
        var notes = new string('x', characterCount);
        bookingContext.Result = Booking.Create(1, 1000m, RoomType.DoubleRoom, 0m, principal, null,
            Discount.Create(DiscountType.None, 0m, null).Value, notes);
    }

    [Then("I should be informed that the room type is invalid")]
    public void ThenIShouldBeInformedThatTheRoomTypeIsInvalid()
    {
        switch (bookingContext.Result)
        {
            case Result<Booking> typedResult:
                Assert.True(typedResult.IsFailure);
                Assert.Contains("room", typedResult.ErrorDetails?.Detail ?? string.Empty,
                    StringComparison.OrdinalIgnoreCase);
                break;
            case Result result:
                Assert.True(result.IsFailure);
                Assert.Contains("room", result.ErrorDetails?.Detail ?? string.Empty,
                    StringComparison.OrdinalIgnoreCase);
                break;
        }
    }

    [Then("I should be informed that the cost exceeds our maximum rate")]
    public void ThenIShouldBeInformedThatTheCostExceedsOurMaximumRate()
    {
        switch (bookingContext.Result)
        {
            case Result<Booking> typedResult:
                Assert.True(typedResult.IsFailure);
                break;
            case Result result:
                Assert.True(result.IsFailure);
                break;
        }
    }

    [Then("I should be informed that the base price must be positive")]
    public void ThenIShouldBeInformedThatTheBasePriceMustBePositive()
    {
        switch (bookingContext.Result)
        {
            case Result<Booking> typedResult:
                Assert.True(typedResult.IsFailure);
                break;
            case Result result:
                Assert.True(result.IsFailure);
                break;
        }
    }

    [Then("I should be informed that room costs must be non-negative")]
    public void ThenIShouldBeInformedThatRoomCostsMustBeNonNegative()
    {
        switch (bookingContext.Result)
        {
            case Result<Booking> typedResult:
                Assert.True(typedResult.IsFailure);
                break;
            case Result result:
                Assert.True(result.IsFailure);
                break;
        }
    }

    [Then("I should not be able to create the booking")]
    public void ThenIShouldNotBeAbleToCreateTheBooking()
    {
        switch (bookingContext.Result)
        {
            case Result<Booking> typedResult:
                Assert.True(typedResult.IsFailure, "Expected booking creation to fail, but it succeeded.");
                break;
            case Result result:
                Assert.True(result.IsFailure, "Expected booking creation to fail, but it succeeded.");
                break;
        }
    }
}
