using ViajantesTurismo.Admin.Contracts;

namespace ViajantesTurismo.Admin.Tests.Shared.Builders;

/// <summary>
/// Provides builder methods for creating DTO instances with default or custom values for tests.
/// </summary>
public static class DtoBuilders
{
    private const decimal DefaultBaseTourPrice = 2000m;
    private const decimal DefaultSingleRoomSupplement = 500m;
    private const decimal DefaultRegularBikePrice = 100m;
    private const decimal DefaultEBikePrice = 200m;
    private const int DefaultMinCustomers = 4;
    private const int DefaultMaxCustomers = 12;
    private const decimal DefaultValidPercentageDiscount = 10m;

    /// <summary>
    /// Builds a GetTourDto with default or custom values.
    /// </summary>
    public static GetTourDto BuildTourDto(
        Guid? id = null,
        string? identifier = null,
        string? name = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        decimal? price = null,
        decimal? singleRoomSupplementPrice = null,
        decimal? regularBikePrice = null,
        decimal? eBikePrice = null,
        CurrencyDto? currency = null,
        ICollection<string>? includedServices = null,
        int? minCustomers = null,
        int? maxCustomers = null,
        int? currentCustomerCount = null)
    {
        var start = startDate ?? DateTime.UtcNow.AddMonths(1);
        var end = endDate ?? start.AddDays(7);

        return new GetTourDto
        {
            Id = id ?? Guid.NewGuid(),
            Identifier = identifier ?? "TOUR-2024-001",
            Name = name ?? "Test Tour",
            StartDate = start,
            EndDate = end,
            Price = price ?? 1500.00m,
            SingleRoomSupplementPrice = singleRoomSupplementPrice ?? 300.00m,
            RegularBikePrice = regularBikePrice ?? 100.00m,
            EBikePrice = eBikePrice ?? 250.00m,
            Currency = currency ?? CurrencyDto.Real,
            IncludedServices = includedServices ?? new List<string> { "Breakfast", "Bike rental" },
            MinCustomers = minCustomers ?? 10,
            MaxCustomers = maxCustomers ?? 30,
            CurrentCustomerCount = currentCustomerCount ?? 15
        };
    }

    /// <summary>
    /// Builds a GetBookingDto with default or custom values.
    /// </summary>
    public static GetBookingDto BuildBookingDto(
        Guid? id = null,
        Guid? tourId = null,
        string? tourIdentifier = null,
        string? tourName = null,
        Guid? customerId = null,
        string? customerName = null,
        Guid? companionId = null,
        string? companionName = null,
        DateTime? bookingDate = null,
        BookingStatusDto? status = null,
        PaymentStatusDto? paymentStatus = null,
        decimal? totalPrice = null,
        DiscountTypeDto? discountType = null,
        decimal? discountAmount = null,
        string? discountReason = null,
        string? notes = null,
        IReadOnlyCollection<GetPaymentDto>? payments = null,
        decimal? amountPaid = null,
        decimal? remainingBalance = null,
        CurrencyDto? currency = null)
    {
        var bookingTotalPrice = totalPrice ?? 1000m;
        var bookingAmountPaid = amountPaid ?? 0m;

        return new GetBookingDto
        {
            Id = id ?? Guid.NewGuid(),
            TourId = tourId ?? Guid.NewGuid(),
            TourIdentifier = tourIdentifier ?? "TOUR-001",
            TourName = tourName ?? "Test Tour",
            CustomerId = customerId ?? Guid.NewGuid(),
            CustomerName = customerName ?? "John Doe",
            CompanionId = companionId,
            CompanionName = companionName,
            BookingDate = bookingDate ?? DateTime.UtcNow,
            Status = status ?? BookingStatusDto.Pending,
            PaymentStatus = paymentStatus ?? PaymentStatusDto.Unpaid,
            TotalPrice = bookingTotalPrice,
            DiscountType = discountType ?? DiscountTypeDto.None,
            DiscountAmount = discountAmount ?? 0m,
            DiscountReason = discountReason,
            Notes = notes,
            Payments = payments ?? Array.Empty<GetPaymentDto>(),
            AmountPaid = bookingAmountPaid,
            RemainingBalance = remainingBalance ?? (bookingTotalPrice - bookingAmountPaid),
            Currency = currency ?? CurrencyDto.Real
        };
    }

