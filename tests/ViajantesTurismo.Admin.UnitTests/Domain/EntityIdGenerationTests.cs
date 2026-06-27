using ViajantesTurismo.Admin.Domain.Shared;
using ViajantesTurismo.Admin.Domain.Tours;
using ViajantesTurismo.Common.Monies;

namespace ViajantesTurismo.Admin.UnitTests.Domain;

public sealed class EntityIdGenerationTests
{
    [Fact]
    public void Tour_create_should_generate_uuidv7_id()
    {
        // Act
        var result = Tour.Create(new TourDefinition(
            "TEST2024",
            "Test Tour",
            DateTime.UtcNow.AddMonths(1),
            DateTime.UtcNow.AddMonths(1).AddDays(7),
            2000m,
            500m,
            100m,
            200m,
            Currency.UsDollar,
            4,
            12,
            ["Hotel", "Breakfast"]));

        // Assert
        Assert.True(result.IsSuccess);
        EntityIdAssertions.AssertUuidV7(result.Value.Id);
    }

    [Fact]
    public void Customer_constructor_should_generate_uuidv7_id()
    {
        // Arrange
        var customer = EntityIdTestData.CreateCustomer();

        // Assert
        EntityIdAssertions.AssertUuidV7(customer.Id);
    }

    [Fact]
    public void Booking_create_should_generate_uuidv7_id()
    {
        // Arrange
        var principalResult = BookingCustomer.Create(Guid.CreateVersion7(), BikeType.Regular, 100m);
        var discountResult = Discount.Create(DiscountType.None, 0m, null);

        // Act
        var result = Booking.Create(
            tourId: Guid.CreateVersion7(),
            basePrice: 2000m,
            room: new BookingRoom(RoomType.DoubleOccupancy, 0m),
            principalCustomer: principalResult.Value,
            companionCustomer: null,
            discount: discountResult.Value,
            notes: null);

        // Assert
        Assert.True(principalResult.IsSuccess);
        Assert.True(discountResult.IsSuccess);
        Assert.True(result.IsSuccess);
        EntityIdAssertions.AssertUuidV7(result.Value.Id);
    }

    [Fact]
    public void Payment_create_should_generate_uuidv7_id()
    {
        // Act
        var result = Payment.Create(
            bookingId: Guid.CreateVersion7(),
            amount: 250m,
            paymentDate: DateTime.UtcNow,
            method: PaymentMethod.CreditCard,
            timeProvider: TimeProvider.System,
            referenceNumber: "TX-123",
            notes: "Initial payment");

        // Assert
        Assert.True(result.IsSuccess);
        EntityIdAssertions.AssertUuidV7(result.Value.Id);
    }

}
