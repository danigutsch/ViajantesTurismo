using ViajantesTurismo.Admin.Contracts;

namespace ViajantesTurismo.Admin.IntegrationTests.Helpers;

/// <summary>
/// Builder methods for creating test DTOs with sensible defaults.
/// </summary>
internal static class DtoBuilders
{
    /// <summary>
    /// Builds a CreateTourDto with default or custom values.
    /// </summary>
    public static CreateTourDto BuildCreateTourDto(
        string? identifier = null,
        string? name = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        decimal? basePrice = null,
        decimal? doubleRoomSupplement = null,
        decimal? regularBikePrice = null,
        decimal? eBikePrice = null,
        int? minCustomers = null,
        int? maxCustomers = null,
        CurrencyDto? currency = null,
        string[]? includedServices = null)
    {
        var tourIdentifier = identifier ?? TestDataGenerator.UniqueTourIdentifier();
        var start = startDate ?? DateTime.UtcNow.AddMonths(2);
        var end = endDate ?? start.AddDays(10);

        return new CreateTourDto
        {
            Identifier = tourIdentifier,
            Name = name ?? $"Test Tour {tourIdentifier}",
            StartDate = start,
            EndDate = end,
            Price = basePrice ?? TestDefaults.BaseTourPrice,
            DoubleRoomSupplementPrice = doubleRoomSupplement ?? TestDefaults.DoubleRoomSupplement,
            RegularBikePrice = regularBikePrice ?? TestDefaults.RegularBikePrice,
            EBikePrice = eBikePrice ?? TestDefaults.EBikePrice,
            MinCustomers = minCustomers ?? TestDefaults.MinCustomers,
            MaxCustomers = maxCustomers ?? TestDefaults.MaxCustomers,
            Currency = currency ?? CurrencyDto.UsDollar,
            IncludedServices = includedServices ?? ["Hotel", "Breakfast", "Bike"]
        };
    }

    /// <summary>
    /// Builds a CreateCustomerDto with default or custom values.
    /// </summary>
    public static CreateCustomerDto BuildCreateCustomerDto(
        string? firstName = null,
        string? lastName = null,
        string? email = null,
        string? mobile = null,
        string? nationalId = null,
        BikeTypeDto? bikeType = null,
        RoomTypeDto? roomType = null,
        BedTypeDto? bedType = null)
    {
        var first = firstName ?? "Test";
        var last = lastName ?? "User";

        return new CreateCustomerDto
        {
            PersonalInfo = new PersonalInfoDto
            {
                FirstName = first,
                LastName = last,
                BirthDate = new DateTime(1990, 1, 1).ToUniversalTime(),
                Gender = "Male",
                Nationality = "American",
                Profession = "Engineer"
            },
            IdentificationInfo = new IdentificationInfoDto
            {
                NationalId = nationalId ?? TestDataGenerator.UniqueNationalId(),
                IdNationality = "American"
            },
            ContactInfo = new ContactInfoDto
            {
                Email = email ?? TestDataGenerator.UniqueEmail($"{first.ToLower()}.{last.ToLower()}"),
                Mobile = mobile ?? TestDataGenerator.UniquePhone(),
                Instagram = null,
                Facebook = null
            },
            Address = new AddressDto
            {
                Street = "123 Main St",
                Complement = null,
                Neighborhood = "Downtown",
                PostalCode = "12345",
                City = "New York",
                State = "NY",
                Country = "USA"
            },
            PhysicalInfo = new PhysicalInfoDto
            {
                WeightKg = 75.0m,
                HeightCentimeters = 180,
                BikeType = bikeType ?? BikeTypeDto.Regular
            },
            AccommodationPreferences = new AccommodationPreferencesDto
            {
                RoomType = roomType ?? RoomTypeDto.SingleRoom,
                BedType = bedType ?? BedTypeDto.SingleBed,
                CompanionId = null
            },
            EmergencyContact = new EmergencyContactDto
            {
                Name = "Emergency Contact",
                Mobile = "+15559876543"
            },
            MedicalInfo = new MedicalInfoDto
            {
                Allergies = null,
                AdditionalInfo = null
            }
        };
    }