    /// <summary>
    /// Builds a GetCustomerDto with default or custom values.
    /// </summary>
    public static GetCustomerDto BuildCustomerDto(
        Guid? id = null,
        string? firstName = null,
        string? lastName = null,
        string? email = null,
        string? mobile = null,
        string? nationality = null,
        BikeTypeDto? bikeType = null)
    {
        return new GetCustomerDto
        {
            Id = id ?? Guid.NewGuid(),
            FirstName = firstName ?? "John",
            LastName = lastName ?? "Doe",
            Email = email ?? "john.doe@example.com",
            Mobile = mobile ?? "+1234567890",
            Nationality = nationality ?? "USA",
            BikeType = bikeType ?? BikeTypeDto.Regular
        };
    }

    /// <summary>
    /// Builds a GetPaymentDto with default or custom values.
    /// </summary>
    public static GetPaymentDto BuildPaymentDto(
        Guid? id = null,
        Guid? bookingId = null,
        decimal? amount = null,
        DateTime? paymentDate = null,
        PaymentMethodDto? method = null,
        string? referenceNumber = null,
        string? notes = null,
        DateTime? recordedAt = null)
    {
        return new GetPaymentDto
        {
            Id = id ?? Guid.NewGuid(),
            BookingId = bookingId ?? Guid.NewGuid(),
            Amount = amount ?? 500.00m,
            PaymentDate = paymentDate ?? DateTime.UtcNow,
            Method = method ?? PaymentMethodDto.CreditCard,
            ReferenceNumber = referenceNumber,
            Notes = notes,
            RecordedAt = recordedAt ?? DateTime.UtcNow
        };
    }

    /// <summary>
    /// Builds a CustomerDetailsDto with default or custom values.
    /// </summary>
    public static CustomerDetailsDto BuildCustomerDetailsDto(
        Guid? id = null,
        PersonalInfoDto? personalInfo = null,
        IdentificationInfoDto? identificationInfo = null,
        ContactInfoDto? contactInfo = null,
        AddressDto? address = null,
        PhysicalInfoDto? physicalInfo = null,
        AccommodationPreferencesDto? accommodationPreferences = null,
        EmergencyContactDto? emergencyContact = null,
        MedicalInfoDto? medicalInfo = null)
    {
        return new CustomerDetailsDto
        {
            Id = id ?? Guid.NewGuid(),
            PersonalInfo = personalInfo ?? new PersonalInfoDto
            {
                FirstName = "John",
                LastName = "Doe",
                Gender = "Male",
                BirthDate = DateTime.UtcNow.AddYears(-30),
                Nationality = "USA",
                Occupation = "Engineer"
            },
            IdentificationInfo = identificationInfo ?? new IdentificationInfoDto
            {
                NationalId = "A12345678",
                IdNationality = "USA"
            },
            ContactInfo = contactInfo ?? new ContactInfoDto
            {
                Email = "john.doe@example.com",
                Mobile = "+1234567890",
                Instagram = null,
                Facebook = null
            },
            Address = address ?? new AddressDto
            {
                Street = "123 Main St",
                Complement = null,
                Neighborhood = "Downtown",
                PostalCode = "10001",
                City = "New York",
                State = "NY",
                Country = "USA"
            },
            PhysicalInfo = physicalInfo ?? new PhysicalInfoDto
            {
                WeightKg = 75,
                HeightCentimeters = 175,
                BikeType = BikeTypeDto.Regular
            },
            AccommodationPreferences = accommodationPreferences ?? new AccommodationPreferencesDto
            {
                RoomType = RoomTypeDto.DoubleOccupancy,
                BedType = BedTypeDto.SingleBed,
                CompanionId = null
            },
            EmergencyContact = emergencyContact ?? new EmergencyContactDto
            {
                Name = "Emergency Contact",
                Mobile = "+9876543210"
            },
            MedicalInfo = medicalInfo ?? new MedicalInfoDto
            {
                Allergies = "None",
                AdditionalInfo = "None"
            }
        };
    }

