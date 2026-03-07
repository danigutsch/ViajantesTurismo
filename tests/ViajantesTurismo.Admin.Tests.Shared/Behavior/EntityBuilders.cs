using ViajantesTurismo.Admin.Domain.Customers;
using ViajantesTurismo.Admin.Domain.Tours;
using ViajantesTurismo.Common.Monies;

namespace ViajantesTurismo.Admin.Tests.Shared.Behavior;

/// <summary>
/// Provides builder methods for creating domain entities with default or custom values for behavior tests.
/// </summary>
public static class EntityBuilders
{
    /// <summary>
    /// Builds a Tour entity with default or custom values.
    /// </summary>
    public static Tour BuildTour(
        string? identifier = null,
        string? name = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        decimal? basePrice = null,
        decimal? singleRoomSupplementPrice = null,
        decimal? regularBikePrice = null,
        decimal? eBikePrice = null,
        Currency? currency = null,
        int? minCustomers = null,
        int? maxCustomers = null,
        string[]? includedServices = null)
    {
        var start = startDate ?? DateTime.UtcNow.AddMonths(1);
        var end = endDate ?? start.AddDays(7);

        return Tour.Create(
            identifier: identifier ?? "TEST2024",
            name: name ?? "Test Tour",
            startDate: start,
            endDate: end,
            basePrice: basePrice ?? 2000.00m,
            singleRoomSupplementPrice: singleRoomSupplementPrice ?? 500.00m,
            regularBikePrice: regularBikePrice ?? 100.00m,
            eBikePrice: eBikePrice ?? 200.00m,
            currency: currency ?? Currency.UsDollar,
            minCustomers: minCustomers ?? 4,
            maxCustomers: maxCustomers ?? 12,
            includedServices: includedServices ?? ["Hotel", "Breakfast"]).Value;
    }

    /// <summary>
    /// Builds a Customer entity with default or custom values.
    /// </summary>
    public static Customer BuildCustomer(
        string? firstName = null,
        string? lastName = null,
        string? gender = null,
        DateTime? birthDate = null,
        string? nationality = null,
        string? occupation = null,
        string? passportNumber = null,
        string? passportCountry = null,
        string? email = null,
        string? mobile = null,
        string? instagram = null,
        string? facebook = null,
        string? street = null,
        string? complement = null,
        string? neighborhood = null,
        string? postalCode = null,
        string? city = null,
        string? state = null,
        string? country = null,
        int? weightKg = null,
        int? heightCentimeters = null,
        BikeType? preferredBike = null,
        RoomType? preferredRoom = null,
        BedType? preferredBed = null,
        Guid? companionId = null,
        string? emergencyContactName = null,
        string? emergencyContactMobile = null,
        string? allergies = null,
        string? medicalAdditionalInfo = null)
    {
        var personalInfo = PersonalInfo.Create(
            firstName ?? "John",
            lastName ?? "Doe",
            gender ?? "Male",
            birthDate ?? DateTime.UtcNow.AddYears(-30),
            nationality ?? "USA",
            occupation ?? "Engineer",
            TimeProvider.System).Value;

        var identificationInfo = IdentificationInfo.Create(
            passportNumber ?? "A12345678",
            passportCountry ?? "USA").Value;

        var contactInfo = ContactInfo.Create(
            email ?? "test@example.com",
            mobile ?? "+1234567890",
            instagram,
            facebook).Value;

        var address = Address.Create(
            street ?? "123 Main St",
            complement,
            neighborhood ?? "Downtown",
            postalCode ?? "10001",
            city ?? "New York",
            state ?? "NY",
            country ?? "USA").Value;

        var physicalInfo = PhysicalInfo.Create(
            weightKg ?? 75,
            heightCentimeters ?? 175,
            preferredBike ?? BikeType.Regular).Value;

        var accommodationPreferences = AccommodationPreferences.Create(
            preferredRoom ?? RoomType.DoubleOccupancy,
            preferredBed ?? BedType.SingleBed,
            companionId).Value;

        var emergencyContact = EmergencyContact.Create(
            emergencyContactName ?? "Emergency Contact",
            emergencyContactMobile ?? "+9876543210").Value;

        var medicalInfo = MedicalInfo.Create(
            allergies ?? "None",
            medicalAdditionalInfo ?? "None").Value;

        return new Customer(
            personalInfo,
            identificationInfo,
            contactInfo,
            address,
            physicalInfo,
            accommodationPreferences,
            emergencyContact,
            medicalInfo);
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
}
