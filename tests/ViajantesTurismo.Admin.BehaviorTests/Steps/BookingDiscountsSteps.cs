using Reqnroll;
using ViajantesTurismo.Admin.BehaviorTests.Context;
using ViajantesTurismo.Admin.Domain.Customers;
using ViajantesTurismo.Admin.Domain.Tours;
using ViajantesTurismo.Common.Monies;
using ViajantesTurismo.Common.Results;

namespace ViajantesTurismo.Admin.BehaviorTests.Steps;

[Binding]
public class BookingDiscountsSteps(TourContext tourContext, BookingContext bookingContext)
{
    [Given(
        @"a tour exists with base price (\d+), double room supplement (\d+), regular bike price (\d+), and e-bike price (\d+)")]
    public void GivenATourExistsWithPricing(decimal basePrice, decimal doubleRoomSupplement, decimal regularBikePrice,
        decimal eBikePrice)
    {
        tourContext.Tour = Tour.Create(
            identifier: "TEST2024",
            name: "Test Tour",
            startDate: DateTime.UtcNow.AddMonths(1),
            endDate: DateTime.UtcNow.AddMonths(1).AddDays(7),
            basePrice: basePrice,
            doubleRoomSupplementPrice: doubleRoomSupplement,
            regularBikePrice: regularBikePrice,
            eBikePrice: eBikePrice,
            currency: Currency.UsDollar,
            minCustomers: 4,
            maxCustomers: 12,
            includedServices: ["Hotel", "Breakfast"]).Value;
    }

    [When(@"I create a booking with principal customer (\d+), regular bike, single room, and no discount")]
    public void WhenICreateABookingWithNoDiscount(int principalCustomerId)
    {
        bookingContext.Result = tourContext.Tour.AddBooking(
            principalCustomerId,
            BikeType.Regular,
            null,
            null,
            RoomType.SingleRoom,
            DiscountType.None,
            0m,
            null,
            null);
    }

    [When(
        @"I create a booking with principal customer (\d+), regular bike, single room, and (\d+(?:\.\d+)?)% discount")]
    public void WhenICreateABookingWithPercentageDiscount(int principalCustomerId, decimal discountPercentage)
    {
        bookingContext.Result = tourContext.Tour.AddBooking(
            principalCustomerId,
            BikeType.Regular,
            null,
            null,
            RoomType.SingleRoom,
            DiscountType.Percentage,
            discountPercentage,
            null,
            null);
    }

    [When(@"I create a booking with principal customer (\d+), regular bike, double room, and (\d+) absolute discount")]
    public void WhenICreateABookingWithAbsoluteDiscount(int principalCustomerId, decimal discountAmount)
    {
        bookingContext.Result = tourContext.Tour.AddBooking(
            principalCustomerId,
            BikeType.Regular,
            null,
            null,
            RoomType.DoubleRoom,
            DiscountType.Absolute,
            discountAmount,
            null,
            null);
    }

    [When(
        @"I create a booking with principal customer (\d+), regular bike, single room, (\d+)% discount, and reason ""(.*)""")]
    public void WhenICreateABookingWithDiscountAndReason(int principalCustomerId, decimal discountPercentage,
        string reason)
    {
        bookingContext.Result = tourContext.Tour.AddBooking(
            principalCustomerId,
            BikeType.Regular,
            null,
            null,
            RoomType.SingleRoom,
            DiscountType.Percentage,
            discountPercentage,
            reason,
            null);
    }

    [When(
        @"I create a booking with principal customer (\d+) regular bike, companion customer (\d+) e-bike, double room, and (\d+)% discount")]
    public void WhenICreateABookingWithCompanionAndDiscount(int principalCustomerId, int companionCustomerId,
        decimal discountPercentage)
    {
        bookingContext.Result = tourContext.Tour.AddBooking(
            principalCustomerId,
            BikeType.Regular,
            companionCustomerId,
            BikeType.EBike,
            RoomType.DoubleRoom,
            DiscountType.Percentage,
            discountPercentage,
            null,
            null);
    }


    [When(@"I create a booking with principal customer (\d+), regular bike, single room, and -(\d+)% discount")]
    public void WhenICreateABookingWithNegativeDiscount(int principalCustomerId, decimal discountPercentage)
    {
        bookingContext.Result = tourContext.Tour.AddBooking(
            principalCustomerId,
            BikeType.Regular,
            null,
            null,
            RoomType.SingleRoom,
            DiscountType.Percentage,
            -discountPercentage,
            null,
            null);
    }