    /// <summary>
    /// Builds a CreateTourDto with default or custom values.
    /// </summary>
    public static CreateTourDto BuildCreateTourDto(
        string? identifier = null,
        string? name = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        decimal? basePrice = null,
        decimal? singleRoomSupplement = null,
        decimal? regularBikePrice = null,
        decimal? eBikePrice = null,
        int? minCustomers = null,
        int? maxCustomers = null,
        CurrencyDto? currency = null,
        string[]? includedServices = null)
    {
        var tourIdentifier = identifier ?? UniqueTourIdentifier();
        var start = startDate ?? DateTime.UtcNow.AddMonths(2);
        var end = endDate ?? start.AddDays(10);

        return new CreateTourDto
        {
            Identifier = tourIdentifier,
            Name = name ?? $"Test Tour {tourIdentifier}",
            StartDate = start,
            EndDate = end,
            Price = basePrice ?? DefaultBaseTourPrice,
            SingleRoomSupplementPrice = singleRoomSupplement ?? DefaultSingleRoomSupplement,
            RegularBikePrice = regularBikePrice ?? DefaultRegularBikePrice,
            EBikePrice = eBikePrice ?? DefaultEBikePrice,
            MinCustomers = minCustomers ?? DefaultMinCustomers,
            MaxCustomers = maxCustomers ?? DefaultMaxCustomers,
            Currency = currency ?? CurrencyDto.UsDollar,
            IncludedServices = includedServices ?? ["Hotel", "Breakfast", "Bike"]
        };
    }

    /// <summary>
    /// Builds an UpdateTourDto with default or custom values.
    /// </summary>
    public static UpdateTourDto BuildUpdateTourDto(
        string? identifier = null,
        string? name = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        decimal? basePrice = null,
        decimal? singleRoomSupplement = null,
        decimal? regularBikePrice = null,
        decimal? eBikePrice = null,
        int? minCustomers = null,
        int? maxCustomers = null,
        CurrencyDto? currency = null,
        string[]? includedServices = null)
    {
        var tourIdentifier = identifier ?? UniqueTourIdentifier();
        var start = startDate ?? DateTime.UtcNow.AddMonths(2);
        var end = endDate ?? start.AddDays(10);

        return new UpdateTourDto
        {
            Identifier = tourIdentifier,
            Name = name ?? $"Test Tour {tourIdentifier}",
            StartDate = start,
            EndDate = end,
            Price = basePrice ?? DefaultBaseTourPrice,
            SingleRoomSupplementPrice = singleRoomSupplement ?? DefaultSingleRoomSupplement,
            RegularBikePrice = regularBikePrice ?? DefaultRegularBikePrice,
            EBikePrice = eBikePrice ?? DefaultEBikePrice,
            MinCustomers = minCustomers ?? DefaultMinCustomers,
            MaxCustomers = maxCustomers ?? DefaultMaxCustomers,
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
                Occupation = "Engineer"
            },
            IdentificationInfo = new IdentificationInfoDto
            {
                NationalId = nationalId ?? UniqueNationalId(),
                IdNationality = "American"
            },
            ContactInfo = new ContactInfoDto
            {
                Email = email ?? UniqueEmail($"{first.ToUpperInvariant()}.{last.ToUpperInvariant()}"),
                Mobile = mobile ?? UniquePhone(),
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
                RoomType = roomType ?? RoomTypeDto.DoubleOccupancy,
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
            RoomType = roomType ?? RoomTypeDto.DoubleOccupancy,
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
            ReferenceNumber = referenceNumber ?? UniqueReferenceNumber(),
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
        decimal discountAmount = DefaultValidPercentageDiscount,
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
            RoomType = roomType ?? RoomTypeDto.DoubleOccupancy,
            PrincipalBikeType = principalBikeType ?? BikeTypeDto.Regular,
            CompanionCustomerId = companionCustomerId,
            CompanionBikeType = companionBikeType
        };
    }

