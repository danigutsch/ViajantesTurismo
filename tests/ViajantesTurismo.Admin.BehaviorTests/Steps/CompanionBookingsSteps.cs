using Reqnroll;
using ViajantesTurismo.Admin.BehaviorTests.Context;
using ViajantesTurismo.Admin.Domain.Customers;
using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.Admin.BehaviorTests.Steps;

[Binding]
public class CompanionBookingsSteps(TourContext tourContext, BookingContext bookingContext)
{
    [Given(@"a principal customer exists")]
    public static void GivenAPrincipalCustomerExists()
    {
    }

    [Given(@"a companion customer exists")]
    public static void GivenACompanionCustomerExists()
    {
    }

    [When(@"I add a booking with principal customer (\d+) on regular bike and companion customer (\d+) on regular bike in double room")]
    public void WhenIAddABookingWithPrincipalCustomerDOnRegularBikeAndCompanionCustomerDOnRegularBikeInDoubleRoom(int principalId, int companionId)
    {
        var result = tourContext.Tour.AddBooking(
            principalId,
            BikeType.Regular,
            companionId,
            BikeType.Regular,
            RoomType.DoubleRoom,
            DiscountType.None,
            0m,
            null,
            null);

        if (result.IsSuccess)
        {
            bookingContext.Booking = result.Value;
        }
    }

    [When(@"I add a booking with principal customer (\d+) on regular bike and companion customer (\d+) on e-bike in double room")]
    public void WhenIAddABookingWithPrincipalCustomerDOnRegularBikeAndCompanionCustomerDOnEBikeInDoubleRoom(int principalId, int companionId)
    {
        var result = tourContext.Tour.AddBooking(
            principalId,
            BikeType.Regular,
            companionId,
            BikeType.EBike,
            RoomType.DoubleRoom,
            DiscountType.None,
            0m,
            null,
            null);

        if (result.IsSuccess)
        {
            bookingContext.Booking = result.Value;
        }
    }

    [When(@"I add a booking with principal customer (\d+) on e-bike and companion customer (\d+) on e-bike in double room")]
    public void WhenIAddABookingWithPrincipalCustomerDOnEBikeAndCompanionCustomerDOnEBikeInDoubleRoom(int principalId, int companionId)
    {
        var result = tourContext.Tour.AddBooking(
            principalId,
            BikeType.EBike,
            companionId,
            BikeType.EBike,
            RoomType.DoubleRoom,
            DiscountType.None,
            0m,
            null,
            null);

        if (result.IsSuccess)
        {
            bookingContext.Booking = result.Value;
        }
    }

    [When(@"I add a booking with principal customer (\d+) on regular bike without companion in single room")]
    public void WhenIAddABookingWithPrincipalCustomerDOnRegularBikeWithoutCompanionInSingleRoom(int principalId)
    {
        var result = tourContext.Tour.AddBooking(
            principalId,
            BikeType.Regular,
            null,
            null,
            RoomType.SingleRoom,
            DiscountType.None,
            0m,
            null,
            null);

        if (result.IsSuccess)
        {
            bookingContext.Booking = result.Value;
        }
    }

    [When(@"I add a booking with principal customer (\d+) on regular bike and companion customer (\d+) with no bike type in double room")]
    public void WhenIAddABookingWithPrincipalCustomerDOnRegularBikeAndCompanionCustomerDWithNoBikeTypeInDoubleRoom(int principalId, int companionId)
    {
        var result = tourContext.Tour.AddBooking(
            principalId,
            BikeType.Regular,
            companionId,
            null,
            RoomType.DoubleRoom,
            DiscountType.None,
            0m,
            null,
            null);

        bookingContext.Result = result;
        if (result.IsSuccess)
        {
            bookingContext.Booking = result.Value;
        }
    }

    [When(@"I add a booking with principal customer (\d+) with no bike type without companion in single room")]
    public void WhenIAddABookingWithPrincipalCustomerDWithNoBikeTypeWithoutCompanionInSingleRoom(int principalId)
    {
        var result = tourContext.Tour.AddBooking(
            principalId,
            BikeType.None,
            null,
            null,
            RoomType.SingleRoom,
            DiscountType.None,
            0m,
            null,
            null);

        bookingContext.Result = result;

        if (result.IsSuccess)
        {
            bookingContext.Booking = result.Value;
        }
    }

    [Then(@"the booking should have a companion customer")]
    public void ThenTheBookingShouldHaveACompanionCustomer()
    {
        Assert.NotNull(bookingContext.Booking);
        Assert.NotNull(bookingContext.Booking.CompanionCustomer);
    }

    [Then(@"the booking should not have a companion customer")]
    public void ThenTheBookingShouldNotHaveACompanionCustomer()
    {
        Assert.NotNull(bookingContext.Booking);
        Assert.Null(bookingContext.Booking.CompanionCustomer);
    }

    [Then(@"the booking should include principal bike price")]
    public void ThenTheBookingShouldIncludePrincipalBikePrice()
    {
        Assert.NotNull(bookingContext.Booking);
        var tour = tourContext.Tour;
        var expectedBikePrice = tour.RegularBikePrice;
        Assert.Equal(expectedBikePrice, bookingContext.Booking.PrincipalCustomer.BikePrice);
    }

    [Then(@"the booking should include companion bike price")]
    public void ThenTheBookingShouldIncludeCompanionBikePrice()
    {
        Assert.NotNull(bookingContext.Booking);
        Assert.NotNull(bookingContext.Booking.CompanionCustomer);
        var tour = tourContext.Tour;
        var expectedBikePrice = tour.RegularBikePrice;
        Assert.Equal(expectedBikePrice, bookingContext.Booking.CompanionCustomer.BikePrice);
    }

    [Then(@"the booking should include principal regular bike price")]
    public void ThenTheBookingShouldIncludePrincipalRegularBikePrice()
    {
        Assert.NotNull(bookingContext.Booking);
        var tour = tourContext.Tour;
        Assert.Equal(tour.RegularBikePrice, bookingContext.Booking.PrincipalCustomer.BikePrice);
    }

    [Then(@"the booking should include companion e-bike price")]
    public void ThenTheBookingShouldIncludeCompanionEBikePrice()
    {
        Assert.NotNull(bookingContext.Booking);
        Assert.NotNull(bookingContext.Booking.CompanionCustomer);
        var tour = tourContext.Tour;
        Assert.Equal(tour.EBikePrice, bookingContext.Booking.CompanionCustomer.BikePrice);
    }

    [Then(@"both customers should have e-bike pricing")]
    public void ThenBothCustomersShouldHaveEBikePricing()
    {
        Assert.NotNull(bookingContext.Booking);
        Assert.NotNull(bookingContext.Booking.CompanionCustomer);
        var tour = tourContext.Tour;
        Assert.Equal(tour.EBikePrice, bookingContext.Booking.PrincipalCustomer.BikePrice);
        Assert.Equal(tour.EBikePrice, bookingContext.Booking.CompanionCustomer.BikePrice);
    }

    [Then(@"the booking should include single room supplement")]
    public void ThenTheBookingShouldIncludeSingleRoomSupplement()
    {
        Assert.NotNull(bookingContext.Booking);
        Assert.Equal(0m, bookingContext.Booking.RoomAdditionalCost);
    }

    [Then(@"the booking should not include single room supplement")]
    public void ThenTheBookingShouldNotIncludeSingleRoomSupplement()
    {
        Assert.NotNull(bookingContext.Booking);
        var tour = tourContext.Tour;
        Assert.Equal(tour.DoubleRoomSupplementPrice, bookingContext.Booking.RoomAdditionalCost);
    }
}
