using Reqnroll;
using ViajantesTurismo.Admin.BehaviorTests.Context;
using ViajantesTurismo.Admin.Domain.Customers;
using ViajantesTurismo.Admin.Domain.Tours;
using ViajantesTurismo.Common.Results;

namespace ViajantesTurismo.Admin.BehaviorTests.Steps;

[Binding]
public sealed class BookingEntitySteps(BookingContext bookingContext)
{
    private static BookingCustomer CreatePrincipalCustomer(decimal bikePrice = 100m, BikeType bikeType = BikeType.Regular)
    {
        var result = BookingCustomer.Create(1, bikeType, bikePrice);
        return result.Value;
    }

    private static BookingCustomer CreateCompanionCustomer(decimal bikePrice = 200m, BikeType bikeType = BikeType.EBike)
    {
        var result = BookingCustomer.Create(2, bikeType, bikePrice);
        return result.Value;
    }

    [When(@"I create a booking with base price (.*), room type ""(.*)"", room cost (.*), and regular bike (.*) for principal")]
    public void WhenICreateABookingWithBasePriceAndRegularBike(decimal basePrice, string roomType, decimal roomCost, decimal bikePrice)
    {
        var principal = CreatePrincipalCustomer(bikePrice);
        var room = Enum.Parse<RoomType>(roomType);
        bookingContext.Result = Booking.Create(1, basePrice, room, roomCost, principal, null, null);
        bookingContext.Action = null!;
    }

    [When(@"I create a booking with base price (.*), room type ""(.*)"", room cost (.*), regular bike (.*) for principal, and eBike (.*) for companion")]
    public void WhenICreateABookingWithPrincipalAndCompanion(decimal basePrice, string roomType, decimal roomCost, decimal principalBikePrice, decimal companionBikePrice)
    {
        var principal = CreatePrincipalCustomer(principalBikePrice);
        var companion = CreateCompanionCustomer(companionBikePrice);
        var room = Enum.Parse<RoomType>(roomType);
        bookingContext.Result = Booking.Create(1, basePrice, room, roomCost, principal, companion, null);
        bookingContext.Action = null!;
    }

    [When(@"I try to create a booking with base price (.*)")]
    public void WhenITryToCreateABookingWithBasePrice(decimal basePrice)
    {
        var principal = CreatePrincipalCustomer();
        bookingContext.Result = Booking.Create(1, basePrice, RoomType.DoubleRoom, 0m, principal, null, null);
        bookingContext.Action = null!;
    }

    [Then(@"the booking price update should fail")]
    public void ThenTheBookingPriceUpdateShouldFail()
    {
        var result = bookingContext.Result switch
        {
            Result r => r,
            Result<Booking> rb => rb.ToResult(),
            _ => throw new InvalidOperationException("Result is not set")
        };
        Assert.False(result.IsSuccess);
    }

    [When(@"I try to create a booking with base price (.*) and room cost (.*)")]
    public void WhenITryToCreateABookingWithBasePriceAndRoomCost(decimal basePrice, decimal roomCost)
    {
        var principal = CreatePrincipalCustomer();
        bookingContext.Result = Booking.Create(1, basePrice, RoomType.SingleRoom, roomCost, principal, null, null);
        bookingContext.Action = null!;
    }

    [When(@"I try to create a booking with notes of (.*) characters")]
    public void WhenITryToCreateABookingWithNotesOfCharacters(int length)
    {
        var principal = CreatePrincipalCustomer();
        var notes = new string('x', length);
        bookingContext.Result = Booking.Create(1, 1000m, RoomType.DoubleRoom, 0m, principal, null, notes);
        bookingContext.Action = null!;
    }

    [When(@"I create a booking with notes of (.*) characters")]
    public void WhenICreateABookingWithNotesOfCharacters(int length)
    {
        var principal = CreatePrincipalCustomer();
        var notes = new string('x', length);
        bookingContext.Result = Booking.Create(1, 1000m, RoomType.DoubleRoom, 0m, principal, null, notes);
        bookingContext.Action = null!;
    }

    [When(@"I create a booking with notes ""(.*)""")]
    public void WhenICreateABookingWithNotes(string notes)
    {
        var principal = CreatePrincipalCustomer();
        bookingContext.Result = Booking.Create(1, 1000m, RoomType.DoubleRoom, 0m, principal, null, notes);
        bookingContext.Action = null!;
    }

    [Given(@"a booking exists")]
    public void GivenABookingExists()
    {
        var principal = CreatePrincipalCustomer();
        var result = Booking.Create(1, 1000m, RoomType.DoubleRoom, 0m, principal, null, null);
        bookingContext.Booking = result.Value;
        bookingContext.Result = result;
        bookingContext.Action = null!;
    }

    [Given(@"a booking exists with notes ""(.*)""")]
    public void GivenABookingExistsWithNotes(string notes)
    {
        var principal = CreatePrincipalCustomer();
        var result = Booking.Create(1, 1000m, RoomType.DoubleRoom, 0m, principal, null, notes);
        bookingContext.Booking = result.Value;
        bookingContext.Result = result;
        bookingContext.Action = null!;
    }

