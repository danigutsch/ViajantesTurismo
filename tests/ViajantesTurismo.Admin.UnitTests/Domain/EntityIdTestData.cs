using ViajantesTurismo.Admin.Domain.Customers;
using ViajantesTurismo.Admin.Domain.Shared;
using ViajantesTurismo.Admin.Domain.Tours;

namespace ViajantesTurismo.Admin.UnitTests.Domain;

internal static class EntityIdTestData
{
    public static Customer CreateCustomer()
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

    public static Payment CreatePayment()
    {
        var result = Payment.Create(
            bookingId: Guid.CreateVersion7(),
            amount: 250m,
            paymentDate: DateTime.UtcNow,
            method: PaymentMethod.CreditCard,
            timeProvider: TimeProvider.System,
            referenceNumber: "TX-123",
            notes: "Initial payment");

        Assert.True(result.IsSuccess);
        return result.Value;
    }
}
