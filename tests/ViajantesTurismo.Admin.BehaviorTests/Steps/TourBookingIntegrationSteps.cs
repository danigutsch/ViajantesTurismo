using Reqnroll;
using ViajantesTurismo.Admin.Domain.Bookings;
using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.Admin.BehaviorTests.Steps;

[Binding]
public sealed class TourBookingIntegrationSteps(ScenarioContext scenarioContext)
{
    [Given(@"a tour exists")]
    public void GivenATourExists()
    {
        var tour = TestHelpers.CreateTestTour();
        scenarioContext["Tour"] = tour;
    }

    [Given(@"a customer exists")]
    public static void GivenACustomerExists()
    {
        // Customer ID will be mocked as 1 for testing
    }

    [Given(@"a tour exists with a pending booking")]
    public void GivenATourExistsWithAPendingBooking()
    {
        var tour = TestHelpers.CreateTestTour();
        var booking = tour.AddBooking(customerId: 1, companionId: null, totalPrice: 1500.00m, notes: null);
        scenarioContext["Tour"] = tour;
        scenarioContext["Booking"] = booking;
        Assert.Equal(BookingStatus.Pending, booking.Status);
    }

    [Given(@"a tour exists with a confirmed booking")]
    public void GivenATourExistsWithAConfirmedBooking()
    {
        var tour = TestHelpers.CreateTestTour();
        var booking = tour.AddBooking(customerId: 1, companionId: null, totalPrice: 1500.00m, notes: null);
        tour.ConfirmBooking(booking.Id);
        scenarioContext["Tour"] = tour;
        scenarioContext["Booking"] = booking;
        Assert.Equal(BookingStatus.Confirmed, booking.Status);
    }

    [Given(@"a tour exists with a booking priced at (.*)")]
    public void GivenATourExistsWithABookingPricedAt(decimal price)
    {
        var tour = TestHelpers.CreateTestTour();
        var booking = tour.AddBooking(customerId: 1, companionId: null, totalPrice: price, notes: null);
        scenarioContext["Tour"] = tour;
        scenarioContext["Booking"] = booking;
    }

    [Given(@"a tour exists with a booking")]
    public void GivenATourExistsWithABooking()
    {
        var tour = TestHelpers.CreateTestTour();
        var booking = tour.AddBooking(customerId: 1, companionId: null, totalPrice: 1500.00m, notes: null);
        scenarioContext["Tour"] = tour;
        scenarioContext["Booking"] = booking;
    }

    [Given(@"a tour exists with a booking priced at (.*) and notes ""(.*)""")]
    public void GivenATourExistsWithABookingPricedAtAndNotes(decimal price, string notes)
    {
        var tour = TestHelpers.CreateTestTour();
        var booking = tour.AddBooking(customerId: 1, companionId: null, totalPrice: price, notes: notes);
        scenarioContext["Tour"] = tour;
        scenarioContext["Booking"] = booking;
    }

    [Given(@"a tour exists with a pending booking priced at (.*)")]
    public void GivenATourExistsWithAPendingBookingPricedAt(decimal price)
    {
        var tour = TestHelpers.CreateTestTour();
        var booking = tour.AddBooking(customerId: 1, companionId: null, totalPrice: price, notes: null);
        scenarioContext["Tour"] = tour;
        scenarioContext["Booking"] = booking;
        Assert.Equal(BookingStatus.Pending, booking.Status);
    }

    [When(@"I add a booking for the customer to the tour with price (.*)")]
    public void WhenIAddABookingForTheCustomerToTheTourWithPrice(decimal price)
    {
        var tour = scenarioContext.Get<Tour>("Tour");
        var booking = tour.AddBooking(customerId: 1, companionId: null, totalPrice: price, notes: null);
        scenarioContext["Booking"] = booking;
    }

    [When(@"I confirm the booking through the tour")]
    public void WhenIConfirmTheBookingThroughTheTour()
    {
        var tour = scenarioContext.Get<Tour>("Tour");
        var booking = scenarioContext.Get<Booking>("Booking");
        tour.ConfirmBooking(booking.Id);
    }

