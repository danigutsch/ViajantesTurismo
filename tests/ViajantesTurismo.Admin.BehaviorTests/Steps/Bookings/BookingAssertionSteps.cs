namespace ViajantesTurismo.Admin.BehaviorTests.Steps.Bookings;

[Binding]
public sealed class BookingAssertionSteps(BookingContext context, TourContext tourContext)
{
    [Then("the booking update should fail with conflict error")]
    public void ThenTheBookingUpdateShouldFailWithConflictError()
    {
        TestAssert.NotNull(context.BookingOperationResult);
        var result = context.BookingOperationResult.Value;
        TestAssert.False(result.IsSuccess);
        TestAssert.Equal(ResultStatus.Conflict, result.Status);
    }

    [Then(@"the booking status should be ""(.*)""")]
    public void ThenTheBookingStatusShouldBe(string expectedStatus)
    {
        var expected = EntityBuilders.ParseBookingStatus(expectedStatus);
        TestAssert.Equal(expected, context.Booking.Status);
    }

    [Then(@"the booking notes should be ""(.*)""")]
    public void ThenTheBookingNotesShouldBe(string expectedNotes)
    {
        TestAssert.Equal(expectedNotes, context.Booking.Notes);
    }

    [Then("the booking notes should be null")]
    public void ThenTheBookingNotesShouldBeNull()
    {
        TestAssert.True(string.IsNullOrEmpty(context.Booking.Notes));
    }

    [Then(@"the booking payment status should be ""(.*)""")]
    public void ThenTheBookingPaymentStatusShouldBe(string expectedStatusString)
    {
        var expected = EntityBuilders.ParsePaymentStatus(expectedStatusString);
        TestAssert.Equal(expected, context.Booking.PaymentStatus);
    }

    [Then(@"the result should fail with message ""(.*)""")]
    public void ThenTheResultShouldFailWithMessage(string expectedMessage)
    {
        TestAssert.NotNull(context.BookingOperationResult);
        var result = context.BookingOperationResult.Value;
        TestAssert.True(result.IsFailure);
        var errorDetails = TestAssert.NotNull(result.ErrorDetails);
        TestAssert.Contains(expectedMessage, errorDetails.Detail, StringComparison.Ordinal);
    }

    [Then(@"the result should fail with message starting with ""(.*)""")]
    public void ThenTheResultShouldFailWithMessageStartingWith(string expectedMessagePrefix)
    {
        TestAssert.NotNull(context.BookingOperationResult);
        var result = context.BookingOperationResult.Value;
        TestAssert.True(result.IsFailure);
        var errorDetails = TestAssert.NotNull(result.ErrorDetails);
        TestAssert.StartsWith(expectedMessagePrefix, errorDetails.Detail, StringComparison.Ordinal);
    }

    [Then("the booking room additional cost should be (.*)")]
    public void ThenTheBookingRoomAdditionalCostShouldBe(decimal expectedCost)
    {
        TestAssert.Equal(expectedCost, context.Booking.RoomAdditionalCost);
    }

    [Then("the booking room additional cost should be the tour single room supplement")]
    public void ThenTheBookingRoomAdditionalCostShouldBeTheTourSingleRoomSupplement()
    {
        TestAssert.Equal(tourContext.Tour.Pricing.SingleRoomSupplementPrice, context.Booking.RoomAdditionalCost);
    }

    [Then("the booking principal customer bike price should be the tour regular bike price")]
    public void ThenTheBookingPrincipalCustomerBikePriceShouldBeTheTourRegularBikePrice()
    {
        TestAssert.Equal(tourContext.Tour.Pricing.RegularBikePrice, context.Booking.PrincipalCustomer.BikePrice);
    }

    [Then("the booking principal customer bike price should be the tour ebike price")]
    public void ThenTheBookingPrincipalCustomerBikePriceShouldBeTheTourEbikePrice()
    {
        TestAssert.Equal(tourContext.Tour.Pricing.EBikePrice, context.Booking.PrincipalCustomer.BikePrice);
    }
}
