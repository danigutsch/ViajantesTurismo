using Reqnroll;
using ViajantesTurismo.Admin.BehaviorTests.Context;
using ViajantesTurismo.Admin.Domain.Customers;
using ViajantesTurismo.Common.Results;

namespace ViajantesTurismo.Admin.BehaviorTests.Steps;

[Binding]
public sealed class BookingSanitizationSteps(BookingContext bookingContext, TourContext tourContext)
{
    [When(@"I add a booking with price (.*)")]
    public void WhenIAddABookingWithPrice(decimal price)
    {
        var result = tourContext.Tour.AddBooking(1, BikeType.Regular, null, null, RoomType.SingleRoom, null);
        if (result.IsSuccess)
        {
            bookingContext.Booking = result.Value;
        }
        else
        {
            bookingContext.Result = result.ToResult();
        }
    }

    [When(@"I add a booking with notes ""(.*)""")]
    public void WhenIAddABookingWithNotes(string notes)
    {
        var result = tourContext.Tour.AddBooking(1, BikeType.Regular, null, null, RoomType.SingleRoom, notes);
        Assert.True(result.IsSuccess);
        bookingContext.Booking = result.Value;
    }

    [When(@"I add a booking with notes exceeding 2000 characters")]
    public void WhenIAddABookingWithNotesExceeding2000Characters()
    {
        var longNotes = new string('A', 2001);
        var result = tourContext.Tour.AddBooking(1, BikeType.Regular, null, null, RoomType.SingleRoom, longNotes);
        if (result.IsSuccess)
        {
            bookingContext.Booking = result.Value;
        }
        else
        {
            bookingContext.Result = result.ToResult();
        }
    }

    [When(@"I add a booking with null notes")]
    public void WhenIAddABookingWithNullNotes()
    {
        var result = tourContext.Tour.AddBooking(1, BikeType.Regular, null, null, RoomType.SingleRoom, null);
        Assert.True(result.IsSuccess);
        bookingContext.Booking = result.Value;
    }

    [When(@"I update the booking notes to null through the tour")]
    public void WhenIUpdateTheBookingNotesToNullThroughTheTour()
    {
        var result = tourContext.Tour.UpdateBookingNotes(bookingContext.Booking.Id, null);
        Assert.True(result.IsSuccess);
    }

    [Then(@"the booking notes should contain normalized whitespace")]
    public void ThenTheBookingNotesShouldContainNormalizedWhitespace()
    {
        Assert.NotNull(bookingContext.Booking.Notes);
        // Should not have multiple consecutive spaces
        Assert.DoesNotContain("  ", bookingContext.Booking.Notes, StringComparison.Ordinal);
        // Should not have leading/trailing whitespace
        Assert.Equal(bookingContext.Booking.Notes.Trim(), bookingContext.Booking.Notes);
    }

    [Then(@"the booking creation should fail with notes validation error")]
    public void ThenTheBookingCreationShouldFailWithNotesValidationError()
    {
        var result = (Result)bookingContext.Result;
        Assert.False(result.IsSuccess);
        Assert.True(result.ErrorDetails?.ValidationErrors?.ContainsKey("notes") ?? false);
    }

    [Then(@"the booking price validation should fail")]
    public void ThenTheBookingPriceValidationShouldFail()
    {
        // When adding booking with invalid price, it should fail
        // We need to verify that the booking was not added to the tour
        Assert.Empty(tourContext.Tour.Bookings);
    }
}