    [When(@"I cancel the booking through the tour")]
    public void WhenICancelTheBookingThroughTheTour()
    {
        var tour = scenarioContext.Get<Tour>("Tour");
        var booking = scenarioContext.Get<Booking>("Booking");
        tour.CancelBooking(booking.Id);
    }

    [When(@"I complete the booking through the tour")]
    public void WhenICompleteTheBookingThroughTheTour()
    {
        var tour = scenarioContext.Get<Tour>("Tour");
        var booking = scenarioContext.Get<Booking>("Booking");
        tour.CompleteBooking(booking.Id);
    }

    [When(@"I update the booking price to (.*) through the tour")]
    public void WhenIUpdateTheBookingPriceToThroughTheTour(decimal newPrice)
    {
        var tour = scenarioContext.Get<Tour>("Tour");
        var booking = scenarioContext.Get<Booking>("Booking");
        tour.UpdateBookingPrice(booking.Id, newPrice);
    }

    [When(@"I update the booking notes to ""(.*)"" through the tour")]
    public void WhenIUpdateTheBookingNotesToThroughTheTour(string notes)
    {
        var tour = scenarioContext.Get<Tour>("Tour");
        var booking = scenarioContext.Get<Booking>("Booking");
        tour.UpdateBookingNotes(booking.Id, notes);
    }

    [When(@"I update both price to (.*) and notes to ""(.*)"" through the tour")]
    public void WhenIUpdateBothPriceToAndNotesToThroughTheTour(decimal price, string notes)
    {
        var tour = scenarioContext.Get<Tour>("Tour");
        var booking = scenarioContext.Get<Booking>("Booking");
        tour.UpdateBookingPrice(booking.Id, price);
        tour.UpdateBookingNotes(booking.Id, notes);
    }

    [When(@"I remove the booking from the tour")]
    public void WhenIRemoveTheBookingFromTheTour()
    {
        var tour = scenarioContext.Get<Tour>("Tour");
        var booking = scenarioContext.Get<Booking>("Booking");
        tour.RemoveBooking(booking.Id);
    }

    [When(@"I try to confirm a non-existent booking")]
    public void WhenITryToConfirmANonExistentBooking()
    {
        var tour = scenarioContext.Get<Tour>("Tour");
        scenarioContext["Action"] = () => tour.ConfirmBooking(99999);
    }

    [When(@"I update the booking with price (.*), notes ""(.*)"", status ""(.*)"", and payment ""(.*)""")]
    public void WhenIUpdateTheBookingWithPriceNotesStatusAndPayment(
        decimal price,
        string notes,
        string statusString,
        string paymentString)
    {
        var tour = scenarioContext.Get<Tour>("Tour");
        var booking = scenarioContext.Get<Booking>("Booking");

        var status = TestHelpers.ParseBookingStatus(statusString);
        var payment = TestHelpers.ParsePaymentStatus(paymentString);

        tour.UpdateBookingPrice(booking.Id, price);
        tour.UpdateBookingNotes(booking.Id, notes);
        
        if (booking.Status != status)
        {
            switch (status)
            {
                case BookingStatus.Confirmed:
                    tour.ConfirmBooking(booking.Id);
                    break;
                case BookingStatus.Cancelled:
                    tour.CancelBooking(booking.Id);
                    break;
                case BookingStatus.Completed:
                    tour.CompleteBooking(booking.Id);
                    break;
            }
        }
        
        tour.UpdateBookingPaymentStatus(booking.Id, payment);
    }

    [Then(@"the tour should have the booking")]
    public void ThenTheTourShouldHaveTheBooking()
    {
        var tour = scenarioContext.Get<Tour>("Tour");
        var booking = scenarioContext.Get<Booking>("Booking");
        Assert.Contains(booking, tour.Bookings);
    }

    [Then(@"the booking should be in pending status")]
    public void ThenTheBookingShouldBeInPendingStatus()
    {
        var booking = scenarioContext.Get<Booking>("Booking");
        Assert.Equal(BookingStatus.Pending, booking.Status);
    }

    [Then(@"the tour should not have the booking")]
    public void ThenTheTourShouldNotHaveTheBooking()
    {
        var tour = scenarioContext.Get<Tour>("Tour");
        var booking = scenarioContext.Get<Booking>("Booking");
        Assert.DoesNotContain(booking, tour.Bookings);
    }
}
