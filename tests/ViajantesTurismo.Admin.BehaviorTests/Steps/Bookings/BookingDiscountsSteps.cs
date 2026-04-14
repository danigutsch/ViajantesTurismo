using ViajantesTurismo.Admin.Contracts;
using ViajantesTurismo.Common.Monies;

using ViajantesTurismo.Admin.Domain.Shared;

namespace ViajantesTurismo.Admin.BehaviorTests.Steps.Bookings;

[Binding]
public class BookingDiscountsSteps(TourContext tourContext, BookingContext bookingContext)
{
    [Given(@"a tour exists with base price (\d+), single room supplement (\d+), regular bike price (\d+), and e-bike price (\d+)")]
    public void GivenATourExistsWithPricing(decimal basePrice, decimal singleRoomSupplement, decimal regularBikePrice, decimal eBikePrice)
    {
        tourContext.Tour = Tour.Create(new TourDefinition(
            "TEST2024",
            "Test Tour",
            DateTime.UtcNow.AddMonths(1),
            DateTime.UtcNow.AddMonths(1).AddDays(7),
            basePrice,
            singleRoomSupplement,
            regularBikePrice,
            eBikePrice,
            Currency.UsDollar,
            4,
            12,
            ["Hotel", "Breakfast"])).Value;
    }

    [When(@"I create a booking with principal customer (\d+), regular bike, single room, and no discount")]
    public void WhenICreateABookingWithNoDiscount(int principalCustomerId)
    {
        var result = tourContext.Tour.AddBooking(new TourBookingRequest(
            Guid.CreateVersion7(),
            BikeType.Regular,
            RoomType.DoubleOccupancy,
            DiscountType.None));

        bookingContext.BookingCreationResult = result;
    }

    [When(@"I create a booking with principal customer (\d+), regular bike, single room, and (\d+(?:\.\d+)?)% discount")]
    public void WhenICreateABookingWithPercentageDiscount(int principalCustomerId, decimal discountPercentage)
    {
        var result = tourContext.Tour.AddBooking(new TourBookingRequest(
            Guid.CreateVersion7(),
            BikeType.Regular,
            RoomType.DoubleOccupancy,
            DiscountType.Percentage,
            discountAmount: discountPercentage));

        bookingContext.BookingCreationResult = result;
    }

    [When(@"I create a booking with principal customer (\d+), regular bike, double room, and (\d+) absolute discount")]
    public void WhenICreateABookingWithAbsoluteDiscount(int principalCustomerId, decimal discountAmount)
    {
        var result = tourContext.Tour.AddBooking(new TourBookingRequest(
            Guid.CreateVersion7(),
            BikeType.Regular,
            RoomType.SingleOccupancy,
            DiscountType.Absolute,
            discountAmount: discountAmount));

        bookingContext.BookingCreationResult = result;
    }

    [When(@"I create a booking with principal customer (\d+), regular bike, single room, (\d+)% discount, and reason ""(.*)""")]
    public void WhenICreateABookingWithDiscountAndReason(int principalCustomerId, decimal discountPercentage, string reason)
    {
        var result = tourContext.Tour.AddBooking(new TourBookingRequest(
            Guid.CreateVersion7(),
            BikeType.Regular,
            RoomType.DoubleOccupancy,
            DiscountType.Percentage,
            discountAmount: discountPercentage,
            discountReason: reason));

        bookingContext.BookingCreationResult = result;
    }

    [When(@"I create a booking with principal customer (\d+) regular bike, companion customer (\d+) e-bike, double room, and (\d+)% discount")]
    public void WhenICreateABookingWithCompanionAndDiscount(int principalCustomerId, int companionCustomerId, decimal discountPercentage)
    {
        var result = tourContext.Tour.AddBooking(new TourBookingRequest(
            Guid.CreateVersion7(),
            BikeType.Regular,
            RoomType.DoubleOccupancy,
            DiscountType.Percentage,
            Guid.CreateVersion7(),
            BikeType.EBike,
            discountPercentage));

        bookingContext.BookingCreationResult = result;
    }


    [When(@"I create a booking with principal customer (\d+), regular bike, single room, and -(\d+)% discount")]
    public void WhenICreateABookingWithNegativeDiscount(int principalCustomerId, decimal discountPercentage)
    {
        var result = tourContext.Tour.AddBooking(new TourBookingRequest(
            Guid.CreateVersion7(),
            BikeType.Regular,
            RoomType.DoubleOccupancy,
            DiscountType.Percentage,
            discountAmount: -discountPercentage));

        bookingContext.BookingCreationResult = result;
    }