    /// <summary>
    /// Builds a CreateBookingDto with default or custom values.
    /// </summary>
    public static CreateBookingDto BuildCreateBookingDto(
        Guid? tourId = null,
        Guid? principalCustomerId = null,
        BikeTypeDto? principalBikeType = null,
        Guid? companionCustomerId = null,
        BikeTypeDto? companionBikeType = null,
        RoomTypeDto? roomType = null,
        string? notes = null,
        DiscountTypeDto discountType = DiscountTypeDto.None,
        decimal discountAmount = 0m,
        string? discountReason = null)
    {
        var hasCompanion = companionCustomerId.HasValue;

        return new CreateBookingDto
        {
            TourId = tourId ?? Guid.CreateVersion7(),
            PrincipalCustomerId = principalCustomerId ?? Guid.CreateVersion7(),
            PrincipalBikeType = principalBikeType ?? BikeTypeDto.Regular,
            CompanionCustomerId = companionCustomerId,
            CompanionBikeType = hasCompanion ? (companionBikeType ?? BikeTypeDto.Regular) : null,
            RoomType = roomType ?? (hasCompanion ? RoomTypeDto.DoubleRoom : RoomTypeDto.SingleRoom),
            Notes = notes ?? "Test booking",
            DiscountType = discountType,
            DiscountAmount = discountAmount,
            DiscountReason = discountReason
        };
    }

    /// <summary>
    /// Builds a CreatePaymentDto with default or custom values.
    /// </summary>
    public static CreatePaymentDto BuildCreatePaymentDto(
        decimal? amount = null,
        DateTime? paymentDate = null,
        PaymentMethodDto? method = null,
        string? referenceNumber = null,
        string? notes = null)
    {
        return new CreatePaymentDto
        {
            Amount = amount ?? 500m,
            PaymentDate = paymentDate ?? DateTime.UtcNow.Date,
            Method = method ?? PaymentMethodDto.BankTransfer,
            ReferenceNumber = referenceNumber ?? TestDataGenerator.UniqueReferenceNumber(),
            Notes = notes
        };
    }

    /// <summary>
    /// Builds an UpdateBookingNotesDto with default or custom values.
    /// </summary>
    public static UpdateBookingNotesDto BuildUpdateBookingNotesDto(string? notes = null)
    {
        return new UpdateBookingNotesDto
        {
            Notes = notes ?? "Updated notes"
        };
    }

    /// <summary>
    /// Builds an UpdateBookingDiscountDto with default or custom values.
    /// </summary>
    public static UpdateBookingDiscountDto BuildUpdateBookingDiscountDto(
        DiscountTypeDto discountType = DiscountTypeDto.Percentage,
        decimal discountAmount = TestDefaults.ValidPercentageDiscount,
        string? discountReason = null)
    {
        return new UpdateBookingDiscountDto
        {
            DiscountType = discountType,
            DiscountAmount = discountAmount,
            DiscountReason = discountReason ?? "Test discount"
        };
    }

    /// <summary>
    /// Builds an UpdateBookingDetailsDto with default or custom values.
    /// </summary>
    public static UpdateBookingDetailsDto BuildUpdateBookingDetailsDto(
        RoomTypeDto? roomType = null,
        BikeTypeDto? principalBikeType = null,
        Guid? companionCustomerId = null,
        BikeTypeDto? companionBikeType = null)
    {
        return new UpdateBookingDetailsDto
        {
            RoomType = roomType ?? RoomTypeDto.SingleRoom,
            PrincipalBikeType = principalBikeType ?? BikeTypeDto.Regular,
            CompanionCustomerId = companionCustomerId,
            CompanionBikeType = companionBikeType
        };
    }
}
