using ViajantesTurismo.Admin.Domain.Shared;

namespace ViajantesTurismo.Admin.BehaviorTests.Steps.Bookings;

[Binding]
public sealed class BookingEntitySteps(BookingContext bookingContext)
{
    [When(@"I create a booking with base price (.*), room type ""(.*)"", room cost (.*), and regular bike (.*) for principal")]
    public void WhenICreateABookingWithBasePriceRoomTypeRoomCostAndRegularBikeForPrincipal(decimal basePrice, string roomType, decimal roomCost, decimal bikePrice)
    {
        var principal = BookingStepDataFactory.CreatePrincipalCustomer(bikePrice);
        var room = Enum.Parse<RoomType>(roomType);
        var result = Booking.Create(
            Guid.CreateVersion7(),
            basePrice,
            new BookingRoom(room, roomCost),
            principal,
            null,
            Discount.Create(DiscountType.None, 0m, null).Value,
            null);

        bookingContext.BookingCreationResult = result;
        bookingContext.Action = null!;
    }

    [When(@"I create a booking with base price (.*), room type ""(.*)"", room cost (.*), regular bike (.*) for principal, and eBike (.*) for companion")]
    public void WhenICreateABookingWithBasePriceRoomTypeRoomCostRegularBikeForPrincipalAndEBikeForCompanion(decimal basePrice, string roomType, decimal roomCost, decimal principalBikePrice,
        decimal companionBikePrice)
    {
        var principal = BookingStepDataFactory.CreatePrincipalCustomer(principalBikePrice);
        var companion = BookingStepDataFactory.CreateCompanionCustomer(companionBikePrice);
        var room = Enum.Parse<RoomType>(roomType);
        var result = Booking.Create(
            Guid.CreateVersion7(),
            basePrice,
            new BookingRoom(room, roomCost),
            principal,
            companion,
            Discount.Create(DiscountType.None, 0m, null).Value,
            null);

        bookingContext.BookingCreationResult = result;
        bookingContext.Action = null!;
    }

    [When("I try to create a booking with base price (.*)")]
    public void WhenITryToCreateABookingWithBasePrice(decimal basePrice)
    {
        var principal = BookingStepDataFactory.CreatePrincipalCustomer();
        var result = Booking.Create(
            Guid.CreateVersion7(),
            basePrice,
            new BookingRoom(RoomType.SingleOccupancy, 0m),
            principal,
            null,
            Discount.Create(
                DiscountType.None,
                0m,
                null).Value,
            null);

        bookingContext.BookingCreationResult = result;
        bookingContext.Action = null!;
    }

    [When("I try to create a booking with base price (.*) and room cost (.*)")]
    public void WhenITryToCreateABookingWithBasePriceAndRoomCost(decimal basePrice, decimal roomCost)
    {
        var principal = BookingStepDataFactory.CreatePrincipalCustomer();
        var result = Booking.Create(
            Guid.CreateVersion7(),
            basePrice,
            new BookingRoom(RoomType.DoubleOccupancy, roomCost),
            principal,
            null,
            Discount.Create(DiscountType.None, 0m, null).Value,
            null);

        bookingContext.BookingCreationResult = result;
        bookingContext.Action = null!;
    }

    [When("I try to create a booking with notes of (.*) characters")]
    public void WhenITryToCreateABookingWithNotesOfCharacters(int length)
    {
        bookingContext.BookingCreationResult = BookingStepDataFactory.CreateBookingWithNotes(length);
        bookingContext.Action = null!;
    }

    [When("I create a booking with notes of (.*) characters")]
    public void WhenICreateABookingWithNotesOfCharacters(int length)
    {
        WhenITryToCreateABookingWithNotesOfCharacters(length);
    }

    [When(@"I create a booking with notes ""(.*)""")]
    public void WhenICreateABookingWithNotes(string notes)
    {
        var principal = BookingStepDataFactory.CreatePrincipalCustomer();
        var result = Booking.Create(Guid.CreateVersion7(),
            1000m,
            new BookingRoom(RoomType.SingleOccupancy, 0m),
            principal,
            null,
            Discount.Create(DiscountType.None, 0m, null).Value,
            notes);

        bookingContext.BookingCreationResult = result;
        bookingContext.Action = null!;
    }

