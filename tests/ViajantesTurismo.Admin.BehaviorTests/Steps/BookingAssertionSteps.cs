using Reqnroll;
using ViajantesTurismo.Admin.BehaviorTests.Context;

namespace ViajantesTurismo.Admin.BehaviorTests.Steps;

[Binding]
public sealed class BookingAssertionSteps(BookingContext context)
{
    [Then(@"the booking status should be ""(.*)""")]
    public void ThenTheBookingStatusShouldBe(string expectedStatus)
    {
        var expected = TestHelpers.ParseBookingStatus(expectedStatus);
        Assert.Equal(expected, context.Booking.Status);
    }

    [Then(@"the booking price should be (.*)")]
    public void ThenTheBookingPriceShouldBe(decimal expectedPrice)
    {
        Assert.Equal(expectedPrice, context.Booking.TotalPrice);
    }

    [Then(@"the booking notes should be ""(.*)""")]
    public void ThenTheBookingNotesShouldBe(string expectedNotes)
    {
        Assert.Equal(expectedNotes, context.Booking.Notes);
    }

    [Then(@"the booking notes should be null")]
    public void ThenTheBookingNotesShouldBeNull()
    {
        Assert.Null(context.Booking.Notes);
    }

    [Then(@"the booking payment status should be ""(.*)""")]
    public void ThenTheBookingPaymentStatusShouldBe(string expectedStatusString)
    {
        var expected = TestHelpers.ParsePaymentStatus(expectedStatusString);
        Assert.Equal(expected, context.Booking.PaymentStatus);
    }

    [Then(@"the operation should fail with message ""(.*)""")]
    public void ThenTheOperationShouldFailWithMessage(string expectedMessage)
    {
        var exception = Assert.ThrowsAny<Exception>(context.Action);
        Assert.Contains(expectedMessage, exception.Message, StringComparison.Ordinal);
    }

    [Then(@"the operation should fail with argument exception ""(.*)""")]
    public void ThenTheOperationShouldFailWithArgumentException(string expectedMessage)
    {
        var exception = Assert.Throws<ArgumentException>(context.Action);
        Assert.Contains(expectedMessage, exception.Message, StringComparison.Ordinal);
    }

    [Then(@"the operation should fail with invalid operation exception ""(.*)""")]
    public void ThenTheOperationShouldFailWithInvalidOperationException(string expectedMessage)
    {
        var exception = Assert.Throws<InvalidOperationException>(context.Action);
        Assert.Contains(expectedMessage, exception.Message, StringComparison.Ordinal);
    }

    [Then(@"the result should fail with message ""(.*)""")]
    public void ThenTheResultShouldFailWithMessage(string expectedMessage)
    {
        var result = context.Result;
        Assert.True(result.IsFailure);
        Assert.Contains(expectedMessage, result.ErrorDetails!.Detail, StringComparison.Ordinal);
    }

    [Then(@"the result should fail with message starting with ""(.*)""")]
    public void ThenTheResultShouldFailWithMessageStartingWith(string expectedMessagePrefix)
    {
        var result = context.Result;
        Assert.True(result.IsFailure);
        Assert.StartsWith(expectedMessagePrefix, result.ErrorDetails!.Detail, StringComparison.Ordinal);
    }
}
