using ViajantesTurismo.Admin.Domain.Shared;
using ViajantesTurismo.Admin.Domain.Tours;
using ViajantesTurismo.Admin.Testing.Behavior;

namespace ViajantesTurismo.Admin.UnitTests.Domain;

/// <summary>Protects atomic whole-selection pricing through the Tour aggregate boundary.</summary>
public sealed class TourPricingInvariantTests
{
    [Fact]
    public void Accepts_a_valid_combined_change_even_when_its_room_change_alone_would_overdiscount()
    {
        // Arrange
        var tour = EntityBuilders.BuildTour(new TourOptions(Pricing: new TourPricingOptions(
            BasePrice: 100, SingleRoomSupplementPrice: 50, RegularBikePrice: 20, EBikePrice: 100)));
        var booking = BookingTestHelpers.AddSingleCustomerBooking(tour, new SingleBookingOptions(
            RoomType: RoomType.SingleOccupancy,
            Discount: new BookingDiscountDefinition(DiscountType.Absolute, 160, "Seasonal booking offer"))).Value;

        // Act
        var result = tour.UpdateBookingDetails(booking.Id, RoomType.DoubleOccupancy, BikeType.EBike, null, null);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        booking.RoomType.ShouldBe(RoomType.DoubleOccupancy);
        booking.PrincipalCustomer.BikeType.ShouldBe(BikeType.EBike);
        booking.Subtotal.ShouldBe(200m);
        booking.TotalPrice.ShouldBe(40m);
    }

    [Fact]
    public void Rejects_an_invalid_combined_change_without_altering_any_booked_services()
    {
        // Arrange
        var tour = EntityBuilders.BuildTour(new TourOptions(Pricing: new TourPricingOptions(
            BasePrice: 100, SingleRoomSupplementPrice: 50, RegularBikePrice: 20, EBikePrice: 100)));
        var booking = BookingTestHelpers.AddSingleCustomerBooking(tour, new SingleBookingOptions(
            RoomType: RoomType.SingleOccupancy,
            Discount: new BookingDiscountDefinition(DiscountType.Absolute, 160, "Seasonal booking offer"))).Value;
        var principal = booking.PrincipalCustomer;

        // Act
        var result = tour.UpdateBookingDetails(booking.Id, RoomType.DoubleOccupancy, BikeType.Regular, null, null);

        // Assert
        result.IsFailure.ShouldBeTrue();
        booking.RoomType.ShouldBe(RoomType.SingleOccupancy);
        booking.RoomAdditionalCost.ShouldBe(50m);
        booking.PrincipalCustomer.ShouldBe(principal);
        booking.CompanionCustomer.ShouldBeNull();
        booking.TotalPrice.ShouldBe(10m);
    }
}
