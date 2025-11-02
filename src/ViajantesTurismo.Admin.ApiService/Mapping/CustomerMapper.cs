using ViajantesTurismo.Admin.Domain.Customers;
using ViajantesTurismo.AdminApi.Contracts;

namespace ViajantesTurismo.Admin.ApiService.Mapping;

/// <summary>
/// Maps Customer-related DTOs to domain objects.
/// </summary>
internal static class CustomerMapper
{
    /// <summary>
    /// Maps a <see cref="BikeTypeDto"/> to a <see cref="BikeType"/>.
    /// </summary>
    public static BikeType MapToBikeType(BikeTypeDto bikeTypeDto)
    {
        return bikeTypeDto switch
        {
            BikeTypeDto.None => BikeType.None,
            BikeTypeDto.Regular => BikeType.Regular,
            BikeTypeDto.EBike => BikeType.EBike,
            _ => throw new ArgumentOutOfRangeException(nameof(bikeTypeDto), bikeTypeDto, "Invalid bike type value.")
        };
    }

    /// <summary>
    /// Maps a <see cref="RoomTypeDto"/> to a <see cref="RoomType"/>.
    /// </summary>
    public static RoomType MapToRoomType(RoomTypeDto roomTypeDto)
    {
        return roomTypeDto switch
        {
            RoomTypeDto.SingleRoom => RoomType.SingleRoom,
            RoomTypeDto.DoubleRoom => RoomType.DoubleRoom,
            _ => throw new ArgumentOutOfRangeException(nameof(roomTypeDto), roomTypeDto, "Invalid room type value.")
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
        return new Address(
            dto.Street,
            dto.Complement,
            dto.Neighborhood!,
            dto.PostalCode,
            dto.City,
            dto.State,
            dto.Country);
    }

    /// <summary>
    /// Maps a <see cref="PhysicalInfoDto"/> to a <see cref="PhysicalInfo"/> domain object.
    /// </summary>
    public static PhysicalInfo MapToPhysicalInfo(PhysicalInfoDto dto)
    {
        return new PhysicalInfo(
            dto.WeightKg,
            dto.HeightCentimeters,
            MapToBikeType(dto.BikeType));
    }

    /// <summary>
    /// Maps an <see cref="AccommodationPreferencesDto"/> to an <see cref="AccommodationPreferences"/> domain object.
    /// </summary>
    public static AccommodationPreferences MapToAccommodationPreferences(AccommodationPreferencesDto dto)
    {
        return new AccommodationPreferences(
            MapToRoomType(dto.RoomType),
            MapToBedType(dto.BedType),
            dto.CompanionId);
    }

    /// <summary>
    /// Maps an <see cref="EmergencyContactDto"/> to an <see cref="EmergencyContact"/> domain object.
    /// </summary>
    public static EmergencyContact MapToEmergencyContact(EmergencyContactDto dto)
    {
        return new EmergencyContact(
            dto.Name,
            dto.Mobile);
    }

    /// <summary>
    /// Maps a <see cref="MedicalInfoDto"/> to a <see cref="MedicalInfo"/> domain object.
    /// </summary>
    public static MedicalInfo MapToMedicalInfo(MedicalInfoDto dto)
    {
        return new MedicalInfo(
            dto.Allergies,
            dto.AdditionalInfo);
    }
}
