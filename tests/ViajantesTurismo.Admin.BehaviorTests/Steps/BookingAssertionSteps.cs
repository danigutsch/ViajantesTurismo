using Reqnroll;
using ViajantesTurismo.Admin.BehaviorTests.Context;
using ViajantesTurismo.Common.Results;

namespace ViajantesTurismo.Admin.BehaviorTests.Steps;

[Binding]
public sealed class BookingAssertionSteps(BookingContext context, TourContext tourContext)
{
    [Then("the booking update should fail with conflict error")]
    public void ThenTheBookingUpdateShouldFailWithConflictError()
    {
        var result = (Result)context.Result;
        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Conflict, result.Status);
    }

    [Then(@"the booking status should be ""(.*)""")]
    public void ThenTheBookingStatusShouldBe(string expectedStatus)
    {
        var expected = EntityBuilders.ParseBookingStatus(expectedStatus);
        Assert.Equal(expected, context.Booking.Status);
    }

    [Then(@"the booking notes should be ""(.*)""")]
    public void ThenTheBookingNotesShouldBe(string expectedNotes)
    {
        Assert.Equal(expectedNotes, context.Booking.Notes);
    }

    [Then("the booking notes should be null")]
    public void ThenTheBookingNotesShouldBeNull()
    {
        Assert.True(string.IsNullOrEmpty(context.Booking.Notes));
    }

    [Then(@"the booking payment status should be ""(.*)""")]
    public void ThenTheBookingPaymentStatusShouldBe(string expectedStatusString)
    {
        var expected = EntityBuilders.ParsePaymentStatus(expectedStatusString);
        Assert.Equal(expected, context.Booking.PaymentStatus);
    }

    [Then(@"the result should fail with message ""(.*)""")]
    public void ThenTheResultShouldFailWithMessage(string expectedMessage)
    {
        var result = (Result)context.Result;
        Assert.True(result.IsFailure);
        Assert.Contains(expectedMessage, result.ErrorDetails!.Detail, StringComparison.Ordinal);
    }

    [Then(@"the result should fail with message starting with ""(.*)""")]
    public void ThenTheResultShouldFailWithMessageStartingWith(string expectedMessagePrefix)
    {
        var result = (Result)context.Result;
        Assert.True(result.IsFailure);
        Assert.StartsWith(expectedMessagePrefix, result.ErrorDetails!.Detail, StringComparison.Ordinal);
    }

    [Then("the booking room additional cost should be (.*)")]
    public void ThenTheBookingRoomAdditionalCostShouldBe(decimal expectedCost)
    {
        Assert.Equal(expectedCost, context.Booking.RoomAdditionalCost);
    }

    [Then("the booking room additional cost should be the tour double room supplement")]
    public void ThenTheBookingRoomAdditionalCostShouldBeTheTourDoubleRoomSupplement()
    {
        Assert.Equal(tourContext.Tour.Pricing.DoubleRoomSupplementPrice, context.Booking.RoomAdditionalCost);
    }

    [Then("the booking principal customer bike price should be the tour regular bike price")]
    public void ThenTheBookingPrincipalCustomerBikePriceShouldBeTheTourRegularBikePrice()
    {
        Assert.Equal(tourContext.Tour.Pricing.RegularBikePrice, context.Booking.PrincipalCustomer.BikePrice);
    }

    [Then("the booking principal customer bike price should be the tour ebike price")]
    public void ThenTheBookingPrincipalCustomerBikePriceShouldBeTheTourEbikePrice()
    {
        Assert.Equal(tourContext.Tour.Pricing.EBikePrice, context.Booking.PrincipalCustomer.BikePrice);
    }
}