    [When(@"I try to create a booking with invalid room type (-?\d+)")]
    public void WhenITryToCreateABookingWithInvalidRoomTypeD(int invalidRoomType)
    {
        var principal = BookingStepDataFactory.CreatePrincipalCustomer();
        var result = Booking.Create(
            Guid.CreateVersion7(),
            1000m,
            new BookingRoom((RoomType)invalidRoomType, 0m),
            principal,
            null,
            Discount.Create(DiscountType.None, 0m, null).Value,
            null);

        bookingContext.BookingCreationResult = result;
        bookingContext.Action = null!;
    }

    [Then(@"the booking should have room type ""(.*)""")]
    public void ThenTheBookingShouldHaveRoomType(string expectedRoomType)
    {
        var type = Enum.Parse<RoomType>(expectedRoomType);
        (bookingContext.Booking.RoomType).ShouldBe(type);
    }

    [Then(@"the booking update should fail with validation error for ""(.*)""")]
    public void ThenTheBookingUpdateShouldFailWithValidationErrorFor(string fieldName)
    {
        (bookingContext.BookingOperationResult).ShouldNotBeNull();
        (bookingContext.BookingOperationResult.Value.IsSuccess).ShouldBeFalse();
        (bookingContext.BookingOperationResult.Value.Status).ShouldBe(ResultStatus.Invalid);
        var errorDetails = (bookingContext.BookingOperationResult.Value.ErrorDetails).ShouldNotBeNull();
        var validationErrors = (errorDetails.ValidationErrors).ShouldNotBeNull();
        (validationErrors.Keys).ShouldContain(fieldName);
    }

    [Then("the booking creation should fail")]
    public void ThenTheBookingCreationShouldFail()
    {
        (bookingContext.BookingCreationResult).ShouldNotBeNull();
        (bookingContext.BookingCreationResult.Value.IsSuccess).ShouldBeFalse();
    }

    [Then("the booking total price should be (.*)")]
    public void ThenTheBookingTotalPriceShouldBe(decimal expectedPrice)
    {
        (bookingContext.Booking.TotalPrice).ShouldBe(expectedPrice);
    }

    [Then(@"the error should be for field ""(.*)""")]
    public void ThenTheErrorShouldBeForField(string fieldName)
    {
        // Check BookingCreationResult first, then BookingOperationResult, then BookingCustomerResult
        if (bookingContext.BookingCreationResult.HasValue)
        {
            (bookingContext.BookingCreationResult.Value.IsSuccess).ShouldBeFalse();
            (bookingContext.BookingCreationResult.Value.Status).ShouldBe(ResultStatus.Invalid);
            var errorDetails = (bookingContext.BookingCreationResult.Value.ErrorDetails).ShouldNotBeNull();
            var validationErrors = (errorDetails.ValidationErrors).ShouldNotBeNull();
            (validationErrors.Keys).ShouldContain(fieldName);
        }
        else if (bookingContext.BookingOperationResult.HasValue)
        {
            (bookingContext.BookingOperationResult.Value.IsSuccess).ShouldBeFalse();
            (bookingContext.BookingOperationResult.Value.Status).ShouldBe(ResultStatus.Invalid);
            var errorDetails = (bookingContext.BookingOperationResult.Value.ErrorDetails).ShouldNotBeNull();
            var validationErrors = (errorDetails.ValidationErrors).ShouldNotBeNull();
            (validationErrors.Keys).ShouldContain(fieldName);
        }
        else if (bookingContext.BookingCustomerResult.HasValue)
        {
            (bookingContext.BookingCustomerResult.Value.IsSuccess).ShouldBeFalse();
            (bookingContext.BookingCustomerResult.Value.Status).ShouldBe(ResultStatus.Invalid);
            var errorDetails = (bookingContext.BookingCustomerResult.Value.ErrorDetails).ShouldNotBeNull();
            var validationErrors = (errorDetails.ValidationErrors).ShouldNotBeNull();
            (validationErrors.Keys).ShouldContain(fieldName);
        }
        else
        {
            false.ShouldBeTrue("No booking result found.");
        }
    }
}
