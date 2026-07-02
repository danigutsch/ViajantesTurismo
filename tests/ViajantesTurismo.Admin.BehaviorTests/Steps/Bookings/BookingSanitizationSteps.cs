using ViajantesTurismo.Admin.Domain.Shared;

namespace ViajantesTurismo.Admin.BehaviorTests.Steps.Bookings;

[Binding]
public sealed class BookingSanitizationSteps(BookingContext bookingContext, TourContext tourContext)
{
    [When(@"I add a booking with notes ""(.*)""")]
    public void WhenIAddABookingWithNotes(string notes)
    {
        var result = tourContext.Tour.AddBooking(new TourBookingRequest(
            Guid.CreateVersion7(),
            BikeType.Regular,
            RoomType.DoubleOccupancy,
            DiscountType.None,
            notes: notes));
        (result.IsSuccess).ShouldBeTrue();
        bookingContext.Booking = result.Value;
    }

    [When("I add a booking with notes exceeding 2000 characters")]
    public void WhenIAddABookingWithNotesExceedingCharacters()
    {
        var longNotes = new string('A', 2001);
        var result = tourContext.Tour.AddBooking(new TourBookingRequest(
            Guid.CreateVersion7(),
            BikeType.Regular,
            RoomType.DoubleOccupancy,
            DiscountType.None,
            notes: longNotes));
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
        var result = tourContext.Tour.AddBooking(new TourBookingRequest(
            Guid.CreateVersion7(),
            BikeType.Regular,
            RoomType.DoubleOccupancy,
            DiscountType.None));
        (result.IsSuccess).ShouldBeTrue();
        bookingContext.Booking = result.Value;
    }

    [When("I update the booking notes to null through the tour")]
    public void WhenIUpdateTheBookingNotesToNullThroughTheTour()
    {
        var result = tourContext.Tour.UpdateBookingNotes(bookingContext.Booking.Id, null);
        (result.IsSuccess).ShouldBeTrue();
    }

    [Then("the booking creation should fail with notes validation error")]
    public void ThenTheBookingCreationShouldFailWithNotesValidationError()
    {
        var result = (bookingContext.BookingOperationResult).ShouldNotBeNull();
        (result.IsSuccess).ShouldBeFalse();
        (result.ErrorDetails?.ValidationErrors?.ContainsKey("notes") ?? false).ShouldBeTrue();
    }
}
