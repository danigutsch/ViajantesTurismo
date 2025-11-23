using ViajantesTurismo.Admin.Contracts;

namespace ViajantesTurismo.Admin.Tests.Shared;

/// <summary>
/// Provides builder methods for creating DTO instances with default or custom values for tests.
/// </summary>
public static class DtoBuilders
{
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
        decimal? doubleRoomSupplementPrice = null,
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
            DoubleRoomSupplementPrice = doubleRoomSupplementPrice ?? 300.00m,
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
        decimal? remainingBalance = null)
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
            RemainingBalance = remainingBalance ?? (bookingTotalPrice - bookingAmountPaid)
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
                RoomType = RoomTypeDto.SingleRoom,
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
}
