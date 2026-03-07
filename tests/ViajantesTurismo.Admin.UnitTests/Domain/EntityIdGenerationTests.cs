using ViajantesTurismo.Admin.Domain.Customers;
using ViajantesTurismo.Admin.Domain.Tours;
using ViajantesTurismo.Common.Monies;

namespace ViajantesTurismo.Admin.UnitTests.Domain;

public sealed class EntityIdGenerationTests
{
    [Fact]
    public void Tour_Create_Should_Generate_UuidV7_Id()
    {
        // Act
        var result = Tour.Create(
            identifier: "TEST2024",
            name: "Test Tour",
            startDate: DateTime.UtcNow.AddMonths(1),
            endDate: DateTime.UtcNow.AddMonths(1).AddDays(7),
            basePrice: 2000m,
            singleRoomSupplementPrice: 500m,
            regularBikePrice: 100m,
            eBikePrice: 200m,
            currency: Currency.UsDollar,
            minCustomers: 4,
            maxCustomers: 12,
            includedServices: ["Hotel", "Breakfast"]);

        // Assert
        Assert.True(result.IsSuccess);
        AssertUuidV7(result.Value.Id);
    }

    [Fact]
    public void Customer_Constructor_Should_Generate_UuidV7_Id()
    {
        // Arrange
        var customer = CreateCustomer();

        // Assert
        AssertUuidV7(customer.Id);
    }

    [Fact]
    public void Booking_Create_Should_Generate_UuidV7_Id()
    {
        // Arrange
        var principalResult = BookingCustomer.Create(Guid.CreateVersion7(), BikeType.Regular, 100m);
        var discountResult = Discount.Create(DiscountType.None, 0m, null);

        // Act
        var result = Booking.Create(
            tourId: Guid.CreateVersion7(),
            basePrice: 2000m,
            roomType: RoomType.DoubleOccupancy,
            roomAdditionalCost: 0m,
            principalCustomer: principalResult.Value,
            companionCustomer: null,
            discount: discountResult.Value,
            notes: null);

        // Assert
        Assert.True(principalResult.IsSuccess);
        Assert.True(discountResult.IsSuccess);
        Assert.True(result.IsSuccess);
        AssertUuidV7(result.Value.Id);
    }

    [Fact]
    public void Payment_Create_Should_Generate_UuidV7_Id()
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
        AssertUuidV7(result.Value.Id);
    }

    private static void AssertUuidV7(Guid id)
    {
        var guidText = id.ToString("D");
        Assert.Equal('7', guidText[14]);
    }

    private static Customer CreateCustomer()
    {
        var personalInfo = PersonalInfo.Create(
            firstName: "John",
            lastName: "Doe",
            gender: "Male",
            birthDate: DateTime.UtcNow.AddYears(-30),
            nationality: "USA",
            occupation: "Engineer",
            timeProvider: TimeProvider.System).Value;

        var identificationInfo = IdentificationInfo.Create(
            nationalId: "A12345678",
            idNationality: "USA").Value;

        var contactInfo = ContactInfo.Create(
            email: "test@example.com",
            mobile: "+1234567890",
            instagram: null,
            facebook: null).Value;

        var address = Address.Create(
            street: "123 Main St",
            complement: null,
            neighborhood: "Downtown",
            postalCode: "10001",
            city: "New York",
            state: "NY",
            country: "USA").Value;

        var physicalInfo = PhysicalInfo.Create(
            weightKg: 75,
            heightCentimeters: 175,
            bikeType: BikeType.Regular).Value;

        var accommodation = AccommodationPreferences.Create(
            roomType: RoomType.DoubleOccupancy,
            bedType: BedType.SingleBed,
            companionId: null).Value;

        var emergency = EmergencyContact.Create(
            name: "Emergency Contact",
            mobile: "+9876543210").Value;

        var medical = MedicalInfo.Create(
            allergies: "None",
            additionalInfo: "None").Value;

        return new Customer(
            personalInfo,
            identificationInfo,
            contactInfo,
            address,
            physicalInfo,
            accommodation,
            emergency,
            medical);
    }
}
