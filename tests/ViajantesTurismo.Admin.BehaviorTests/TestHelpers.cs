using ViajantesTurismo.Admin.Domain.Customers;
using ViajantesTurismo.Admin.Domain.Tours;
using ViajantesTurismo.Common.Monies;

namespace ViajantesTurismo.Admin.BehaviorTests;

/// <summary>
/// Provides shared helper methods for behavior tests.
/// </summary>
public static class TestHelpers
{
    /// <summary>
    /// Creates a test tour with default values.
    /// </summary>
    public static Tour CreateTestTour()
    {
        return Tour.Create(
            identifier: "TEST2024",
            name: "Test Tour",
            startDate: DateTime.UtcNow.AddMonths(1),
            endDate: DateTime.UtcNow.AddMonths(1).AddDays(7),
            basePrice: 2000.00m,
            doubleRoomSupplementPrice: 500.00m,
            regularBikePrice: 100.00m,
            eBikePrice: 200.00m,
            currency: Currency.UsDollar,
            minCustomers: 4,
            maxCustomers: 12,
            includedServices: ["Hotel", "Breakfast"]).Value;
    }

    /// <summary>
    /// Creates a test tour with specific dates.
    /// </summary>
    public static Tour CreateTestTourWithDates(DateTime startDate, DateTime endDate)
    {
        return Tour.Create(
            identifier: "TEST2024",
            name: "Test Tour",
            startDate: startDate,
            endDate: endDate,
            basePrice: 2000.00m,
            doubleRoomSupplementPrice: 500.00m,
            regularBikePrice: 100.00m,
            eBikePrice: 200.00m,
            currency: Currency.UsDollar,
            minCustomers: 4,
            maxCustomers: 12,
            includedServices: ["Hotel", "Breakfast"]).Value;
    }

    /// <summary>
    /// Creates a test tour with a specific identifier and name.
    /// </summary>
    public static Tour CreateTestTourWithIdentifierAndName(string identifier, string name)
    {
        return Tour.Create(
            identifier: identifier,
            name: name,
            startDate: DateTime.UtcNow.AddMonths(1),
            endDate: DateTime.UtcNow.AddMonths(1).AddDays(7),
            basePrice: 2000.00m,
            doubleRoomSupplementPrice: 500.00m,
            regularBikePrice: 100.00m,
            eBikePrice: 200.00m,
            currency: Currency.UsDollar,
            minCustomers: 4,
            maxCustomers: 12,
            includedServices: ["Hotel", "Breakfast"]).Value;
    }

    /// <summary>
    /// Creates a test tour with a specific base price for payment testing.
    /// With a base price of 900 and regular bike (100), a single room booking has TotalPrice = 1000.
    /// </summary>
    public static Tour CreateTestTourForPaymentTests()
    {
        return Tour.Create(
            identifier: "PAY2024",
            name: "Payment Test Tour",
            startDate: DateTime.UtcNow.AddMonths(1),
            endDate: DateTime.UtcNow.AddMonths(1).AddDays(7),
            basePrice: 900.00m,
            doubleRoomSupplementPrice: 500.00m,
            regularBikePrice: 100.00m,
            eBikePrice: 200.00m,
            currency: Currency.UsDollar,
            minCustomers: 4,
            maxCustomers: 12,
            includedServices: ["Hotel", "Breakfast"]).Value;
    }

    /// <summary>
    /// Parses a booking status string to BookingStatus enum.
    /// </summary>
    public static BookingStatus ParseBookingStatus(string statusString)
    {
        return statusString switch
        {
            "Pending" => BookingStatus.Pending,
            "Confirmed" => BookingStatus.Confirmed,
            "Cancelled" => BookingStatus.Cancelled,
            "Completed" => BookingStatus.Completed,
            _ => throw new ArgumentException($"Unknown status: {statusString}", nameof(statusString))
        };
    }