    /// <summary>
    /// Builds an UpdateCustomerDto with default or custom values.
    /// </summary>
    public static UpdateCustomerDto BuildUpdateCustomerDto(
        string? firstName = null,
        string? lastName = null,
        string? email = null,
        string? mobile = null,
        string? nationalId = null,
        BikeTypeDto? bikeType = null,
        RoomTypeDto? roomType = null,
        BedTypeDto? bedType = null,
        string? occupation = null,
        string? street = null,
        string? complement = null,
        string? city = null,
        string? state = null,
        string? country = null,
        string? postalCode = null,
        decimal? weightKg = null,
        int? heightCentimeters = null,
        string? emergencyContactName = null,
        string? emergencyContactMobile = null,
        string? allergies = null,
        string? medicalAdditionalInfo = null,
        string? instagram = null,
        string? facebook = null)
    {
        var first = firstName ?? "Test";
        var last = lastName ?? "User";

        return new UpdateCustomerDto
        {
            PersonalInfo = new PersonalInfoDto
            {
                FirstName = first,
                LastName = last,
                BirthDate = new DateTime(1990, 1, 1).ToUniversalTime(),
                Gender = "Male",
                Nationality = "American",
                Occupation = occupation ?? "Engineer"
            },
            IdentificationInfo = new IdentificationInfoDto
            {
                NationalId = nationalId ?? UniqueNationalId(),
                IdNationality = "American"
            },
            ContactInfo = new ContactInfoDto
            {
                Email = email ?? UniqueEmail($"{first.ToUpperInvariant()}.{last.ToUpperInvariant()}"),
                Mobile = mobile ?? UniquePhone(),
                Instagram = instagram,
                Facebook = facebook
            },
            Address = new AddressDto
            {
                Street = street ?? "123 Main St",
                Complement = complement,
                Neighborhood = "Downtown",
                PostalCode = postalCode ?? "12345",
                City = city ?? "New York",
                State = state ?? "NY",
                Country = country ?? "USA"
            },
            PhysicalInfo = new PhysicalInfoDto
            {
                WeightKg = weightKg ?? 75.0m,
                HeightCentimeters = heightCentimeters ?? 180,
                BikeType = bikeType ?? BikeTypeDto.Regular
            },
            AccommodationPreferences = new AccommodationPreferencesDto
            {
                RoomType = roomType ?? RoomTypeDto.DoubleOccupancy,
                BedType = bedType ?? BedTypeDto.SingleBed,
                CompanionId = null
            },
            EmergencyContact = new EmergencyContactDto
            {
                Name = emergencyContactName ?? "Emergency Contact",
                Mobile = emergencyContactMobile ?? "+15559876543"
            },
            MedicalInfo = new MedicalInfoDto
            {
                Allergies = allergies,
                AdditionalInfo = medicalAdditionalInfo
            }
        };
    }

    private static string UniqueEmail(string prefix = "test")
    {
        var cleanPrefix = prefix.Replace(" ", string.Empty, StringComparison.Ordinal)
            .Replace("\t", string.Empty, StringComparison.Ordinal);
        var suffix = Guid.NewGuid().ToString("N")[..8];
        return $"{cleanPrefix}.{suffix}@example.com";
    }

    private static string UniqueNationalId(string prefix = "ID")
    {
        var suffix = Random.Shared.Next(10000000, 99999999);
        return $"{prefix}{suffix}";
    }

    private static string UniquePhone(string prefix = "+1555")
    {
        var suffix = Random.Shared.Next(1000000, 9999999);
        return $"{prefix}{suffix}";
    }

    private static string UniqueTourIdentifier(string prefix = "TOUR")
    {
        var suffix = Guid.NewGuid().ToString("N")[..6].ToUpperInvariant();
        return $"{prefix}{suffix}";
    }

    private static string UniqueReferenceNumber(string prefix = "REF")
    {
        var suffix = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        return $"{prefix}-{suffix}";
    }
}
