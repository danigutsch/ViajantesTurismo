using Reqnroll;
using ViajantesTurismo.Admin.BehaviorTests.Context;
using ViajantesTurismo.Admin.Domain.Customers;
using ViajantesTurismo.Admin.Domain.Tours;
using ViajantesTurismo.Common.Results;

namespace ViajantesTurismo.Admin.BehaviorTests.Steps;

[Binding]
public sealed class TourBookingIntegrationSteps(BookingContext bookingContext, TourContext tourContext)
{
    [Given(@"a tour exists")]
    public void GivenATourExists()
    {
        tourContext.Tour = TestHelpers.CreateTestTour();
    }

    [Given(@"a customer exists")]
    public static void GivenACustomerExists()
    {
    }

    [Given(@"a tour exists with a pending booking")]
    public void GivenATourExistsWithAPendingBooking()
    {
        tourContext.Tour = TestHelpers.CreateTestTour();
        var result = tourContext.Tour.AddBooking(1, BikeType.Regular, null, null, RoomType.SingleRoom, DiscountType.None, 0m, null, null);
        Assert.True(result.IsSuccess);
        bookingContext.Booking = result.Value;
        Assert.Equal(BookingStatus.Pending, bookingContext.Booking.Status);
    }

    [Given(@"a tour exists with a confirmed booking")]
    public void GivenATourExistsWithAConfirmedBooking()
    {
        tourContext.Tour = TestHelpers.CreateTestTour();
        var addResult = tourContext.Tour.AddBooking(1, BikeType.Regular, null, null, RoomType.SingleRoom, DiscountType.None, 0m, null, null);
        Assert.True(addResult.IsSuccess);
        bookingContext.Booking = addResult.Value;
        var result = tourContext.Tour.ConfirmBooking(bookingContext.Booking.Id);
        Assert.True(result.IsSuccess);
        Assert.Equal(BookingStatus.Confirmed, bookingContext.Booking.Status);
    }

    [Given(@"a tour exists with a cancelled booking")]
    public void GivenATourExistsWithACancelledBooking()
    {
        tourContext.Tour = TestHelpers.CreateTestTour();
        var addResult = tourContext.Tour.AddBooking(1, BikeType.Regular, null, null, RoomType.SingleRoom, DiscountType.None, 0m, null, null);
        Assert.True(addResult.IsSuccess);
        bookingContext.Booking = addResult.Value;
        var result = tourContext.Tour.CancelBooking(bookingContext.Booking.Id);
        Assert.True(result.IsSuccess);
        Assert.Equal(BookingStatus.Cancelled, bookingContext.Booking.Status);
    }

    [Given(@"a tour exists with a completed booking")]
    public void GivenATourExistsWithACompletedBooking()
    {
        tourContext.Tour = TestHelpers.CreateTestTour();
        var addResult = tourContext.Tour.AddBooking(1, BikeType.Regular, null, null, RoomType.SingleRoom, DiscountType.None, 0m, null, null);
        Assert.True(addResult.IsSuccess);
        bookingContext.Booking = addResult.Value;
        var result = tourContext.Tour.CompleteBooking(bookingContext.Booking.Id);
        Assert.True(result.IsSuccess);
        Assert.Equal(BookingStatus.Completed, bookingContext.Booking.Status);
    }

    [Given(@"a tour exists with a booking")]
    public void GivenATourExistsWithABooking()
    {
        tourContext.Tour = TestHelpers.CreateTestTour();
        var result = tourContext.Tour.AddBooking(1, BikeType.Regular, null, null, RoomType.SingleRoom, DiscountType.None, 0m, null, null);
        Assert.True(result.IsSuccess);
        bookingContext.Booking = result.Value;
    }

    [Given(@"a tour exists with a booking priced at (.*) and notes ""(.*)""")]
    public void GivenATourExistsWithABookingPricedAtAndNotes(decimal price, string notes)
    {
        tourContext.Tour = TestHelpers.CreateTestTour();
        var result = tourContext.Tour.AddBooking(1, BikeType.Regular, null, null, RoomType.SingleRoom, DiscountType.None, 0m, null, notes);
        Assert.True(result.IsSuccess);
        bookingContext.Booking = result.Value;
    }

    [When(@"I add a booking for the customer to the tour with price (.*)")]
    public void WhenIAddABookingForTheCustomerToTheTourWithPrice(decimal price)
    {
        var result = tourContext.Tour.AddBooking(1, BikeType.Regular, null, null, RoomType.SingleRoom, DiscountType.None, 0m, null, null);
        Assert.True(result.IsSuccess);
        bookingContext.Booking = result.Value;
    }

