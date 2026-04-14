using ViajantesTurismo.Admin.Domain.Customers;
using ViajantesTurismo.Admin.Domain.Shared;
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
    public static Tour BuildTour(TourOptions? options = null)
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
        var builder = new CustomerBuilder(options);
        return builder.Build();
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

    private sealed class CustomerBuilder(CustomerOptions? options)
    {
        private readonly CustomerOptions _options = options ?? new CustomerOptions();

        public Customer Build()
        {
            var personalInfo = PersonalInfo.Create(
                _options.FirstName ?? "John",
                _options.LastName ?? "Doe",
                _options.Gender ?? "Male",
                _options.BirthDate ?? DateTime.UtcNow.AddYears(-30),
                _options.Nationality ?? "USA",
                _options.Occupation ?? "Engineer",
                TimeProvider.System).Value;

            var identificationInfo = IdentificationInfo.Create(
                _options.PassportNumber ?? "A12345678",
                _options.PassportCountry ?? "USA").Value;

            var contactInfo = ContactInfo.Create(
                _options.Email ?? "test@example.com",
                _options.Mobile ?? "+1234567890",
                _options.Instagram,
                _options.Facebook).Value;

            var address = Address.Create(
                _options.Street ?? "123 Main St",
                _options.Complement,
                _options.Neighborhood ?? "Downtown",
                _options.PostalCode ?? "10001",
                _options.City ?? "New York",
                _options.State ?? "NY",
                _options.Country ?? "USA").Value;

            var physicalInfo = PhysicalInfo.Create(
                _options.WeightKg ?? 75,
                _options.HeightCentimeters ?? 175,
                _options.PreferredBike ?? BikeType.Regular).Value;

            var accommodationPreferences = AccommodationPreferences.Create(
                _options.PreferredRoom ?? RoomType.DoubleOccupancy,
                _options.PreferredBed ?? BedType.SingleBed,
                _options.CompanionId).Value;

            var emergencyContact = EmergencyContact.Create(
                _options.EmergencyContactName ?? "Emergency Contact",
                _options.EmergencyContactMobile ?? "+9876543210").Value;

            var medicalInfo = MedicalInfo.Create(
                _options.Allergies ?? "None",
                _options.MedicalAdditionalInfo ?? "None").Value;

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
    }
}