    /// <summary>
    /// Parses a payment status string to PaymentStatus enum.
    /// </summary>
    public static PaymentStatus ParsePaymentStatus(string statusString)
    {
        return statusString switch
        {
            "Paid" => PaymentStatus.Paid,
            "PartiallyPaid" => PaymentStatus.PartiallyPaid,
            "Unpaid" => PaymentStatus.Unpaid,
            "Refunded" => PaymentStatus.Refunded,
            _ => throw new ArgumentException($"Unknown payment status: {statusString}", nameof(statusString))
        };
    }

    /// <summary>
    /// Parses a currency code string to Currency enum.
    /// </summary>
    public static Currency ParseCurrency(string currencyCode)
    {
        return currencyCode switch
        {
            "USD" => Currency.UsDollar,
            "EUR" => Currency.Euro,
            "BRL" => Currency.Real,
            _ => throw new ArgumentException($"Unknown currency: {currencyCode}", nameof(currencyCode))
        };
    }

    /// <summary>
    /// Creates a test customer with default values.
    /// </summary>
    public static Customer CreateTestCustomer()
    {
        var personalInfo = PersonalInfo.Create("John", "Doe", "Male", DateTime.UtcNow.AddYears(-30), "USA", "Engineer", TimeProvider.System).Value;
        var identificationInfo = IdentificationInfo.Create("A12345678", "USA").Value;
        var contactInfo = ContactInfo.Create("john@example.com", "+1234567890", null, null).Value;
        var address = Address.Create("123 Main St", null, null, "10001", "New York", "NY", "USA").Value;
        var physicalInfo = PhysicalInfo.Create(75, 175, BikeType.Regular).Value;
        var accommodationPreferences = AccommodationPreferences.Create(RoomType.SingleRoom, BedType.SingleBed, null).Value;
        var emergencyContact = EmergencyContact.Create("Jane Doe", "+9876543210").Value;
        var medicalInfo = MedicalInfo.Create("None", "None").Value;

        return new Customer(personalInfo, identificationInfo, contactInfo, address, physicalInfo, accommodationPreferences, emergencyContact, medicalInfo);
    }

    /// <summary>
    /// Creates a test customer with specific names.
    /// </summary>
    public static Customer CreateTestCustomerWithNames(string firstName, string lastName)
    {
        var personalInfo = PersonalInfo.Create(firstName, lastName, "Male", DateTime.UtcNow.AddYears(-30), "USA", "Engineer", TimeProvider.System).Value;
        var identificationInfo = IdentificationInfo.Create("A12345678", "USA").Value;
        var contactInfo = ContactInfo.Create("test@example.com", "+1234567890", null, null).Value;
        var address = Address.Create("123 Main St", null, "Downtown", "10001", "New York", "NY", "USA").Value;
        var physicalInfo = PhysicalInfo.Create(75, 175, BikeType.Regular).Value;
        var accommodationPreferences = AccommodationPreferences.Create(RoomType.SingleRoom, BedType.SingleBed, null).Value;
        var emergencyContact = EmergencyContact.Create("Emergency Contact", "+9876543210").Value;
        var medicalInfo = MedicalInfo.Create("None", "None").Value;

        return new Customer(personalInfo, identificationInfo, contactInfo, address, physicalInfo, accommodationPreferences, emergencyContact, medicalInfo);
    }

