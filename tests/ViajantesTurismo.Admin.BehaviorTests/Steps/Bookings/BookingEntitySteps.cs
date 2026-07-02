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
        global::SharedKernel.Testing.Assertions.TestAssert.Equal(type, bookingContext.Booking.RoomType);
    }

    [Then(@"the booking update should fail with validation error for ""(.*)""")]
    public void ThenTheBookingUpdateShouldFailWithValidationErrorFor(string fieldName)
    {
        global::SharedKernel.Testing.Assertions.TestAssert.NotNull(bookingContext.BookingOperationResult);
        global::SharedKernel.Testing.Assertions.TestAssert.False(bookingContext.BookingOperationResult.Value.IsSuccess);
        global::SharedKernel.Testing.Assertions.TestAssert.Equal(ResultStatus.Invalid, bookingContext.BookingOperationResult.Value.Status);
        global::SharedKernel.Testing.Assertions.TestAssert.Contains(fieldName, bookingContext.BookingOperationResult.Value.ErrorDetails!.ValidationErrors!.Keys);
    }

    [Then("the booking creation should fail")]
    public void ThenTheBookingCreationShouldFail()
    {
        global::SharedKernel.Testing.Assertions.TestAssert.NotNull(bookingContext.BookingCreationResult);
        global::SharedKernel.Testing.Assertions.TestAssert.False(bookingContext.BookingCreationResult.Value.IsSuccess);
    }

    [Then("the booking total price should be (.*)")]
    public void ThenTheBookingTotalPriceShouldBe(decimal expectedPrice)
    {
        global::SharedKernel.Testing.Assertions.TestAssert.Equal(expectedPrice, bookingContext.Booking.TotalPrice);
    }

    [Then(@"the error should be for field ""(.*)""")]
    public void ThenTheErrorShouldBeForField(string fieldName)
    {
        // Check BookingCreationResult first, then BookingOperationResult, then BookingCustomerResult
        if (bookingContext.BookingCreationResult.HasValue)
        {
            global::SharedKernel.Testing.Assertions.TestAssert.False(bookingContext.BookingCreationResult.Value.IsSuccess);
            global::SharedKernel.Testing.Assertions.TestAssert.Equal(ResultStatus.Invalid, bookingContext.BookingCreationResult.Value.Status);
            global::SharedKernel.Testing.Assertions.TestAssert.Contains(fieldName, bookingContext.BookingCreationResult.Value.ErrorDetails!.ValidationErrors!.Keys);
        }
        else if (bookingContext.BookingOperationResult.HasValue)
        {
            global::SharedKernel.Testing.Assertions.TestAssert.False(bookingContext.BookingOperationResult.Value.IsSuccess);
            global::SharedKernel.Testing.Assertions.TestAssert.Equal(ResultStatus.Invalid, bookingContext.BookingOperationResult.Value.Status);
            global::SharedKernel.Testing.Assertions.TestAssert.Contains(fieldName, bookingContext.BookingOperationResult.Value.ErrorDetails!.ValidationErrors!.Keys);
        }
        else if (bookingContext.BookingCustomerResult.HasValue)
        {
            global::SharedKernel.Testing.Assertions.TestAssert.False(bookingContext.BookingCustomerResult.Value.IsSuccess);
            global::SharedKernel.Testing.Assertions.TestAssert.Equal(ResultStatus.Invalid, bookingContext.BookingCustomerResult.Value.Status);
            global::SharedKernel.Testing.Assertions.TestAssert.Contains(fieldName, bookingContext.BookingCustomerResult.Value.ErrorDetails!.ValidationErrors!.Keys);
        }
        else
        {
            global::SharedKernel.Testing.Assertions.TestAssert.Fail("No booking result found.");
        }
    }
}
