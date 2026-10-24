using ViajantesTurismo.Admin.Contracts.Application;
using ViajantesTurismo.Admin.Domain.Customers;
using ViajantesTurismo.Admin.Domain.Shared;

namespace ViajantesTurismo.Admin.Application.Mappings;

/// <summary>
/// Maps Customer-related DTOs to domain objects.
/// </summary>
public static class CustomerMapper
{
    /// <summary>
    /// Maps a <see cref="BikeType"/> to a <see cref="BikeTypeDto"/>.
    /// </summary>
    public static BikeTypeDto MapToBikeTypeDto(BikeType bikeType)
    {
        return bikeType switch
        {
            BikeType.None => BikeTypeDto.None,
            BikeType.Regular => BikeTypeDto.Regular,
            BikeType.EBike => BikeTypeDto.EBike,
            _ => throw new ArgumentOutOfRangeException(nameof(bikeType), bikeType, "Invalid bike type value.")
        };
    }

    /// <summary>
    /// Maps a <see cref="BedTypeDto"/> to a <see cref="BedType"/>.
    /// </summary>
    public static BedType MapToBedType(BedTypeDto bedTypeDto)
    {
        return bedTypeDto switch
        {
            BedTypeDto.SingleBed => BedType.SingleBed,
            BedTypeDto.DoubleBed => BedType.DoubleBed,
            _ => throw new ArgumentOutOfRangeException(nameof(bedTypeDto), bedTypeDto, "Invalid bed type value.")
        };
    }

    /// <summary>
    /// Maps an <see cref="AccommodationPreferencesDto"/> to an <see cref="AccommodationPreferencesDefinition"/>.
    /// </summary>
    public static AccommodationPreferencesDefinition MapToAccommodationPreferencesDefinition(AccommodationPreferencesDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        return new AccommodationPreferencesDefinition(
            BookingMapper.MapToRoomType(dto.RoomType),
            MapToBedType(dto.BedType),
            dto.CompanionId);
    }

    /// <summary>
    /// Maps a <see cref="PersonalInfoDto"/> to a <see cref="PersonalInfoDefinition"/>.
    /// </summary>
    public static PersonalInfoDefinition MapToPersonalInfoDefinition(PersonalInfoDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        return new PersonalInfoDefinition(dto.FirstName, dto.LastName, dto.Gender, dto.BirthDate, dto.Nationality,
            dto.Occupation);
    }

    /// <summary>
    /// Maps an <see cref="IdentificationInfoDto"/> to an <see cref="IdentificationInfoDefinition"/>.
    /// </summary>
    public static IdentificationInfoDefinition MapToIdentificationInfoDefinition(IdentificationInfoDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        return new IdentificationInfoDefinition(dto.NationalId, dto.IdNationality);
    }

    /// <summary>
    /// Maps a <see cref="ContactInfoDto"/> to a <see cref="ContactInfoDefinition"/>.
    /// </summary>
    public static ContactInfoDefinition MapToContactInfoDefinition(ContactInfoDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        return new ContactInfoDefinition(dto.Email, dto.Mobile, dto.Instagram, dto.Facebook);
    }

    /// <summary>
    /// Maps an <see cref="AddressDto"/> to an <see cref="AddressDefinition"/>.
    /// </summary>
    public static AddressDefinition MapToAddressDefinition(AddressDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        return new AddressDefinition(dto.Street, dto.Complement, dto.Neighborhood, dto.PostalCode, dto.City,
            dto.State, dto.Country);
    }

    /// <summary>
    /// Maps a <see cref="PhysicalInfoDto"/> to a <see cref="PhysicalInfoDefinition"/>.
    /// </summary>
    public static PhysicalInfoDefinition MapToPhysicalInfoDefinition(PhysicalInfoDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        return new PhysicalInfoDefinition(dto.WeightKg, dto.HeightCentimeters, BookingMapper.MapToBikeType(dto.BikeType));
    }

    /// <summary>
    /// Maps emergency contact and medical DTOs to a <see cref="CustomerHealthDefinition"/>.
    /// </summary>
    public static CustomerHealthDefinition MapToHealthDefinition(EmergencyContactDto emergencyContact,
        MedicalInfoDto medicalInfo)
    {
        ArgumentNullException.ThrowIfNull(emergencyContact);
        ArgumentNullException.ThrowIfNull(medicalInfo);

        return new CustomerHealthDefinition(emergencyContact.Name, emergencyContact.Mobile, medicalInfo.Allergies,
            medicalInfo.AdditionalInfo);
    }

    /// <summary>
    /// Maps an existing customer's current profile to a <see cref="CustomerDefinition"/>.
    /// </summary>
    public static CustomerDefinition MapToDefinition(Customer customer)
    {
        ArgumentNullException.ThrowIfNull(customer);

        var personal = customer.PersonalInfo;
        var address = customer.Address;
        return new CustomerDefinition(
            new PersonalInfoDefinition(personal.FirstName, personal.LastName, personal.Gender, personal.BirthDate,
                personal.Nationality, personal.Occupation),
            new IdentificationInfoDefinition(customer.IdentificationInfo.NationalId,
                customer.IdentificationInfo.IdNationality),
            new ContactInfoDefinition(customer.ContactInfo.Email, customer.ContactInfo.Mobile,
                customer.ContactInfo.Instagram, customer.ContactInfo.Facebook),
            new AddressDefinition(address.Street, address.Complement, address.Neighborhood, address.PostalCode,
                address.City, address.State, address.Country),
            new PhysicalInfoDefinition(customer.PhysicalInfo.WeightKg, customer.PhysicalInfo.HeightCentimeters,
                customer.PhysicalInfo.BikeType),
            new AccommodationPreferencesDefinition(customer.AccommodationPreferences.RoomType,
                customer.AccommodationPreferences.BedType, customer.AccommodationPreferences.CompanionId),
            new CustomerHealthDefinition(customer.EmergencyContact.Name, customer.EmergencyContact.Mobile,
                customer.MedicalInfo.Allergies, customer.MedicalInfo.AdditionalInfo));
    }
}
