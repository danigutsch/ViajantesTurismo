using System.ComponentModel.DataAnnotations;
using ViajantesTurismo.AdminApi.Contracts;

namespace ViajantesTurismo.Web.Models;

internal sealed class AccommodationPreferencesFormModel
{
    [Required(ErrorMessage = "Room type is required")]
    public RoomTypeDto? RoomType { get; set; }

    [Required(ErrorMessage = "Bed type is required")]
    public BedTypeDto? BedType { get; set; }

    public int? CompanionId { get; set; }

    public AccommodationPreferencesStepDto ToDto() => new()
    {
        RoomType = RoomType,
        BedType = BedType,
        CompanionId = CompanionId
    };

    public static AccommodationPreferencesFormModel FromDto(AccommodationPreferencesStepDto dto) => new()
    {
        RoomType = dto.RoomType,
        BedType = dto.BedType,
        CompanionId = dto.CompanionId
    };
}