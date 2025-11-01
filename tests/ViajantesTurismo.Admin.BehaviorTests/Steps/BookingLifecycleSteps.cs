using Reqnroll;
using ViajantesTurismo.Admin.Domain.Bookings;
using ViajantesTurismo.Admin.Domain.Tours;
using ViajantesTurismo.Common.Monies;

namespace ViajantesTurismo.Admin.BehaviorTests.Steps;

[Binding]
public sealed class BookingLifecycleSteps(ScenarioContext scenarioContext)
{
    [Given(@"a pending booking exists")]
    public void GivenAPendingBookingExists()
    {
        var tour = CreateTestTour();
        var booking = tour.AddBooking(customerId: 1, companionId: null, totalPrice: 1500.00m, notes: null);
        scenarioContext["Tour"] = tour;
        scenarioContext["Booking"] = booking;
        Assert.Equal(BookingStatus.Pending, booking.Status);
    }

    [Given(@"a pending booking exists with price (.*)")]
    public void GivenAPendingBookingExistsWithPrice(decimal price)
    {
        var tour = CreateTestTour();
        var booking = tour.AddBooking(customerId: 1, companionId: null, totalPrice: price, notes: null);
        scenarioContext["Tour"] = tour;
        scenarioContext["Booking"] = booking;
        Assert.Equal(BookingStatus.Pending, booking.Status);
    }

    [Given(@"a confirmed booking exists")]
    public void GivenAConfirmedBookingExists()
    {
        var tour = CreateTestTour();
        var booking = tour.AddBooking(customerId: 1, companionId: null, totalPrice: 1500.00m, notes: null);
        tour.ConfirmBooking(booking.Id);
        scenarioContext["Tour"] = tour;
        scenarioContext["Booking"] = booking;
        Assert.Equal(BookingStatus.Confirmed, booking.Status);
    }

    [Given(@"a cancelled booking exists")]
    public void GivenACancelledBookingExists()
    {
        var tour = CreateTestTour();
        var booking = tour.AddBooking(customerId: 1, companionId: null, totalPrice: 1500.00m, notes: null);
        tour.CancelBooking(booking.Id);
        scenarioContext["Tour"] = tour;
        scenarioContext["Booking"] = booking;
        Assert.Equal(BookingStatus.Cancelled, booking.Status);
    }

    [Given(@"a completed booking exists")]
    public void GivenACompletedBookingExists()
    {
        var tour = CreateTestTour();
        var booking = tour.AddBooking(customerId: 1, companionId: null, totalPrice: 1500.00m, notes: null);
        tour.ConfirmBooking(booking.Id);
        tour.CompleteBooking(booking.Id);
        scenarioContext["Tour"] = tour;
        scenarioContext["Booking"] = booking;
        Assert.Equal(BookingStatus.Completed, booking.Status);
    }

    [When(@"the operator confirms the booking")]
    public void WhenTheOperatorConfirmsTheBooking()
    {
        var tour = scenarioContext.Get<Tour>("Tour");
        var booking = scenarioContext.Get<Booking>("Booking");
        tour.ConfirmBooking(booking.Id);
    }

    [When(@"the operator cancels the booking")]
    public void WhenTheOperatorCancelsTheBooking()
    {
        var tour = scenarioContext.Get<Tour>("Tour");
        var booking = scenarioContext.Get<Booking>("Booking");
        tour.CancelBooking(booking.Id);
    }

    [When(@"the operator completes the booking")]
    public void WhenTheOperatorCompletesTheBooking()
    {
        var tour = scenarioContext.Get<Tour>("Tour");
        var booking = scenarioContext.Get<Booking>("Booking");
        tour.CompleteBooking(booking.Id);
    }

    [When(@"the operator tries to confirm the booking")]
    public void WhenTheOperatorTriesToConfirmTheBooking()
    {
        var tour = scenarioContext.Get<Tour>("Tour");
        var booking = scenarioContext.Get<Booking>("Booking");
        scenarioContext["Action"] = () => tour.ConfirmBooking(booking.Id);
    }

    [When(@"the operator tries to cancel the booking")]
    public void WhenTheOperatorTriesToCancelTheBooking()
    {
        var tour = scenarioContext.Get<Tour>("Tour");
        var booking = scenarioContext.Get<Booking>("Booking");
        scenarioContext["Action"] = () => tour.CancelBooking(booking.Id);
    }

    [When(@"the operator tries to complete the booking")]
    public void WhenTheOperatorTriesToCompleteTheBooking()
    {
        var tour = scenarioContext.Get<Tour>("Tour");
        var booking = scenarioContext.Get<Booking>("Booking");
        scenarioContext["Action"] = () => tour.CompleteBooking(booking.Id);
    }

    [When(@"the operator updates the price to (.*)")]
    public void WhenTheOperatorUpdatesThePriceTo(decimal newPrice)
    {
        var tour = scenarioContext.Get<Tour>("Tour");
        var booking = scenarioContext.Get<Booking>("Booking");
        tour.UpdateBookingPrice(booking.Id, newPrice);
    }

    [When(@"the operator tries to update the price to (.*)")]
    public void WhenTheOperatorTriesToUpdateThePriceTo(decimal newPrice)
    {
        var tour = scenarioContext.Get<Tour>("Tour");
        var booking = scenarioContext.Get<Booking>("Booking");
        scenarioContext["Action"] = () => tour.UpdateBookingPrice(booking.Id, newPrice);
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
        
        var paymentStatus = paymentStatusString switch
        {
            "Paid" => PaymentStatus.Paid,
            "PartiallyPaid" => PaymentStatus.PartiallyPaid,
            "Unpaid" => PaymentStatus.Unpaid,
            _ => throw new ArgumentException($"Unknown payment status: {paymentStatusString}")
        };

        // Update through Tour aggregate - Booking should not be modified directly
        tour.UpdateBooking(booking.Id, booking.TotalPrice, booking.Notes, booking.Status, paymentStatus);
    }

    private static Tour CreateTestTour()
    {
        return new Tour(
            identifier: "TEST2024",
            name: "Test Tour",
            startDate: DateTime.UtcNow.AddMonths(1),
            endDate: DateTime.UtcNow.AddMonths(1).AddDays(7),
            price: 2000.00m,
            singleRoomSupplementPrice: 500.00m,
            regularBikePrice: 100.00m,
            eBikePrice: 200.00m,
            currency: Currency.UsDollar,
            includedServices: ["Hotel", "Breakfast"]);
    }
}
