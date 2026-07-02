namespace ViajantesTurismo.Admin.BehaviorTests.Steps.Bookings;

[Binding]
public sealed class BookingAssertionSteps(BookingContext context, TourContext tourContext)
{
    [Then("the booking update should fail with conflict error")]
    public void ThenTheBookingUpdateShouldFailWithConflictError()
    {
        var result = (context.BookingOperationResult).ShouldNotBeNull();
        (result.IsSuccess).ShouldBeFalse();
        (result.Status).ShouldBe(ResultStatus.Conflict);
    }

    [Then(@"the booking status should be ""(.*)""")]
    public void ThenTheBookingStatusShouldBe(string expectedStatus)
    {
        var expected = EntityBuilders.ParseBookingStatus(expectedStatus);
        (context.Booking.Status).ShouldBe(expected);
    }

    [Then(@"the booking notes should be ""(.*)""")]
    public void ThenTheBookingNotesShouldBe(string expectedNotes)
    {
        (context.Booking.Notes).ShouldBe(expectedNotes);
    }

    [Then("the booking notes should be null")]
    public void ThenTheBookingNotesShouldBeNull()
    {
        (string.IsNullOrEmpty(context.Booking.Notes)).ShouldBeTrue();
    }

    [Then(@"the booking payment status should be ""(.*)""")]
    public void ThenTheBookingPaymentStatusShouldBe(string expectedStatusString)
    {
        var expected = EntityBuilders.ParsePaymentStatus(expectedStatusString);
        (context.Booking.PaymentStatus).ShouldBe(expected);
    }

    [Then(@"the result should fail with message ""(.*)""")]
    public void ThenTheResultShouldFailWithMessage(string expectedMessage)
    {
        var result = (context.BookingOperationResult).ShouldNotBeNull();
        (result.IsFailure).ShouldBeTrue();
        var errorDetails = (result.ErrorDetails).ShouldNotBeNull();
        (errorDetails.Detail).ShouldContain(expectedMessage, StringComparison.Ordinal);
    }

    [Then(@"the result should fail with message starting with ""(.*)""")]
    public void ThenTheResultShouldFailWithMessageStartingWith(string expectedMessagePrefix)
    {
        var result = (context.BookingOperationResult).ShouldNotBeNull();
        (result.IsFailure).ShouldBeTrue();
        var errorDetails = (result.ErrorDetails).ShouldNotBeNull();
        (errorDetails.Detail).ShouldStartWith(expectedMessagePrefix);
    }

    [Then("the booking room additional cost should be (.*)")]
    public void ThenTheBookingRoomAdditionalCostShouldBe(decimal expectedCost)
    {
        (context.Booking.RoomAdditionalCost).ShouldBe(expectedCost);
    }

    [Then("the booking room additional cost should be the tour single room supplement")]
    public void ThenTheBookingRoomAdditionalCostShouldBeTheTourSingleRoomSupplement()
    {
        (context.Booking.RoomAdditionalCost).ShouldBe(tourContext.Tour.Pricing.SingleRoomSupplementPrice);
    }

    [Then("the booking principal customer bike price should be the tour regular bike price")]
    public void ThenTheBookingPrincipalCustomerBikePriceShouldBeTheTourRegularBikePrice()
    {
        (context.Booking.PrincipalCustomer.BikePrice).ShouldBe(tourContext.Tour.Pricing.RegularBikePrice);
    }

    [Then("the booking principal customer bike price should be the tour ebike price")]
    public void ThenTheBookingPrincipalCustomerBikePriceShouldBeTheTourEbikePrice()
    {
        (context.Booking.PrincipalCustomer.BikePrice).ShouldBe(tourContext.Tour.Pricing.EBikePrice);
    }
}
