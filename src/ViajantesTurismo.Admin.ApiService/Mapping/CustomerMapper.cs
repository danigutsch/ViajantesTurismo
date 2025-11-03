using ViajantesTurismo.Admin.Domain.Customers;
using ViajantesTurismo.AdminApi.Contracts;

namespace ViajantesTurismo.Admin.ApiService.Mapping;

/// <summary>
/// Maps Customer-related DTOs to domain objects.
/// </summary>
internal static class CustomerMapper
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
    /// Maps an <see cref="AddressDto"/> to an <see cref="Address"/> domain object.
    /// </summary>
    public static Address MapToAddress(AddressDto dto)
    {
        return Address.Create(
            dto.Street,
            dto.Complement,
            dto.Neighborhood,
            dto.PostalCode,
            dto.City,
            dto.State,
            dto.Country).Value;
    }

    /// <summary>
    /// Maps a <see cref="PhysicalInfoDto"/> to a <see cref="PhysicalInfo"/> domain object.
    /// </summary>
    public static PhysicalInfo MapToPhysicalInfo(PhysicalInfoDto dto)
    {
        return PhysicalInfo.Create(
            dto.WeightKg,
            dto.HeightCentimeters,
            BookingMapper.MapToBikeType(dto.BikeType)).Value;
    }

    /// <summary>
    /// Maps an <see cref="AccommodationPreferencesDto"/> to an <see cref="AccommodationPreferences"/> domain object.
    /// </summary>
    public static AccommodationPreferences MapToAccommodationPreferences(AccommodationPreferencesDto dto)
    {
        return AccommodationPreferences.Create(
            BookingMapper.MapToRoomType(dto.RoomType),
            MapToBedType(dto.BedType),
            dto.CompanionId).Value;
    }

    /// <summary>
    /// Maps an <see cref="EmergencyContactDto"/> to an <see cref="EmergencyContact"/> domain object.
    /// </summary>
    public static EmergencyContact MapToEmergencyContact(EmergencyContactDto dto)
    {
        return EmergencyContact.Create(
            dto.Name,
            dto.Mobile).Value;
    }

    /// <summary>
    /// Maps a <see cref="MedicalInfoDto"/> to a <see cref="MedicalInfo"/> domain object.
    /// </summary>
    public static MedicalInfo MapToMedicalInfo(MedicalInfoDto dto)
    {
        return MedicalInfo.Create(
            dto.Allergies,
            dto.AdditionalInfo).Value;
    }
}