using ViajantesTurismo.Admin.Domain.Shared;

namespace ViajantesTurismo.Admin.BehaviorTests.Steps.Bookings;

[Binding]
public class CompanionBookingsSteps(TourContext tourContext, BookingContext bookingContext)
{
    [Given("a principal customer exists")]
    public static void GivenAPrincipalCustomerExists()
    {
        TestAssert.True(true);
    }

    [Given("a companion customer exists")]
    public static void GivenACompanionCustomerExists()
    {
        TestAssert.True(true);
    }

    [When(
        @"I add a booking with principal customer (\d+) on regular bike and companion customer (\d+) on regular bike in double room")]
    public void WhenIAddABookingWithPrincipalCustomerDOnRegularBikeAndCompanionCustomerDOnRegularBikeInDoubleRoom(int principalId, int companionId)
    {
        var principalGuid = Guid.CreateVersion7();
        var companionGuid = principalId == companionId ? principalGuid : Guid.CreateVersion7();

        bookingContext.BookingCreationResult = tourContext.Tour.AddBooking(new TourBookingRequest(
            principalGuid,
            BikeType.Regular,
            RoomType.DoubleOccupancy,
            DiscountType.None,
            companionGuid,
            BikeType.Regular));
    }

    [When(@"I add a booking with principal customer (\d+) on regular bike and companion customer (\d+) on e-bike in double room")]
    public void WhenIAddABookingWithPrincipalCustomerDOnRegularBikeAndCompanionCustomerDOnEBikeInDoubleRoom(int principalId, int companionId)
    {
        bookingContext.BookingCreationResult = tourContext.Tour.AddBooking(new TourBookingRequest(
            Guid.CreateVersion7(),
            BikeType.Regular,
            RoomType.DoubleOccupancy,
            DiscountType.None,
            Guid.CreateVersion7(),
            BikeType.EBike));
    }

    [When(@"I add a booking with principal customer (\d+) on e-bike and companion customer (\d+) on e-bike in double room")]
    public void WhenIAddABookingWithPrincipalCustomerDOnEBikeAndCompanionCustomerDOnEBikeInDoubleRoom(int principalId, int companionId)
    {
        bookingContext.BookingCreationResult = tourContext.Tour.AddBooking(new TourBookingRequest(
            Guid.CreateVersion7(),
            BikeType.EBike,
            RoomType.DoubleOccupancy,
            DiscountType.None,
            Guid.CreateVersion7(),
            BikeType.EBike));
    }

    [When(@"I add a booking with principal customer (\d+) on regular bike without companion in single room")]
    public void WhenIAddABookingWithPrincipalCustomerDOnRegularBikeWithoutCompanionInSingleRoom(int principalId)
    {
        bookingContext.BookingCreationResult = tourContext.Tour.AddBooking(new TourBookingRequest(
            Guid.CreateVersion7(),
            BikeType.Regular,
            RoomType.SingleOccupancy,
            DiscountType.None));
    }

    [When(@"I add a booking with principal customer (\d+) on regular bike and companion customer (\d+) with no bike type in double room")]
    public void WhenIAddABookingWithPrincipalCustomerDOnRegularBikeAndCompanionCustomerDWithNoBikeTypeInDoubleRoom(int principalId, int companionId)
    {
        bookingContext.BookingCreationResult = tourContext.Tour.AddBooking(new TourBookingRequest(
            Guid.CreateVersion7(),
            BikeType.Regular,
            RoomType.DoubleOccupancy,
            DiscountType.None,
            Guid.CreateVersion7()));
    }

    [When(@"I add a booking with principal customer (\d+) with no bike type without companion in single room")]
    public void WhenIAddABookingWithPrincipalCustomerDWithNoBikeTypeWithoutCompanionInSingleRoom(int principalId)
    {
        bookingContext.BookingCreationResult = tourContext.Tour.AddBooking(new TourBookingRequest(
            Guid.CreateVersion7(),
            BikeType.None,
            RoomType.SingleOccupancy,
            DiscountType.None));
    }

    [Then("the booking should have a companion customer")]
    public void ThenTheBookingShouldHaveACompanionCustomer()
    {
        TestAssert.NotNull(bookingContext.BookingCreationResult);
        TestAssert.True(bookingContext.BookingCreationResult.Value.IsSuccess);
        TestAssert.NotNull(bookingContext.BookingCreationResult.Value.Value.CompanionCustomer);
    }

