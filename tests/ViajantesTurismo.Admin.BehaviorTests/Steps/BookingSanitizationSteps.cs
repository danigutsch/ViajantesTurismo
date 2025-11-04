using Reqnroll;
using ViajantesTurismo.Admin.BehaviorTests.Context;
using ViajantesTurismo.Admin.Domain.Customers;
using ViajantesTurismo.Admin.Domain.Tours;
using ViajantesTurismo.Common.Results;

namespace ViajantesTurismo.Admin.BehaviorTests.Steps;

[Binding]
public sealed class BookingSanitizationSteps(BookingContext bookingContext, TourContext tourContext)
{
    [When(@"I add a booking with price (.*)")]
    public void WhenIAddABookingWithPrice(decimal price)
    {
        var result = tourContext.Tour.AddBooking(1, BikeType.Regular, null, null, RoomType.SingleRoom, DiscountType.None, 0m, null, null);
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
        var result = tourContext.Tour.AddBooking(1, BikeType.Regular, null, null, RoomType.SingleRoom, DiscountType.None, 0m, null, notes);
        Assert.True(result.IsSuccess);
        bookingContext.Booking = result.Value;
    }

    [When(@"I add a booking with notes exceeding 2000 characters")]
    public void WhenIAddABookingWithNotesExceedingCharacters()
    {
        var longNotes = new string('A', 2001);
        var result = tourContext.Tour.AddBooking(1, BikeType.Regular, null, null, RoomType.SingleRoom, DiscountType.None, 0m, null, longNotes);
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
        var result = tourContext.Tour.AddBooking(1, BikeType.Regular, null, null, RoomType.SingleRoom, DiscountType.None, 0m, null, null);
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
        Assert.DoesNotContain("  ", bookingContext.Booking.Notes, StringComparison.Ordinal);
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
        Assert.Empty(tourContext.Tour.Bookings);
    }
}