    [When(@"I create a booking with principal customer (\d+), regular bike, single room, and (\d+) absolute discount")]
    public void WhenICreateABookingWithInvalidAbsoluteDiscount(int principalCustomerId, decimal discountAmount)
    {
        var result = tourContext.Tour.AddBooking(new TourBookingRequest(
            Guid.CreateVersion7(),
            BikeType.Regular,
            RoomType.DoubleOccupancy,
            DiscountType.Absolute,
            discountAmount: discountAmount));

        bookingContext.BookingCreationResult = result;
    }


    [When(@"I create a booking with base price (\d+), room cost (\d+), principal bike (\d+), companion bike (\d+), and (\d+)% discount")]
    public void WhenICreateABookingWithSpecificPricing(decimal basePrice, decimal roomCost, decimal bike1, decimal bike2, decimal discount)
    {
        const RoomType roomType = RoomType.DoubleOccupancy;
        var companionId = bike2 > 0 ? (Guid?)Guid.CreateVersion7() : null;
        var companionBikeType = bike2 > 0 ? (BikeType?)BikeType.EBike : null;

        var result = tourContext.Tour.AddBooking(new TourBookingRequest(
            Guid.CreateVersion7(),
            BikeType.Regular,
            roomType,
            DiscountType.Percentage,
            companionId,
            companionBikeType,
            discount));

        bookingContext.BookingCreationResult = result;
    }

    [Then(@"the booking total price should be approximately (\d+\.\d+)")]
    public void ThenTheBookingTotalPriceShouldBeApproximately(decimal expectedPrice)
    {
        Assert.NotNull(bookingContext.BookingCreationResult);
        Assert.True(bookingContext.BookingCreationResult.Value.IsSuccess);
        var booking = bookingContext.BookingCreationResult.Value.Value;
        Assert.True(Math.Abs(booking.TotalPrice - expectedPrice) < 0.1m,
            $"Expected approximately {expectedPrice}, but got {booking.TotalPrice}");
    }

    [Then(@"the booking should have discount reason ""(.*)""")]
    public void ThenTheBookingShouldHaveDiscountReason(string expectedReason)
    {
        Assert.NotNull(bookingContext.BookingCreationResult);
        Assert.True(bookingContext.BookingCreationResult.Value.IsSuccess);
        var booking = bookingContext.BookingCreationResult.Value.Value;
        Assert.NotNull(booking.Discount);
        Assert.Equal(expectedReason, booking.Discount.Reason);
    }

    [Then(@"the booking should fail with error containing ""(.*)""")]
    public void ThenTheBookingShouldFailWithErrorContaining(string expectedErrorText)
    {
        Assert.NotNull(bookingContext.BookingCreationResult);
        Assert.True(bookingContext.BookingCreationResult.Value.IsFailure);
        Assert.Contains(expectedErrorText, bookingContext.BookingCreationResult.Value.ErrorDetails!.Detail, StringComparison.OrdinalIgnoreCase);
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
        Assert.NotNull(bookingContext.BookingCreationResult);
        Assert.True(bookingContext.BookingCreationResult.Value.IsFailure);
        Assert.Contains("percentage", bookingContext.BookingCreationResult.Value.ErrorDetails!.Detail, StringComparison.OrdinalIgnoreCase);
    }

    [When("I create a booking with principal customer 1, regular bike, single room, 15% discount, and a very long reason")]
    public void WhenICreateABookingWithLongReason()
    {
        var longReason = new string('a', ContractConstants.MaxDiscountReasonLength + 1);
        WhenICreateABookingWithDiscountAndReason(1, 15m, longReason);
    }

    [Then("I should be informed that the discount reason is too short")]
    public void ThenIShouldBeInformedThatTheDiscountReasonIsTooShort()
    {
        Assert.NotNull(bookingContext.BookingCreationResult);
        Assert.True(bookingContext.BookingCreationResult.Value.IsFailure);
        Assert.Contains("Reason must be at least", bookingContext.BookingCreationResult.Value.ErrorDetails.Detail, StringComparison.OrdinalIgnoreCase);
    }

    [Then("I should be informed that the discount reason is too long")]
    public void ThenIShouldBeInformedThatTheDiscountReasonIsTooLong()
    {
        Assert.NotNull(bookingContext.BookingCreationResult);
        Assert.True(bookingContext.BookingCreationResult.Value.IsFailure);
        Assert.Contains("Reason cannot exceed", bookingContext.BookingCreationResult.Value.ErrorDetails.Detail, StringComparison.OrdinalIgnoreCase);
    }
}
