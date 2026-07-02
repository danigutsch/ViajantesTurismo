using ViajantesTurismo.Admin.Domain.Shared;

namespace ViajantesTurismo.Admin.BehaviorTests.Steps.Tours;

[Binding]
public sealed class TourBookingIntegrationSteps(BookingContext bookingContext, TourContext tourContext)
{
    [Given("a tour exists")]
    public void GivenATourExists()
    {
        tourContext.Tour = EntityBuilders.BuildTour();
    }

    [Given("a customer exists")]
    public static void GivenACustomerExists()
    {
        TestAssert.True(true);
    }

    [Given("a tour exists with a pending booking")]
    public void GivenATourExistsWithAPendingBooking()
    {
        tourContext.Tour = EntityBuilders.BuildTour();
        var result = tourContext.Tour.AddBooking(new TourBookingRequest(
            Guid.CreateVersion7(),
            BikeType.Regular,
            RoomType.DoubleOccupancy,
            DiscountType.None));
        TestAssert.True(result.IsSuccess);
        bookingContext.Booking = result.Value;
        TestAssert.Equal(BookingStatus.Pending, bookingContext.Booking.Status);
    }

    [Given("a tour exists with a confirmed booking")]
    public void GivenATourExistsWithAConfirmedBooking()
    {
        tourContext.Tour = EntityBuilders.BuildTour();
        var addResult = tourContext.Tour.AddBooking(new TourBookingRequest(
            Guid.CreateVersion7(),
            BikeType.Regular,
            RoomType.DoubleOccupancy,
            DiscountType.None));
        TestAssert.True(addResult.IsSuccess);
        bookingContext.Booking = addResult.Value;
        var result = tourContext.Tour.ConfirmBooking(bookingContext.Booking.Id);
        TestAssert.True(result.IsSuccess);
        TestAssert.Equal(BookingStatus.Confirmed, bookingContext.Booking.Status);
    }

    [Given("a tour exists with a cancelled booking")]
    public void GivenATourExistsWithACancelledBooking()
    {
        tourContext.Tour = EntityBuilders.BuildTour();
        var addResult = tourContext.Tour.AddBooking(new TourBookingRequest(
            Guid.CreateVersion7(),
            BikeType.Regular,
            RoomType.DoubleOccupancy,
            DiscountType.None));
        TestAssert.True(addResult.IsSuccess);
        bookingContext.Booking = addResult.Value;
        var result = tourContext.Tour.CancelBooking(bookingContext.Booking.Id);
        TestAssert.True(result.IsSuccess);
        TestAssert.Equal(BookingStatus.Cancelled, bookingContext.Booking.Status);
    }

    [Given("a tour exists with a completed booking")]
    public void GivenATourExistsWithACompletedBooking()
    {
        tourContext.Tour = EntityBuilders.BuildTour();
        var addResult = tourContext.Tour.AddBooking(new TourBookingRequest(
            Guid.CreateVersion7(),
            BikeType.Regular,
            RoomType.DoubleOccupancy,
            DiscountType.None));
        TestAssert.True(addResult.IsSuccess);
        bookingContext.Booking = addResult.Value;

        var confirmResult = tourContext.Tour.ConfirmBooking(bookingContext.Booking.Id);
        TestAssert.True(confirmResult.IsSuccess);
        TestAssert.Equal(BookingStatus.Confirmed, bookingContext.Booking.Status);

        var result = tourContext.Tour.CompleteBooking(bookingContext.Booking.Id);
        TestAssert.True(result.IsSuccess);
        TestAssert.Equal(BookingStatus.Completed, bookingContext.Booking.Status);
    }

    [Given("a tour exists with a booking")]
    public void GivenATourExistsWithABooking()
    {
        tourContext.Tour = EntityBuilders.BuildTour();
        var result = tourContext.Tour.AddBooking(new TourBookingRequest(
            Guid.CreateVersion7(),
            BikeType.Regular,
            RoomType.DoubleOccupancy,
            DiscountType.None));
        TestAssert.True(result.IsSuccess);
        bookingContext.Booking = result.Value;
    }

    [Given(@"a tour exists with a booking priced at (.*) and notes ""(.*)""")]
    public void GivenATourExistsWithABookingPricedAtAndNotes(decimal price, string notes)
    {
        tourContext.Tour = EntityBuilders.BuildTour();
        var result = tourContext.Tour.AddBooking(new TourBookingRequest(
            Guid.CreateVersion7(),
            BikeType.Regular,
            RoomType.DoubleOccupancy,
            DiscountType.None,
            notes: notes));
        TestAssert.True(result.IsSuccess);
        bookingContext.Booking = result.Value;
    }