    [Then("the booking should not have a companion customer")]
    public void ThenTheBookingShouldNotHaveACompanionCustomer()
    {
        TestAssert.NotNull(bookingContext.BookingCreationResult);
        TestAssert.True(bookingContext.BookingCreationResult.Value.IsSuccess);
        TestAssert.Null(bookingContext.BookingCreationResult.Value.Value.CompanionCustomer);
    }

    [Then("the booking should include principal bike price")]
    public void ThenTheBookingShouldIncludePrincipalBikePrice()
    {
        TestAssert.NotNull(bookingContext.BookingCreationResult);
        TestAssert.True(bookingContext.BookingCreationResult.Value.IsSuccess);
        var booking = bookingContext.BookingCreationResult.Value.Value;
        var tour = tourContext.Tour;
        var expectedBikePrice = tour.Pricing.RegularBikePrice;
        TestAssert.Equal(expectedBikePrice, booking.PrincipalCustomer.BikePrice);
    }

    [Then("the booking should include companion bike price")]
    public void ThenTheBookingShouldIncludeCompanionBikePrice()
    {
        TestAssert.NotNull(bookingContext.BookingCreationResult);
        TestAssert.True(bookingContext.BookingCreationResult.Value.IsSuccess);
        var booking = bookingContext.BookingCreationResult.Value.Value;
        TestAssert.NotNull(booking.CompanionCustomer);
        var tour = tourContext.Tour;
        var expectedBikePrice = tour.Pricing.RegularBikePrice;
        TestAssert.Equal(expectedBikePrice, booking.CompanionCustomer.BikePrice);
    }

    [Then("the booking should include principal regular bike price")]
    public void ThenTheBookingShouldIncludePrincipalRegularBikePrice()
    {
        TestAssert.NotNull(bookingContext.BookingCreationResult);
        TestAssert.True(bookingContext.BookingCreationResult.Value.IsSuccess);
        var booking = bookingContext.BookingCreationResult.Value.Value;
        var tour = tourContext.Tour;
        TestAssert.Equal(tour.Pricing.RegularBikePrice, booking.PrincipalCustomer.BikePrice);
    }

    [Then("the booking should include companion e-bike price")]
    public void ThenTheBookingShouldIncludeCompanionEBikePrice()
    {
        TestAssert.NotNull(bookingContext.BookingCreationResult);
        TestAssert.True(bookingContext.BookingCreationResult.Value.IsSuccess);
        var booking = bookingContext.BookingCreationResult.Value.Value;
        TestAssert.NotNull(booking.CompanionCustomer);
        var tour = tourContext.Tour;
        TestAssert.Equal(tour.Pricing.EBikePrice, booking.CompanionCustomer.BikePrice);
    }

    [Then("both customers should have e-bike pricing")]
    public void ThenBothCustomersShouldHaveEBikePricing()
    {
        TestAssert.NotNull(bookingContext.BookingCreationResult);
        TestAssert.True(bookingContext.BookingCreationResult.Value.IsSuccess);
        var booking = bookingContext.BookingCreationResult.Value.Value;
        TestAssert.NotNull(booking.CompanionCustomer);
        var tour = tourContext.Tour;
        TestAssert.Equal(tour.Pricing.EBikePrice, booking.PrincipalCustomer.BikePrice);
        TestAssert.Equal(tour.Pricing.EBikePrice, booking.CompanionCustomer.BikePrice);
    }

    [Then("the booking should include single room supplement")]
    public void ThenTheBookingShouldIncludeSingleRoomSupplement()
    {
        TestAssert.NotNull(bookingContext.BookingCreationResult);
        TestAssert.True(bookingContext.BookingCreationResult.Value.IsSuccess);
        var booking = bookingContext.BookingCreationResult.Value.Value;
        var tour = tourContext.Tour;
        TestAssert.Equal(tour.Pricing.SingleRoomSupplementPrice, booking.RoomAdditionalCost);
    }

    [Then("the booking should not include single room supplement")]
    public void ThenTheBookingShouldNotIncludeSingleRoomSupplement()
    {
        TestAssert.NotNull(bookingContext.BookingCreationResult);
        TestAssert.True(bookingContext.BookingCreationResult.Value.IsSuccess);
        var booking = bookingContext.BookingCreationResult.Value.Value;
        TestAssert.Equal(0m, booking.RoomAdditionalCost);
    }
}
