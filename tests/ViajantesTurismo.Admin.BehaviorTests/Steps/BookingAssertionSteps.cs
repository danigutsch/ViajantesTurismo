using Reqnroll;
using ViajantesTurismo.Admin.Domain.Bookings;

namespace ViajantesTurismo.Admin.BehaviorTests.Steps;

[Binding]
public sealed class BookingAssertionSteps(ScenarioContext scenarioContext)
{
    [Then(@"the booking status should be ""(.*)""")]
    public void ThenTheBookingStatusShouldBe(string expectedStatus)
    {
        var booking = scenarioContext.Get<Booking>("Booking");
        var expected = expectedStatus switch
        {
            "Pending" => BookingStatus.Pending,
            "Confirmed" => BookingStatus.Confirmed,
            "Cancelled" => BookingStatus.Cancelled,
            "Completed" => BookingStatus.Completed,
            _ => throw new ArgumentException($"Unknown status: {expectedStatus}")
        };

        Assert.Equal(expected, booking.Status);
    }

    [Then(@"the booking price should be (.*)")]
    public void ThenTheBookingPriceShouldBe(decimal expectedPrice)
    {
        var booking = scenarioContext.Get<Booking>("Booking");
        Assert.Equal(expectedPrice, booking.TotalPrice);
    }

    [Then(@"the booking notes should be ""(.*)""")]
    public void ThenTheBookingNotesShouldBe(string expectedNotes)
    {
        var booking = scenarioContext.Get<Booking>("Booking");
        Assert.Equal(expectedNotes, booking.Notes);
    }

    [Then(@"the booking payment status should be ""(.*)""")]
    public void ThenTheBookingPaymentStatusShouldBe(string expectedStatusString)
    {
        var booking = scenarioContext.Get<Booking>("Booking");
        var expected = expectedStatusString switch
        {
            "Paid" => PaymentStatus.Paid,
            "PartiallyPaid" => PaymentStatus.PartiallyPaid,
            "Unpaid" => PaymentStatus.Unpaid,
            _ => throw new ArgumentException($"Unknown payment status: {expectedStatusString}")
        };

        Assert.Equal(expected, booking.PaymentStatus);
    }

    [Then(@"the operation should fail with message ""(.*)""")]
    public void ThenTheOperationShouldFailWithMessage(string expectedMessage)
    {
        var action = scenarioContext.Get<Action>("Action");
        var exception = Assert.ThrowsAny<Exception>(action);
        Assert.Contains(expectedMessage, exception.Message, StringComparison.Ordinal);
    }
}