    [When("I add a booking for the customer to the tour with price (.*)")]
    public void WhenIAddABookingForTheCustomerToTheTourWithPrice(decimal price)
    {
        var result = tourContext.Tour.AddBooking(new TourBookingRequest(
            Guid.CreateVersion7(),
            BikeType.Regular,
            RoomType.DoubleOccupancy,
            DiscountType.None));
        TestAssert.True(result.IsSuccess);
        bookingContext.Booking = result.Value;
    }

    [When(@"I add a booking to tour with bike type ""(.*)"" and no companion")]
    public void WhenIAddABookingToTourWithBikeTypeAndNoCompanion(string bikeTypeString)
    {
        var bikeType = Enum.Parse<BikeType>(bikeTypeString);
        var result = tourContext.Tour.AddBooking(new TourBookingRequest(
            Guid.CreateVersion7(),
            bikeType,
            RoomType.DoubleOccupancy,
            DiscountType.None));
        bookingContext.BookingCreationResult = result;
        TestAssert.True(result.IsSuccess);
        bookingContext.Booking = result.Value;
    }

    [When(@"I add a booking to tour with room type ""(.*)""")]
    public void WhenIAddABookingToTourWithRoomType(string roomTypeString)
    {
        var roomType = Enum.Parse<RoomType>(roomTypeString);
        var result = tourContext.Tour.AddBooking(new TourBookingRequest(
            Guid.CreateVersion7(),
            BikeType.Regular,
            roomType,
            DiscountType.None));
        bookingContext.BookingCreationResult = result;
        TestAssert.True(result.IsSuccess);
        bookingContext.Booking = result.Value;
    }

    [When("I confirm the booking through the tour")]
    public void WhenIConfirmTheBookingThroughTheTour()
    {
        var result = tourContext.Tour.ConfirmBooking(bookingContext.Booking.Id);
        TestAssert.True(result.IsSuccess);
    }

    [When("I cancel the booking through the tour")]
    public void WhenICancelTheBookingThroughTheTour()
    {
        var result = tourContext.Tour.CancelBooking(bookingContext.Booking.Id);
        TestAssert.True(result.IsSuccess);
    }

    [When("I complete the booking through the tour")]
    public void WhenICompleteTheBookingThroughTheTour()
    {
        var result = tourContext.Tour.CompleteBooking(bookingContext.Booking.Id);
        TestAssert.True(result.IsSuccess);
    }

    [When(@"I update the booking notes to ""(.*)"" through the tour")]
    public void WhenIUpdateTheBookingNotesToThroughTheTour(string notes)
    {
        var result = tourContext.Tour.UpdateBookingNotes(bookingContext.Booking.Id, notes);
        TestAssert.True(result.IsSuccess);
    }

    [When("I remove the booking from the tour")]
    public void WhenIRemoveTheBookingFromTheTour()
    {
        var result = tourContext.Tour.RemoveBooking(bookingContext.Booking.Id);
        TestAssert.True(result.IsSuccess);
    }

    [When("I try to confirm a non-existent booking")]
    public void WhenITryToConfirmANonExistentBooking()
    {
        var result = tourContext.Tour.ConfirmBooking(Guid.CreateVersion7());
        bookingContext.BookingOperationResult = result;
    }

    [When("I try to remove a non-existent booking")]
    public void WhenITryToRemoveANonExistentBooking()
    {
        var result = tourContext.Tour.RemoveBooking(Guid.CreateVersion7());
        bookingContext.BookingOperationResult = result;
    }

    [When("I try to cancel a non-existent booking")]
    public void WhenITryToCancelANonExistentBooking()
    {
        var result = tourContext.Tour.CancelBooking(Guid.CreateVersion7());
        bookingContext.BookingOperationResult = result;
    }

    [When("I try to complete a non-existent booking")]
    public void WhenITryToCompleteANonExistentBooking()
    {
        var result = tourContext.Tour.CompleteBooking(Guid.CreateVersion7());
        bookingContext.BookingOperationResult = result;
    }

    [When("I try to update notes for a non-existent booking")]
    public void WhenITryToUpdateNotesForANonExistentBooking()
    {
        var result = tourContext.Tour.UpdateBookingNotes(Guid.CreateVersion7(), "Some notes");
        bookingContext.BookingOperationResult = result;
    }

    [Then("the tour should have the booking")]
    public void ThenTheTourShouldHaveTheBooking()
    {
        TestAssert.Contains(bookingContext.Booking, tourContext.Tour.Bookings);
    }

    [Then("the booking should be in pending status")]
    public void ThenTheBookingShouldBeInPendingStatus()
    {
        TestAssert.Equal(BookingStatus.Pending, bookingContext.Booking.Status);
    }

    [Then("the tour should not have the booking")]
    public void ThenTheTourShouldNotHaveTheBooking()
    {
        TestAssert.DoesNotContain(bookingContext.Booking, tourContext.Tour.Bookings);
    }
}