    /// <summary>
    /// Creates a test customer with a specific passport.
    /// </summary>
    public static Customer CreateTestCustomerWithPassport(string passport)
    {
        var personalInfo = PersonalInfo.Create("John", "Doe", "Male", DateTime.UtcNow.AddYears(-30), "USA", "Engineer", TimeProvider.System).Value;
        var identificationInfo = IdentificationInfo.Create(passport, "USA").Value;
        var contactInfo = ContactInfo.Create("test@example.com", "+1234567890", null, null).Value;
        var address = Address.Create("123 Main St", null, "Downtown", "10001", "New York", "NY", "USA").Value;
        var physicalInfo = PhysicalInfo.Create(75, 175, BikeType.Regular).Value;
        var accommodationPreferences = AccommodationPreferences.Create(RoomType.SingleRoom, BedType.SingleBed, null).Value;
        var emergencyContact = EmergencyContact.Create("Emergency Contact", "+9876543210").Value;
        var medicalInfo = MedicalInfo.Create("None", "None").Value;

        return new Customer(personalInfo, identificationInfo, contactInfo, address, physicalInfo, accommodationPreferences, emergencyContact, medicalInfo);
    }

    /// <summary>
    /// Creates a test customer with a specific email.
    /// </summary>
    public static Customer CreateTestCustomerWithEmail(string email)
    {
        var personalInfo = PersonalInfo.Create("John", "Doe", "Male", DateTime.UtcNow.AddYears(-30), "USA", "Engineer", TimeProvider.System).Value;
        var identificationInfo = IdentificationInfo.Create("A12345678", "USA").Value;
        var contactInfo = ContactInfo.Create(email, "+1234567890", null, null).Value;
        var address = Address.Create("123 Main St", null, "Downtown", "10001", "New York", "NY", "USA").Value;
        var physicalInfo = PhysicalInfo.Create(75, 175, BikeType.Regular).Value;
        var accommodationPreferences = AccommodationPreferences.Create(RoomType.SingleRoom, BedType.SingleBed, null).Value;
        var emergencyContact = EmergencyContact.Create("Emergency Contact", "+9876543210").Value;
        var medicalInfo = MedicalInfo.Create("None", "None").Value;

        return new Customer(personalInfo, identificationInfo, contactInfo, address, physicalInfo, accommodationPreferences, emergencyContact, medicalInfo);
    }

    /// <summary>
    /// Creates a test customer with a specific city.
    /// </summary>
    public static Customer CreateTestCustomerWithCity(string city)
    {
        var personalInfo = PersonalInfo.Create("John", "Doe", "Male", DateTime.UtcNow.AddYears(-30), "USA", "Engineer", TimeProvider.System).Value;
        var identificationInfo = IdentificationInfo.Create("A12345678", "USA").Value;
        var contactInfo = ContactInfo.Create("test@example.com", "+1234567890", null, null).Value;
        var address = Address.Create("123 Main St", null, "Downtown", "10001", city, "NY", "USA").Value;
        var physicalInfo = PhysicalInfo.Create(75, 175, BikeType.Regular).Value;
        var accommodationPreferences = AccommodationPreferences.Create(RoomType.SingleRoom, BedType.SingleBed, null).Value;
        var emergencyContact = EmergencyContact.Create("Emergency Contact", "+9876543210").Value;
        var medicalInfo = MedicalInfo.Create("None", "None").Value;

        return new Customer(personalInfo, identificationInfo, contactInfo, address, physicalInfo, accommodationPreferences, emergencyContact, medicalInfo);
    }

    /// <summary>
    /// Creates a test customer with a specific height.
    /// </summary>
    public static Customer CreateTestCustomerWithHeight(int height)
    {
        var personalInfo = PersonalInfo.Create("John", "Doe", "Male", DateTime.UtcNow.AddYears(-30), "USA", "Engineer", TimeProvider.System).Value;
        var identificationInfo = IdentificationInfo.Create("A12345678", "USA").Value;
        var contactInfo = ContactInfo.Create("test@example.com", "+1234567890", null, null).Value;
        var address = Address.Create("123 Main St", null, "Downtown", "10001", "New York", "NY", "USA").Value;
        var physicalInfo = PhysicalInfo.Create(75, height, BikeType.Regular).Value;
        var accommodationPreferences = AccommodationPreferences.Create(RoomType.SingleRoom, BedType.SingleBed, null).Value;
        var emergencyContact = EmergencyContact.Create("Emergency Contact", "+9876543210").Value;
        var medicalInfo = MedicalInfo.Create("None", "None").Value;

        return new Customer(personalInfo, identificationInfo, contactInfo, address, physicalInfo, accommodationPreferences, emergencyContact, medicalInfo);
    }

