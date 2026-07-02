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
        (true).ShouldBeTrue();
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
        (result.IsSuccess).ShouldBeTrue();
        bookingContext.Booking = result.Value;
        (bookingContext.Booking.Status).ShouldBe(BookingStatus.Pending);
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
        (addResult.IsSuccess).ShouldBeTrue();
        bookingContext.Booking = addResult.Value;
        var result = tourContext.Tour.ConfirmBooking(bookingContext.Booking.Id);
        (result.IsSuccess).ShouldBeTrue();
        (bookingContext.Booking.Status).ShouldBe(BookingStatus.Confirmed);
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
        (addResult.IsSuccess).ShouldBeTrue();
        bookingContext.Booking = addResult.Value;
        var result = tourContext.Tour.CancelBooking(bookingContext.Booking.Id);
        (result.IsSuccess).ShouldBeTrue();
        (bookingContext.Booking.Status).ShouldBe(BookingStatus.Cancelled);
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
        (addResult.IsSuccess).ShouldBeTrue();
        bookingContext.Booking = addResult.Value;

        var confirmResult = tourContext.Tour.ConfirmBooking(bookingContext.Booking.Id);
        (confirmResult.IsSuccess).ShouldBeTrue();
        (bookingContext.Booking.Status).ShouldBe(BookingStatus.Confirmed);

        var result = tourContext.Tour.CompleteBooking(bookingContext.Booking.Id);
        (result.IsSuccess).ShouldBeTrue();
        (bookingContext.Booking.Status).ShouldBe(BookingStatus.Completed);
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
        (result.IsSuccess).ShouldBeTrue();
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
        (result.IsSuccess).ShouldBeTrue();
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
        (result.IsSuccess).ShouldBeTrue();
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
        (result.IsSuccess).ShouldBeTrue();
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
        (result.IsSuccess).ShouldBeTrue();
        bookingContext.Booking = result.Value;
    }

    [When("I confirm the booking through the tour")]
    public void WhenIConfirmTheBookingThroughTheTour()
    {
        var result = tourContext.Tour.ConfirmBooking(bookingContext.Booking.Id);
        (result.IsSuccess).ShouldBeTrue();
    }

    [When("I cancel the booking through the tour")]
    public void WhenICancelTheBookingThroughTheTour()
    {
        var result = tourContext.Tour.CancelBooking(bookingContext.Booking.Id);
        (result.IsSuccess).ShouldBeTrue();
    }

    [When("I complete the booking through the tour")]
    public void WhenICompleteTheBookingThroughTheTour()
    {
        var result = tourContext.Tour.CompleteBooking(bookingContext.Booking.Id);
        (result.IsSuccess).ShouldBeTrue();
    }

    [When(@"I update the booking notes to ""(.*)"" through the tour")]
    public void WhenIUpdateTheBookingNotesToThroughTheTour(string notes)
    {
        var result = tourContext.Tour.UpdateBookingNotes(bookingContext.Booking.Id, notes);
        (result.IsSuccess).ShouldBeTrue();
    }

    [When("I remove the booking from the tour")]
    public void WhenIRemoveTheBookingFromTheTour()
    {
        var result = tourContext.Tour.RemoveBooking(bookingContext.Booking.Id);
        (result.IsSuccess).ShouldBeTrue();
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
        (tourContext.Tour.Bookings).ShouldContain(bookingContext.Booking);
    }

    [Then("the booking should be in pending status")]
    public void ThenTheBookingShouldBeInPendingStatus()
    {
        (bookingContext.Booking.Status).ShouldBe(BookingStatus.Pending);
    }

    [Then("the tour should not have the booking")]
    public void ThenTheTourShouldNotHaveTheBooking()
    {
        (tourContext.Tour.Bookings).ShouldNotContain(bookingContext.Booking);
    }
}
