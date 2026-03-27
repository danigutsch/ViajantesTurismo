namespace ViajantesTurismo.Admin.BehaviorTests.Steps.Bookings;

[Binding]
public sealed class BookingEntitySteps(BookingContext bookingContext)
{
    private static Result<Booking> CreateBookingWithNotes(int length)
    {
        var principal = CreatePrincipalCustomer();
        var notes = new string('x', length);
        return Booking.Create(
            Guid.CreateVersion7(),
            1000m,
            new BookingRoom(RoomType.SingleOccupancy, 0m),
            principal,
            null,
            Discount.Create(DiscountType.None, 0m, null).Value,
            notes);
    }

    private static BookingCustomer CreatePrincipalCustomer(decimal bikePrice = 100m, BikeType bikeType = BikeType.Regular)
    {
        var result = BookingCustomer.Create(Guid.CreateVersion7(), bikeType, bikePrice);
        return result.Value;
    }

    private static BookingCustomer CreateCompanionCustomer(decimal bikePrice = 200m, BikeType bikeType = BikeType.EBike)
    {
        var result = BookingCustomer.Create(Guid.CreateVersion7(), bikeType, bikePrice);
        return result.Value;
    }

    [When(@"I create a booking with base price (.*), room type ""(.*)"", room cost (.*), and regular bike (.*) for principal")]
    public void WhenICreateABookingWithBasePriceRoomTypeRoomCostAndRegularBikeForPrincipal(decimal basePrice, string roomType, decimal roomCost, decimal bikePrice)
    {
        var principal = CreatePrincipalCustomer(bikePrice);
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
        var principal = CreatePrincipalCustomer(principalBikePrice);
        var companion = CreateCompanionCustomer(companionBikePrice);
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
        var principal = CreatePrincipalCustomer();
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
        var principal = CreatePrincipalCustomer();
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
        bookingContext.BookingCreationResult = CreateBookingWithNotes(length);
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
        var principal = CreatePrincipalCustomer();
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
        var principal = CreatePrincipalCustomer();
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
        Assert.Equal(type, bookingContext.Booking.RoomType);
    }

    [Then(@"the booking update should fail with validation error for ""(.*)""")]
    public void ThenTheBookingUpdateShouldFailWithValidationErrorFor(string fieldName)
    {
        Assert.NotNull(bookingContext.BookingOperationResult);
        Assert.False(bookingContext.BookingOperationResult.Value.IsSuccess);
        Assert.Equal(ResultStatus.Invalid, bookingContext.BookingOperationResult.Value.Status);
        Assert.Contains(fieldName, bookingContext.BookingOperationResult.Value.ErrorDetails!.ValidationErrors!.Keys);
    }

    [Then("the booking creation should fail")]
    public void ThenTheBookingCreationShouldFail()
    {
        Assert.NotNull(bookingContext.BookingCreationResult);
        Assert.False(bookingContext.BookingCreationResult.Value.IsSuccess);
    }

    [Then("the booking total price should be (.*)")]
    public void ThenTheBookingTotalPriceShouldBe(decimal expectedPrice)
    {
        Assert.Equal(expectedPrice, bookingContext.Booking.TotalPrice);
    }

    [Then(@"the error should be for field ""(.*)""")]
    public void ThenTheErrorShouldBeForField(string fieldName)
    {
        // Check BookingCreationResult first, then BookingOperationResult, then BookingCustomerResult
        if (bookingContext.BookingCreationResult.HasValue)
        {
            Assert.False(bookingContext.BookingCreationResult.Value.IsSuccess);
            Assert.Equal(ResultStatus.Invalid, bookingContext.BookingCreationResult.Value.Status);
            Assert.Contains(fieldName, bookingContext.BookingCreationResult.Value.ErrorDetails!.ValidationErrors!.Keys);
        }
        else if (bookingContext.BookingOperationResult.HasValue)
        {
            Assert.False(bookingContext.BookingOperationResult.Value.IsSuccess);
            Assert.Equal(ResultStatus.Invalid, bookingContext.BookingOperationResult.Value.Status);
            Assert.Contains(fieldName, bookingContext.BookingOperationResult.Value.ErrorDetails!.ValidationErrors!.Keys);
        }
        else if (bookingContext.BookingCustomerResult.HasValue)
        {
            Assert.False(bookingContext.BookingCustomerResult.Value.IsSuccess);
            Assert.Equal(ResultStatus.Invalid, bookingContext.BookingCustomerResult.Value.Status);
            Assert.Contains(fieldName, bookingContext.BookingCustomerResult.Value.ErrorDetails!.ValidationErrors!.Keys);
        }
        else
        {
            Assert.Fail("No booking result found.");
        }
    }
}