    [When(@"I create a booking with principal customer (\d+), regular bike, single room, and (\d+) absolute discount")]
    public void WhenICreateABookingWithInvalidAbsoluteDiscount(int principalCustomerId, decimal discountAmount)
    {
        bookingContext.Result = tourContext.Tour.AddBooking(
            principalCustomerId,
            BikeType.Regular,
            null,
            null,
            RoomType.SingleRoom,
            DiscountType.Absolute,
            discountAmount,
            null,
            null);
    }


    [When(
        @"I create a booking with base price (\d+), room cost (\d+), principal bike (\d+), companion bike (\d+), and (\d+)% discount")]
    public void WhenICreateABookingWithSpecificPricing(decimal basePrice, decimal roomCost, decimal bike1,
        decimal bike2, decimal discount)
    {
        var roomType = bike2 > 0 ? RoomType.DoubleRoom : RoomType.SingleRoom;
        var companionId = bike2 > 0 ? (int?)2 : null;
        var companionBikeType = bike2 > 0 ? (BikeType?)BikeType.EBike : null;

        bookingContext.Result = tourContext.Tour.AddBooking(
            1,
            BikeType.Regular,
            companionId,
            companionBikeType,
            roomType,
            DiscountType.Percentage,
            discount,
            null,
            null);
    }

    [Then(@"the booking total price should be approximately (\d+\.\d+)")]
    public void ThenTheBookingTotalPriceShouldBeApproximately(decimal expectedPrice)
    {
        var result = (Result<Booking>)bookingContext.Result;
        Assert.True(result.IsSuccess);
        var booking = result.Value;
        Assert.True(Math.Abs(booking.TotalPrice - expectedPrice) < 0.1m,
            $"Expected approximately {expectedPrice}, but got {booking.TotalPrice}");
    }

    [Then(@"the booking should have discount reason ""(.*)""")]
    public void ThenTheBookingShouldHaveDiscountReason(string expectedReason)
    {
        var result = (Result<Booking>)bookingContext.Result;
        Assert.True(result.IsSuccess);
        var booking = result.Value;
        Assert.NotNull(booking.Discount);
        Assert.Equal(expectedReason, booking.Discount.Reason);
    }

    [Then(@"the booking should fail with error containing ""(.*)""")]
    public void ThenTheBookingShouldFailWithErrorContaining(string expectedErrorText)
    {
        var result = (Result<Booking>)bookingContext.Result;
        Assert.True(result.IsFailure);
        Assert.Contains(expectedErrorText, result.ErrorDetails.Detail, StringComparison.OrdinalIgnoreCase);
    }

    [When(@"I attempt to apply a (-?\d+(?:\.\d+)?)% discount to a booking")]
    public void WhenIAttemptToApplyPercentageDiscount(decimal discountPercentage)
    {
        WhenICreateABookingWithPercentageDiscount(1, discountPercentage);
    }

    [When(@"I attempt to apply a (\d+) absolute discount to a (\d+) booking")]
    public void WhenIAttemptToApplyAbsoluteDiscountToBooking(decimal discountAmount, decimal bookingAmount)
    {
        WhenICreateABookingWithInvalidAbsoluteDiscount(1, discountAmount);
    }

    [Then("I should be informed that the final price must be greater than zero")]
    public void ThenIShouldBeInformedThatTheFinalPriceMustBeGreaterThanZero()
    {
        ThenTheBookingShouldFailWithErrorContaining("final price");
    }

    [Then("I should be informed that discounts cannot be negative")]
    public void ThenIShouldBeInformedThatDiscountsCannotBeNegative()
    {
        ThenTheBookingShouldFailWithErrorContaining("negative");
    }

    [Then("I should be informed that the discount cannot exceed the subtotal")]
    public void ThenIShouldBeInformedThatTheDiscountCannotExceedTheSubtotal()
    {
        ThenTheBookingShouldFailWithErrorContaining("exceed");
    }

    [Then(@"I should be informed that percentage discounts cannot exceed (\d+)%")]
    public void ThenIShouldBeInformedThatPercentageDiscountsCannotExceed(int maxPercentage)
    {
        var result = (Result<Booking>)bookingContext.Result;
        Assert.True(result.IsFailure);
        Assert.Contains("percentage", result.ErrorDetails.Detail, StringComparison.OrdinalIgnoreCase);
    }
}
