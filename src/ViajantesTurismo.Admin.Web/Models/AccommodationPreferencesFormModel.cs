using System.ComponentModel.DataAnnotations;
using ViajantesTurismo.AdminApi.Contracts;

namespace ViajantesTurismo.Admin.Web.Models;

internal sealed class AccommodationPreferencesFormModel
{
    [Required(ErrorMessage = "Room type is required")]
    public RoomTypeDto? RoomType { get; set; }

    [Required(ErrorMessage = "Bed type is required")]
    public BedTypeDto? BedType { get; set; }

    public int? CompanionId { get; set; }

    public AccommodationPreferencesDto ToDto() => new()
    {
        RoomType = RoomType!.Value,
        BedType = BedType!.Value,
        CompanionId = CompanionId
    };

    public static AccommodationPreferencesFormModel FromDto(AccommodationPreferencesDto dto) => new()
    {
        RoomType = dto.RoomType,
        BedType = dto.BedType,
        CompanionId = dto.CompanionId
    };
}