    [When(@"I add a booking to tour with bike type ""(.*)"" and no companion")]
    public void WhenIAddABookingToTourWithBikeTypeAndNoCompanion(string bikeTypeString)
    {
        var bikeType = Enum.Parse<BikeType>(bikeTypeString);
        var result = tourContext.Tour.AddBooking(1, bikeType, null, null, RoomType.SingleRoom, DiscountType.None, 0m, null, null);
        Assert.True(result.IsSuccess);
        bookingContext.Booking = result.Value;
    }

    [When(@"I add a booking to tour with room type ""(.*)""")]
    public void WhenIAddABookingToTourWithRoomType(string roomTypeString)
    {
        var roomType = Enum.Parse<RoomType>(roomTypeString);
        var result = tourContext.Tour.AddBooking(1, BikeType.Regular, null, null, roomType, DiscountType.None, 0m, null, null);
        Assert.True(result.IsSuccess);
        bookingContext.Booking = result.Value;
    }

    [When(@"I confirm the booking through the tour")]
    public void WhenIConfirmTheBookingThroughTheTour()
    {
        var result = tourContext.Tour.ConfirmBooking(bookingContext.Booking.Id);
        Assert.True(result.IsSuccess);
    }

    [When(@"I cancel the booking through the tour")]
    public void WhenICancelTheBookingThroughTheTour()
    {
        var result = tourContext.Tour.CancelBooking(bookingContext.Booking.Id);
        Assert.True(result.IsSuccess);
    }

    [When(@"I complete the booking through the tour")]
    public void WhenICompleteTheBookingThroughTheTour()
    {
        var result = tourContext.Tour.CompleteBooking(bookingContext.Booking.Id);
        Assert.True(result.IsSuccess);
    }

    [When(@"I update the booking notes to ""(.*)"" through the tour")]
    public void WhenIUpdateTheBookingNotesToThroughTheTour(string notes)
    {
        var result = tourContext.Tour.UpdateBookingNotes(bookingContext.Booking.Id, notes);
        Assert.True(result.IsSuccess);
    }

    [When(@"I update the payment status to ""(.*)"" through the tour")]
    public void WhenIUpdateThePaymentStatusToThroughTheTour(string paymentStatusString)
    {
        var paymentStatus = TestHelpers.ParsePaymentStatus(paymentStatusString);
        bookingContext.Result = tourContext.Tour.UpdateBookingPaymentStatus(bookingContext.Booking.Id, paymentStatus);
        var result = (Result)bookingContext.Result;
        Assert.True(result.IsSuccess);
    }

    [When(@"I remove the booking from the tour")]
    public void WhenIRemoveTheBookingFromTheTour()
    {
        var result = tourContext.Tour.RemoveBooking(bookingContext.Booking.Id);
        Assert.True(result.IsSuccess);
    }

    [When(@"I try to confirm a non-existent booking")]
    public void WhenITryToConfirmANonExistentBooking()
    {
        bookingContext.Result = tourContext.Tour.ConfirmBooking(99999);
    }

    [When(@"I try to remove a non-existent booking")]
    public void WhenITryToRemoveANonExistentBooking()
    {
        bookingContext.Result = tourContext.Tour.RemoveBooking(99999);
    }

    [When(@"I try to cancel a non-existent booking")]
    public void WhenITryToCancelANonExistentBooking()
    {
        bookingContext.Result = tourContext.Tour.CancelBooking(99999);
    }

    [When(@"I try to complete a non-existent booking")]
    public void WhenITryToCompleteANonExistentBooking()
    {
        bookingContext.Result = tourContext.Tour.CompleteBooking(99999);
    }

    [When(@"I try to update notes for a non-existent booking")]
    public void WhenITryToUpdateNotesForANonExistentBooking()
    {
        bookingContext.Result = tourContext.Tour.UpdateBookingNotes(99999, "Some notes");
    }

    [When(@"I try to update payment status for a non-existent booking")]
    public void WhenITryToUpdatePaymentStatusForANonExistentBooking()
    {
        bookingContext.Result = tourContext.Tour.UpdateBookingPaymentStatus(99999, PaymentStatus.Paid);
    }

    [Then(@"the tour should have the booking")]
    public void ThenTheTourShouldHaveTheBooking()
    {
        Assert.Contains(bookingContext.Booking, tourContext.Tour.Bookings);
    }

    [Then(@"the booking should be in pending status")]
    public void ThenTheBookingShouldBeInPendingStatus()
    {
        Assert.Equal(BookingStatus.Pending, bookingContext.Booking.Status);
    }

    [Then(@"the tour should not have the booking")]
    public void ThenTheTourShouldNotHaveTheBooking()
    {
        Assert.DoesNotContain(bookingContext.Booking, tourContext.Tour.Bookings);
    }
}