    /// <summary>
    /// Creates a test customer with a specific bed type.
    /// </summary>
    public static Customer CreateTestCustomerWithBedType(BedType bedType)
    {
        var personalInfo = PersonalInfo.Create("John", "Doe", "Male", DateTime.UtcNow.AddYears(-30), "USA", "Engineer", TimeProvider.System).Value;
        var identificationInfo = IdentificationInfo.Create("A12345678", "USA").Value;
        var contactInfo = ContactInfo.Create("test@example.com", "+1234567890", null, null).Value;
        var address = Address.Create("123 Main St", null, "Downtown", "10001", "New York", "NY", "USA").Value;
        var physicalInfo = PhysicalInfo.Create(75, 175, BikeType.Regular).Value;
        var accommodationPreferences = AccommodationPreferences.Create(RoomType.SingleRoom, bedType, null).Value;
        var emergencyContact = EmergencyContact.Create("Emergency Contact", "+9876543210").Value;
        var medicalInfo = MedicalInfo.Create("None", "None").Value;

        return new Customer(personalInfo, identificationInfo, contactInfo, address, physicalInfo, accommodationPreferences, emergencyContact, medicalInfo);
    }

    /// <summary>
    /// Creates a test customer with a specific emergency contact.
    /// </summary>
    public static Customer CreateTestCustomerWithEmergencyContact(string name)
    {
        var personalInfo = PersonalInfo.Create("John", "Doe", "Male", DateTime.UtcNow.AddYears(-30), "USA", "Engineer", TimeProvider.System).Value;
        var identificationInfo = IdentificationInfo.Create("A12345678", "USA").Value;
        var contactInfo = ContactInfo.Create("test@example.com", "+1234567890", null, null).Value;
        var address = Address.Create("123 Main St", null, "Downtown", "10001", "New York", "NY", "USA").Value;
        var physicalInfo = PhysicalInfo.Create(75, 175, BikeType.Regular).Value;
        var accommodationPreferences = AccommodationPreferences.Create(RoomType.SingleRoom, BedType.SingleBed, null).Value;
        var emergencyContact = EmergencyContact.Create(name, "+9876543210").Value;
        var medicalInfo = MedicalInfo.Create("None", "None").Value;

        return new Customer(personalInfo, identificationInfo, contactInfo, address, physicalInfo, accommodationPreferences, emergencyContact, medicalInfo);
    }

    /// <summary>
    /// Creates a test customer with specific allergies.
    /// </summary>
    public static Customer CreateTestCustomerWithAllergies(string allergies)
    {
        var personalInfo = PersonalInfo.Create("John", "Doe", "Male", DateTime.UtcNow.AddYears(-30), "USA", "Engineer", TimeProvider.System).Value;
        var identificationInfo = IdentificationInfo.Create("A12345678", "USA").Value;
        var contactInfo = ContactInfo.Create("test@example.com", "+1234567890", null, null).Value;
        var address = Address.Create("123 Main St", null, "Downtown", "10001", "New York", "NY", "USA").Value;
        var physicalInfo = PhysicalInfo.Create(75, 175, BikeType.Regular).Value;
        var accommodationPreferences = AccommodationPreferences.Create(RoomType.SingleRoom, BedType.SingleBed, null).Value;
        var emergencyContact = EmergencyContact.Create("Emergency Contact", "+9876543210").Value;
        var medicalInfo = MedicalInfo.Create(allergies, "None").Value;

        return new Customer(personalInfo, identificationInfo, contactInfo, address, physicalInfo, accommodationPreferences, emergencyContact, medicalInfo);
    }
}