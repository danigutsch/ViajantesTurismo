using Reqnroll;
using ViajantesTurismo.Admin.BehaviorTests.Context;
using ViajantesTurismo.Admin.Domain.Customers;
using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.Admin.BehaviorTests.Steps.Bookings;

[Binding]
public sealed class BookingSanitizationSteps(BookingContext bookingContext, TourContext tourContext)
{
    [When(@"I add a booking with notes ""(.*)""")]
    public void WhenIAddABookingWithNotes(string notes)
    {
        var result = tourContext.Tour.AddBooking(Guid.CreateVersion7(), BikeType.Regular, null, null, RoomType.DoubleOccupancy,
            DiscountType.None, 0m, null, notes);
        Assert.True(result.IsSuccess);
        bookingContext.Booking = result.Value;
    }

    [When("I add a booking with notes exceeding 2000 characters")]
    public void WhenIAddABookingWithNotesExceedingCharacters()
    {
        var longNotes = new string('A', 2001);
        var result = tourContext.Tour.AddBooking(Guid.CreateVersion7(), BikeType.Regular, null, null, RoomType.DoubleOccupancy,
            DiscountType.None, 0m, null, longNotes);
        if (result.IsSuccess)
        {
            bookingContext.Booking = result.Value;
        }
        else
        {
            bookingContext.BookingOperationResult = result.ToResult();
        }
    }

    [When("I add a booking with null notes")]
    public void WhenIAddABookingWithNullNotes()
    {
        var result = tourContext.Tour.AddBooking(Guid.CreateVersion7(), BikeType.Regular, null, null, RoomType.DoubleOccupancy,
            DiscountType.None, 0m, null, null);
        Assert.True(result.IsSuccess);
        bookingContext.Booking = result.Value;
    }

    [When("I update the booking notes to null through the tour")]
    public void WhenIUpdateTheBookingNotesToNullThroughTheTour()
    {
        var result = tourContext.Tour.UpdateBookingNotes(bookingContext.Booking.Id, null);
        Assert.True(result.IsSuccess);
    }

    [Then("the booking creation should fail with notes validation error")]
    public void ThenTheBookingCreationShouldFailWithNotesValidationError()
    {
        Assert.NotNull(bookingContext.BookingOperationResult);
        var result = bookingContext.BookingOperationResult.Value;
        Assert.False(result.IsSuccess);
        Assert.True(result.ErrorDetails?.ValidationErrors?.ContainsKey("notes") ?? false);
    }
}