    [Given(@"a booking exists with status ""(.*)""")]
    public void GivenABookingExistsWithStatus(string status)
    {
        var principal = CreatePrincipalCustomer();
        var result = Booking.Create(1, 1000m, RoomType.DoubleRoom, 0m, principal, null, null);
        bookingContext.Booking = result.Value;
        bookingContext.Result = result;
        bookingContext.Action = null!;

        switch (status)
        {
            case "Confirmed":
                bookingContext.Booking.Confirm();
                break;
            case "Cancelled":
                bookingContext.Booking.Cancel();
                break;
            case "Completed":
                bookingContext.Booking.Complete();
                break;
        }
    }

    [When(@"I update the payment status to ""(.*)""")]
    public void WhenIUpdateThePaymentStatusTo(string status)
    {
        var paymentStatus = Enum.Parse<PaymentStatus>(status);
        bookingContext.Booking.UpdatePaymentStatus(paymentStatus);
    }

    [When(@"I confirm the booking")]
    public void WhenIConfirmTheBooking()
    {
        bookingContext.Result = bookingContext.Booking.Confirm();
    }

    [When(@"I try to confirm the booking")]
    public void WhenITryToConfirmTheBooking()
    {
        bookingContext.Result = bookingContext.Booking.Confirm();
    }

    [When(@"I cancel the booking")]
    public void WhenICancelTheBooking()
    {
        bookingContext.Result = bookingContext.Booking.Cancel();
    }

    [When(@"I try to cancel the booking")]
    public void WhenITryToCancelTheBooking()
    {
        bookingContext.Result = bookingContext.Booking.Cancel();
    }

    [When(@"I complete the booking")]
    public void WhenICompleteTheBooking()
    {
        bookingContext.Result = bookingContext.Booking.Complete();
    }

    [When(@"I try to complete the booking")]
    public void WhenITryToCompleteTheBooking()
    {
        bookingContext.Result = bookingContext.Booking.Complete();
    }

    [When(@"I update the booking total price to (.*)")]
    public void WhenIUpdateTheBookingTotalPriceTo(decimal price)
    {
        bookingContext.Result = bookingContext.Booking.UpdatePrice(price);
    }

    [When(@"I try to update the booking total price to (.*)")]
    public void WhenITryToUpdateTheBookingTotalPriceTo(decimal price)
    {
        bookingContext.Result = bookingContext.Booking.UpdatePrice(price);
    }

    [When(@"I update the booking notes to ""(.*)""")]
    public void WhenIUpdateTheBookingNotesTo(string notes)
    {
        bookingContext.Result = bookingContext.Booking.UpdateNotes(notes);
    }

    [When(@"I update the booking notes to null")]
    public void WhenIUpdateTheBookingNotesToNull()
    {
        bookingContext.Result = bookingContext.Booking.UpdateNotes(null);
    }

    [When(@"I try to update the booking notes to (.*) characters")]
    public void WhenITryToUpdateTheBookingNotesToCharacters(int length)
    {
        var notes = new string('x', length);
        bookingContext.Result = bookingContext.Booking.UpdateNotes(notes);
    }

    [Then(@"the booking creation should fail")]
    public void ThenTheBookingCreationShouldFail()
    {
        var result = (Result<Booking>)bookingContext.Result;
        Assert.False(result.IsSuccess);
    }

    [Then(@"the booking total price should be (.*)")]
    public void ThenTheBookingTotalPriceShouldBe(decimal expectedPrice)
    {
        Assert.Equal(expectedPrice, bookingContext.Booking.TotalPrice);
    }

    [Then(@"the error should be for field ""(.*)""")]
    public void ThenTheErrorShouldBeForField(string fieldName)
    {
        switch (bookingContext.Result)
        {
            case Result<Booking> typedResult:
                Assert.False(typedResult.IsSuccess);
                Assert.Equal(ResultStatus.Invalid, typedResult.Status);
                Assert.Contains(fieldName, typedResult.ErrorDetails!.ValidationErrors!.Keys);
                break;
            case Result result:
                Assert.False(result.IsSuccess);
                Assert.Equal(ResultStatus.Invalid, result.Status);
                Assert.Contains(fieldName, result.ErrorDetails!.ValidationErrors!.Keys);
                break;
        }
    }

    [Then(@"the status transition should fail")]
    public void ThenTheStatusTransitionShouldFail()
    {
        var result = (Result)bookingContext.Result;
        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Conflict, result.Status);
    }

    [Then(@"the error should mention ""(.*)"" and ""(.*)""")]
    public void ThenTheErrorShouldMentionAnd(string text1, string text2)
    {
        var result = (Result)bookingContext.Result;
        Assert.NotNull(result.ErrorDetails);
        Assert.Contains(text1, result.ErrorDetails.Detail, StringComparison.Ordinal);
        Assert.Contains(text2, result.ErrorDetails.Detail, StringComparison.Ordinal);
    }

    [Then(@"the notes update should fail")]
    public void ThenTheNotesUpdateShouldFail()
    {
        var result = (Result)bookingContext.Result;
        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Invalid, result.Status);
    }
}
