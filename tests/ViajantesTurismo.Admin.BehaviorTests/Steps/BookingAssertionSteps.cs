using Reqnroll;
using ViajantesTurismo.Admin.Domain.Bookings;
using ViajantesTurismo.Common.Results;

namespace ViajantesTurismo.Admin.BehaviorTests.Steps;

[Binding]
public sealed class BookingAssertionSteps(ScenarioContext scenarioContext)
{
    [Then(@"the booking status should be ""(.*)""")]
    public void ThenTheBookingStatusShouldBe(string expectedStatus)
    {
        var booking = scenarioContext.Get<Booking>("Booking");
        var expected = TestHelpers.ParseBookingStatus(expectedStatus);
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

    [Then(@"the booking notes should be null")]
    public void ThenTheBookingNotesShouldBeNull()
    {
        var booking = scenarioContext.Get<Booking>("Booking");
        Assert.Null(booking.Notes);
    }

    [Then(@"the booking payment status should be ""(.*)""")]
    public void ThenTheBookingPaymentStatusShouldBe(string expectedStatusString)
    {
        var booking = scenarioContext.Get<Booking>("Booking");
        var expected = TestHelpers.ParsePaymentStatus(expectedStatusString);
        Assert.Equal(expected, booking.PaymentStatus);
    }

    [Then(@"the operation should fail with message ""(.*)""")]
    public void ThenTheOperationShouldFailWithMessage(string expectedMessage)
    {
        var action = scenarioContext.Get<Action>("Action");
        var exception = Assert.ThrowsAny<Exception>(action);
        Assert.Contains(expectedMessage, exception.Message, StringComparison.Ordinal);
    }

    [Then(@"the operation should fail with argument exception ""(.*)""")]
    public void ThenTheOperationShouldFailWithArgumentException(string expectedMessage)
    {
        var action = scenarioContext.Get<Action>("Action");
        var exception = Assert.Throws<ArgumentException>(action);
        Assert.Contains(expectedMessage, exception.Message, StringComparison.Ordinal);
    }

    [Then(@"the operation should fail with invalid operation exception ""(.*)""")]
    public void ThenTheOperationShouldFailWithInvalidOperationException(string expectedMessage)
    {
        var action = scenarioContext.Get<Action>("Action");
        var exception = Assert.Throws<InvalidOperationException>(action);
        Assert.Contains(expectedMessage, exception.Message, StringComparison.Ordinal);
    }

    [Then(@"the result should fail with message ""(.*)""")]
    public void ThenTheResultShouldFailWithMessage(string expectedMessage)
    {
        var result = scenarioContext.Get<Result>("Result");
        Assert.True(result.IsFailure);
        Assert.Contains(expectedMessage, result.ErrorDetails!.Detail, StringComparison.Ordinal);
    }

    [Then(@"the result should fail with message starting with ""(.*)""")]
    public void ThenTheResultShouldFailWithMessageStartingWith(string expectedMessagePrefix)
    {
        var result = scenarioContext.Get<Result>("Result");
        Assert.True(result.IsFailure);
        Assert.StartsWith(expectedMessagePrefix, result.ErrorDetails!.Detail, StringComparison.Ordinal);
    }
}