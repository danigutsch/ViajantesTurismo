using Reqnroll;
using ViajantesTurismo.Admin.BehaviorTests.Context;
using ViajantesTurismo.Admin.Domain.Bookings;

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
        // Customer ID will be mocked as 1 for testing
    }

    [Given(@"a tour exists with a pending booking")]
    public void GivenATourExistsWithAPendingBooking()
    {
        tourContext.Tour = TestHelpers.CreateTestTour();
        var result = tourContext.Tour.AddBooking(customerId: 1, companionId: null, notes: null);
        Assert.True(result.IsSuccess);
        bookingContext.Booking = result.Value;
        Assert.Equal(BookingStatus.Pending, bookingContext.Booking.Status);
    }

    [Given(@"a tour exists with a confirmed booking")]
    public void GivenATourExistsWithAConfirmedBooking()
    {
        tourContext.Tour = TestHelpers.CreateTestTour();
        var addResult = tourContext.Tour.AddBooking(customerId: 1, companionId: null, notes: null);
        Assert.True(addResult.IsSuccess);
        bookingContext.Booking = addResult.Value;
        var result = tourContext.Tour.ConfirmBooking(bookingContext.Booking.Id);
        Assert.True(result.IsSuccess);
        Assert.Equal(BookingStatus.Confirmed, bookingContext.Booking.Status);
    }

    [Given(@"a tour exists with a booking priced at (.*)")]
    public void GivenATourExistsWithABookingPricedAt(decimal price)
    {
        tourContext.Tour = TestHelpers.CreateTestTour();
        var result = tourContext.Tour.AddBooking(customerId: 1, companionId: null, notes: null);
        Assert.True(result.IsSuccess);
        bookingContext.Booking = result.Value;
    }

    [Given(@"a tour exists with a booking")]
    public void GivenATourExistsWithABooking()
    {
        tourContext.Tour = TestHelpers.CreateTestTour();
        var result = tourContext.Tour.AddBooking(customerId: 1, companionId: null, notes: null);
        Assert.True(result.IsSuccess);
        bookingContext.Booking = result.Value;
    }

    [Given(@"a tour exists with a booking priced at (.*) and notes ""(.*)""")]
    public void GivenATourExistsWithABookingPricedAtAndNotes(decimal price, string notes)
    {
        tourContext.Tour = TestHelpers.CreateTestTour();
        var result = tourContext.Tour.AddBooking(customerId: 1, companionId: null, notes: notes);
        Assert.True(result.IsSuccess);
        bookingContext.Booking = result.Value;
    }

    [Given(@"a tour exists with a pending booking priced at (.*)")]
    public void GivenATourExistsWithAPendingBookingPricedAt(decimal price)
    {
        tourContext.Tour = TestHelpers.CreateTestTour();
        var result = tourContext.Tour.AddBooking(customerId: 1, companionId: null, notes: null);
        Assert.True(result.IsSuccess);
        bookingContext.Booking = result.Value;
        Assert.Equal(BookingStatus.Pending, bookingContext.Booking.Status);
    }

    [When(@"I add a booking for the customer to the tour with price (.*)")]
    public void WhenIAddABookingForTheCustomerToTheTourWithPrice(decimal price)
    {
        var result = tourContext.Tour.AddBooking(customerId: 1, companionId: null, notes: null);
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

    [When(@"I update the booking price to (.*) through the tour")]
    public void WhenIUpdateTheBookingPriceToThroughTheTour(decimal newPrice)
    {
        var result = tourContext.Tour.UpdateBookingPrice(bookingContext.Booking.Id, newPrice);
        Assert.True(result.IsSuccess);
    }

    [When(@"I update the booking notes to ""(.*)"" through the tour")]
    public void WhenIUpdateTheBookingNotesToThroughTheTour(string notes)
    {
        var result = tourContext.Tour.UpdateBookingNotes(bookingContext.Booking.Id, notes);
        Assert.True(result.IsSuccess);
    }

    [When(@"I update both price to (.*) and notes to ""(.*)"" through the tour")]
    public void WhenIUpdateBothPriceToAndNotesToThroughTheTour(decimal price, string notes)
    {
        var priceResult = tourContext.Tour.UpdateBookingPrice(bookingContext.Booking.Id, price);
        Assert.True(priceResult.IsSuccess);
        var notesResult = tourContext.Tour.UpdateBookingNotes(bookingContext.Booking.Id, notes);
        Assert.True(notesResult.IsSuccess);
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

    [When(@"I try to update price for a non-existent booking")]
    public void WhenITryToUpdatePriceForANonExistentBooking()
    {
        bookingContext.Result = tourContext.Tour.UpdateBookingPrice(99999, 1000.00m);
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

    [When(@"I update the booking with price (.*), notes ""(.*)"", status ""(.*)"", and payment ""(.*)""")]
    public void WhenIUpdateTheBookingWithPriceNotesStatusAndPayment(
        decimal price,
        string notes,
        string statusString,
        string paymentString)
    {
        var status = TestHelpers.ParseBookingStatus(statusString);
        var payment = TestHelpers.ParsePaymentStatus(paymentString);

        var priceResult = tourContext.Tour.UpdateBookingPrice(bookingContext.Booking.Id, price);
        Assert.True(priceResult.IsSuccess);
        var notesResult = tourContext.Tour.UpdateBookingNotes(bookingContext.Booking.Id, notes);
        Assert.True(notesResult.IsSuccess);

        if (bookingContext.Booking.Status != status)
        {
            switch (status)
            {
                case BookingStatus.Confirmed:
                    var confirmResult = tourContext.Tour.ConfirmBooking(bookingContext.Booking.Id);
                    Assert.True(confirmResult.IsSuccess);
                    break;
                case BookingStatus.Cancelled:
                    var cancelResult = tourContext.Tour.CancelBooking(bookingContext.Booking.Id);
                    Assert.True(cancelResult.IsSuccess);
                    break;
                case BookingStatus.Completed:
                    var completeResult = tourContext.Tour.CompleteBooking(bookingContext.Booking.Id);
                    Assert.True(completeResult.IsSuccess);
                    break;
            }
        }

        var result = tourContext.Tour.UpdateBookingPaymentStatus(bookingContext.Booking.Id, payment);
        Assert.True(result.IsSuccess);
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