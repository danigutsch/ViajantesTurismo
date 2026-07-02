using ViajantesTurismo.Admin.Domain.Shared;

namespace ViajantesTurismo.Admin.BehaviorTests.Steps.Bookings;

[Binding]
public class CompanionBookingsSteps(TourContext tourContext, BookingContext bookingContext)
{
    [Given("a principal customer exists")]
    public static void GivenAPrincipalCustomerExists()
    {
        (true).ShouldBeTrue();
    }

    [Given("a companion customer exists")]
    public static void GivenACompanionCustomerExists()
    {
        (true).ShouldBeTrue();
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
        (bookingContext.BookingCreationResult).ShouldNotBeNull();
        (bookingContext.BookingCreationResult.Value.IsSuccess).ShouldBeTrue();
        (bookingContext.BookingCreationResult.Value.Value.CompanionCustomer).ShouldNotBeNull();
    }

    [Then("the booking should not have a companion customer")]
    public void ThenTheBookingShouldNotHaveACompanionCustomer()
    {
        (bookingContext.BookingCreationResult).ShouldNotBeNull();
        (bookingContext.BookingCreationResult.Value.IsSuccess).ShouldBeTrue();
        (bookingContext.BookingCreationResult.Value.Value.CompanionCustomer).ShouldBeNull();
    }

    [Then("the booking should include principal bike price")]
    public void ThenTheBookingShouldIncludePrincipalBikePrice()
    {
        (bookingContext.BookingCreationResult).ShouldNotBeNull();
        (bookingContext.BookingCreationResult.Value.IsSuccess).ShouldBeTrue();
        var booking = bookingContext.BookingCreationResult.Value.Value;
        var tour = tourContext.Tour;
        var expectedBikePrice = tour.Pricing.RegularBikePrice;
        (booking.PrincipalCustomer.BikePrice).ShouldBe(expectedBikePrice);
    }

    [Then("the booking should include companion bike price")]
    public void ThenTheBookingShouldIncludeCompanionBikePrice()
    {
        (bookingContext.BookingCreationResult).ShouldNotBeNull();
        (bookingContext.BookingCreationResult.Value.IsSuccess).ShouldBeTrue();
        var booking = bookingContext.BookingCreationResult.Value.Value;
        (booking.CompanionCustomer).ShouldNotBeNull();
        var tour = tourContext.Tour;
        var expectedBikePrice = tour.Pricing.RegularBikePrice;
        (booking.CompanionCustomer.BikePrice).ShouldBe(expectedBikePrice);
    }

    [Then("the booking should include principal regular bike price")]
    public void ThenTheBookingShouldIncludePrincipalRegularBikePrice()
    {
        (bookingContext.BookingCreationResult).ShouldNotBeNull();
        (bookingContext.BookingCreationResult.Value.IsSuccess).ShouldBeTrue();
        var booking = bookingContext.BookingCreationResult.Value.Value;
        var tour = tourContext.Tour;
        (booking.PrincipalCustomer.BikePrice).ShouldBe(tour.Pricing.RegularBikePrice);
    }

    [Then("the booking should include companion e-bike price")]
    public void ThenTheBookingShouldIncludeCompanionEBikePrice()
    {
        (bookingContext.BookingCreationResult).ShouldNotBeNull();
        (bookingContext.BookingCreationResult.Value.IsSuccess).ShouldBeTrue();
        var booking = bookingContext.BookingCreationResult.Value.Value;
        (booking.CompanionCustomer).ShouldNotBeNull();
        var tour = tourContext.Tour;
        (booking.CompanionCustomer.BikePrice).ShouldBe(tour.Pricing.EBikePrice);
    }

    [Then("both customers should have e-bike pricing")]
    public void ThenBothCustomersShouldHaveEBikePricing()
    {
        (bookingContext.BookingCreationResult).ShouldNotBeNull();
        (bookingContext.BookingCreationResult.Value.IsSuccess).ShouldBeTrue();
        var booking = bookingContext.BookingCreationResult.Value.Value;
        (booking.CompanionCustomer).ShouldNotBeNull();
        var tour = tourContext.Tour;
        (booking.PrincipalCustomer.BikePrice).ShouldBe(tour.Pricing.EBikePrice);
        (booking.CompanionCustomer.BikePrice).ShouldBe(tour.Pricing.EBikePrice);
    }

    [Then("the booking should include single room supplement")]
    public void ThenTheBookingShouldIncludeSingleRoomSupplement()
    {
        (bookingContext.BookingCreationResult).ShouldNotBeNull();
        (bookingContext.BookingCreationResult.Value.IsSuccess).ShouldBeTrue();
        var booking = bookingContext.BookingCreationResult.Value.Value;
        var tour = tourContext.Tour;
        (booking.RoomAdditionalCost).ShouldBe(tour.Pricing.SingleRoomSupplementPrice);
    }

    [Then("the booking should not include single room supplement")]
    public void ThenTheBookingShouldNotIncludeSingleRoomSupplement()
    {
        (bookingContext.BookingCreationResult).ShouldNotBeNull();
        (bookingContext.BookingCreationResult.Value.IsSuccess).ShouldBeTrue();
        var booking = bookingContext.BookingCreationResult.Value.Value;
        (booking.RoomAdditionalCost).ShouldBe(0m);
    }
}
