using ViajantesTurismo.Admin.Domain.Shared;
using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.Admin.UnitTests.Domain;

/// <summary>Protects existing booking discounts when later service changes lower the subtotal.</summary>
public sealed class BookingPricingInvariantTests
{
    [Fact]
    public void Rejects_room_changes_that_make_the_existing_discount_exceed_the_subtotal()
    {
        // Arrange
        var principal = BookingCustomer.Create(Guid.CreateVersion7(), BikeType.Regular, 20).Value;
        var discount = Discount.Create(DiscountType.Absolute, 160, "Seasonal booking offer").Value;
        var booking = Booking.Create(Guid.CreateVersion7(), 100, new BookingRoom(RoomType.SingleOccupancy, 50), principal, null,
            discount, null, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)).Value;

        // Act
        var result = booking.UpdateRoom(RoomType.DoubleOccupancy, 0, null);

        // Assert
        result.IsFailure.ShouldBeTrue();
        booking.RoomType.ShouldBe(RoomType.SingleOccupancy);
        booking.RoomAdditionalCost.ShouldBe(50m);
        booking.TotalPrice.ShouldBe(10m);
    }

    [Fact]
    public void Rejects_companion_removal_that_makes_the_existing_discount_exceed_the_subtotal()
    {
        // Arrange
        var principal = BookingCustomer.Create(Guid.CreateVersion7(), BikeType.Regular, 20).Value;
        var companion = BookingCustomer.Create(Guid.CreateVersion7(), BikeType.EBike, 30).Value;
        var discount = Discount.Create(DiscountType.Absolute, 140, "Seasonal booking offer").Value;
        var booking = Booking.Create(Guid.CreateVersion7(), 100, new BookingRoom(RoomType.DoubleOccupancy, 0), principal, companion,
            discount, null, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)).Value;

        // Act
        var result = booking.UpdateCompanion(null);

        // Assert
        result.IsFailure.ShouldBeTrue();
        booking.CompanionCustomer.ShouldBe(companion);
        booking.TotalPrice.ShouldBe(10m);
    }

    [Fact]
    public void Rejects_bike_changes_that_make_the_existing_discount_exceed_the_subtotal()
    {
        // Arrange
        var principal = BookingCustomer.Create(Guid.CreateVersion7(), BikeType.Regular, 20).Value;
        var discount = Discount.Create(DiscountType.Absolute, 160, "Seasonal booking offer").Value;
        var booking = Booking.Create(Guid.CreateVersion7(), 100, new BookingRoom(RoomType.SingleOccupancy, 50), principal, null,
            discount, null, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)).Value;

        // Act
        var result = booking.UpdatePrincipalBike(BikeType.Regular, 0);

        // Assert
        result.IsFailure.ShouldBeTrue();
        booking.PrincipalCustomer.ShouldBe(principal);
        booking.TotalPrice.ShouldBe(10m);
    }
}
