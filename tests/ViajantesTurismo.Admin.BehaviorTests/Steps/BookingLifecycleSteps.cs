using Reqnroll;
using ViajantesTurismo.Admin.Domain.Bookings;
using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.Admin.BehaviorTests.Steps;

[Binding]
public sealed class BookingLifecycleSteps(ScenarioContext scenarioContext)
{
    [Given(@"a pending booking exists")]
    public void GivenAPendingBookingExists()
    {
        var tour = TestHelpers.CreateTestTour();
        var booking = tour.AddBooking(customerId: 1, companionId: null, totalPrice: 1500.00m, notes: null);
        scenarioContext["Tour"] = tour;
        scenarioContext["Booking"] = booking;
        Assert.Equal(BookingStatus.Pending, booking.Status);
    }

    [Given(@"a pending booking exists with price (.*)")]
    public void GivenAPendingBookingExistsWithPrice(decimal price)
    {
        var tour = TestHelpers.CreateTestTour();
        var booking = tour.AddBooking(customerId: 1, companionId: null, totalPrice: price, notes: null);
        scenarioContext["Tour"] = tour;
        scenarioContext["Booking"] = booking;
        Assert.Equal(BookingStatus.Pending, booking.Status);
    }

    [Given(@"a confirmed booking exists")]
    public void GivenAConfirmedBookingExists()
    {
        var tour = TestHelpers.CreateTestTour();
        var booking = tour.AddBooking(customerId: 1, companionId: null, totalPrice: 1500.00m, notes: null);
        tour.ConfirmBooking(booking.Id);
        scenarioContext["Tour"] = tour;
        scenarioContext["Booking"] = booking;
        Assert.Equal(BookingStatus.Confirmed, booking.Status);
    }

    [Given(@"a cancelled booking exists")]
    public void GivenACancelledBookingExists()
    {
        var tour = TestHelpers.CreateTestTour();
        var booking = tour.AddBooking(customerId: 1, companionId: null, totalPrice: 1500.00m, notes: null);
        var result = tour.CancelBooking(booking.Id);
        Assert.True(result.IsSuccess);
        scenarioContext["Tour"] = tour;
        scenarioContext["Booking"] = booking;
        Assert.Equal(BookingStatus.Cancelled, booking.Status);
    }

    [Given(@"a completed booking exists")]
    public void GivenACompletedBookingExists()
    {
        var tour = TestHelpers.CreateTestTour();
        var booking = tour.AddBooking(customerId: 1, companionId: null, totalPrice: 1500.00m, notes: null);
        var confirmResult = tour.ConfirmBooking(booking.Id);
        Assert.True(confirmResult.IsSuccess);
        var completeResult = tour.CompleteBooking(booking.Id);
        Assert.True(completeResult.IsSuccess);
        scenarioContext["Tour"] = tour;
        scenarioContext["Booking"] = booking;
        Assert.Equal(BookingStatus.Completed, booking.Status);
    }

    [When(@"the operator confirms the booking")]
    public void WhenTheOperatorConfirmsTheBooking()
    {
        var tour = scenarioContext.Get<Tour>("Tour");
        var booking = scenarioContext.Get<Booking>("Booking");
        var result = tour.ConfirmBooking(booking.Id);
        Assert.True(result.IsSuccess);
    }

    [When(@"the operator cancels the booking")]
    public void WhenTheOperatorCancelsTheBooking()
    {
        var tour = scenarioContext.Get<Tour>("Tour");
        var booking = scenarioContext.Get<Booking>("Booking");
        var result = tour.CancelBooking(booking.Id);
        Assert.True(result.IsSuccess);
    }

    [When(@"the operator completes the booking")]
    public void WhenTheOperatorCompletesTheBooking()
    {
        var tour = scenarioContext.Get<Tour>("Tour");
        var booking = scenarioContext.Get<Booking>("Booking");
        var result = tour.CompleteBooking(booking.Id);
        Assert.True(result.IsSuccess);
    }

    [When(@"the operator tries to confirm the booking")]
    public void WhenTheOperatorTriesToConfirmTheBooking()
    {
        var tour = scenarioContext.Get<Tour>("Tour");
        var booking = scenarioContext.Get<Booking>("Booking");
        scenarioContext["Result"] = tour.ConfirmBooking(booking.Id);
    }

    [When(@"the operator tries to cancel the booking")]
    public void WhenTheOperatorTriesToCancelTheBooking()
    {
        var tour = scenarioContext.Get<Tour>("Tour");
        var booking = scenarioContext.Get<Booking>("Booking");
        scenarioContext["Result"] = tour.CancelBooking(booking.Id);
    }

    [When(@"the operator tries to complete the booking")]
    public void WhenTheOperatorTriesToCompleteTheBooking()
    {
        var tour = scenarioContext.Get<Tour>("Tour");
        var booking = scenarioContext.Get<Booking>("Booking");
        scenarioContext["Result"] = tour.CompleteBooking(booking.Id);
    }

    [When(@"the operator updates the price to (.*)")]
    public void WhenTheOperatorUpdatesThePriceTo(decimal newPrice)
    {
        var tour = scenarioContext.Get<Tour>("Tour");
        var booking = scenarioContext.Get<Booking>("Booking");
        var result = tour.UpdateBookingPrice(booking.Id, newPrice);
        Assert.True(result.IsSuccess);
    }

    [When(@"the operator tries to update the price to (.*)")]
    public void WhenTheOperatorTriesToUpdateThePriceTo(decimal newPrice)
    {
        var tour = scenarioContext.Get<Tour>("Tour");
        var booking = scenarioContext.Get<Booking>("Booking");
        scenarioContext["Result"] = tour.UpdateBookingPrice(booking.Id, newPrice);
    }

    [When(@"the operator updates the notes to ""(.*)""")]
    public void WhenTheOperatorUpdatesTheNotesTo(string notes)
    {
        var tour = scenarioContext.Get<Tour>("Tour");
        var booking = scenarioContext.Get<Booking>("Booking");
        tour.UpdateBookingNotes(booking.Id, notes);
    }

    [When(@"the operator updates the payment status to ""(.*)""")]
    public void WhenTheOperatorUpdatesThePaymentStatusTo(string paymentStatusString)
    {
        var tour = scenarioContext.Get<Tour>("Tour");
        var booking = scenarioContext.Get<Booking>("Booking");

        var paymentStatus = TestHelpers.ParsePaymentStatus(paymentStatusString);

        var result = tour.UpdateBookingPaymentStatus(booking.Id, paymentStatus);
        Assert.True(result.IsSuccess);
    }
}
