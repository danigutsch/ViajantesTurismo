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
        return BuildTour(new TourOptions(
            identifier,
            name,
            new TourScheduleOptions(startDate, endDate),
            new TourPricingOptions(basePrice, singleRoomSupplementPrice, regularBikePrice, eBikePrice, currency),
            new TourCapacityOptions(minCustomers, maxCustomers),
            includedServices));
    }

    /// <summary>
    /// Builds a Tour entity with default or custom values.
    /// </summary>
    public static Tour BuildTour(TourOptions? options)
    {
        options ??= new TourOptions();
        var scheduleOptions = options.Schedule ?? new TourScheduleOptions();
        var pricingOptions = options.Pricing ?? new TourPricingOptions();
        var capacityOptions = options.Capacity ?? new TourCapacityOptions();

        var start = scheduleOptions.StartDate ?? DateTime.UtcNow.AddMonths(1);

        return Tour.Create(new TourDefinition(
            options.Identifier ?? "TEST2024",
            options.Name ?? "Test Tour",
            new TourScheduleDefinition(
                start,
                scheduleOptions.EndDate ?? start.AddDays(7)),
            new TourPricingDefinition(
                pricingOptions.BasePrice ?? 2000.00m,
                pricingOptions.SingleRoomSupplementPrice ?? 500.00m,
                pricingOptions.RegularBikePrice ?? 100.00m,
                pricingOptions.EBikePrice ?? 200.00m,
                pricingOptions.Currency ?? Currency.UsDollar),
            new TourCapacityDefinition(
                capacityOptions.MinCustomers ?? 4,
                capacityOptions.MaxCustomers ?? 12),
            options.IncludedServices ?? ["Hotel", "Breakfast"])).Value;
    }

    /// <summary>
    /// Builds a Customer entity with default or custom values.
    /// </summary>
    public static Customer BuildCustomer(CustomerOptions? options)
    {
        options ??= new CustomerOptions();

        var personalInfo = PersonalInfo.Create(
            options.FirstName ?? "John",
            options.LastName ?? "Doe",
            options.Gender ?? "Male",
            options.BirthDate ?? DateTime.UtcNow.AddYears(-30),
            options.Nationality ?? "USA",
            options.Occupation ?? "Engineer",
            TimeProvider.System).Value;

        var identificationInfo = IdentificationInfo.Create(
            options.PassportNumber ?? "A12345678",
            options.PassportCountry ?? "USA").Value;

        var contactInfo = ContactInfo.Create(
            options.Email ?? "test@example.com",
            options.Mobile ?? "+1234567890",
            options.Instagram,
            options.Facebook).Value;

        var address = Address.Create(
            options.Street ?? "123 Main St",
            options.Complement,
            options.Neighborhood ?? "Downtown",
            options.PostalCode ?? "10001",
            options.City ?? "New York",
            options.State ?? "NY",
            options.Country ?? "USA").Value;

        var physicalInfo = PhysicalInfo.Create(
            options.WeightKg ?? 75,
            options.HeightCentimeters ?? 175,
            options.PreferredBike ?? BikeType.Regular).Value;

        var accommodationPreferences = AccommodationPreferences.Create(
            options.PreferredRoom ?? RoomType.DoubleOccupancy,
            options.PreferredBed ?? BedType.SingleBed,
            options.CompanionId).Value;

        var emergencyContact = EmergencyContact.Create(
            options.EmergencyContactName ?? "Emergency Contact",
            options.EmergencyContactMobile ?? "+9876543210").Value;

        var medicalInfo = MedicalInfo.Create(
            options.Allergies ?? "None",
            options.MedicalAdditionalInfo ?? "None").Value;

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
