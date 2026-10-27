using System.ComponentModel.DataAnnotations;
using ViajantesTurismo.Admin.Contracts.Application;

namespace ViajantesTurismo.Management.Web.Models;

/// <summary>
/// Form model for a customer accommodation preferences.
/// </summary>
internal sealed class AccommodationPreferencesFormModel
{
    /// <summary>
    /// Selected room type; required.
    /// </summary>
    [Required(ErrorMessage = "Room type is required")]
    public RoomTypeDto? RoomType
    {
        get; set;
    }

    /// <summary>
    /// Selected bed type; required.
    /// </summary>
    [Required(ErrorMessage = "Bed type is required")]
    public BedTypeDto? BedType
    {
        get; set;
    }

    /// <summary>
    /// Identifier of the companion sharing the room, or null when none.
    /// </summary>
    public Guid? CompanionId
    {
        get; set;
    }

    /// <summary>
    /// Converts the validated form to an accommodation preferences DTO.
    /// </summary>
    public AccommodationPreferencesDto ToDto() => new()
    {
        RoomType = RoomType!.Value,
        BedType = BedType!.Value,
        CompanionId = CompanionId
    };

    /// <summary>
    /// Creates a form model from an accommodation preferences DTO.
    /// </summary>
    public static AccommodationPreferencesFormModel FromDto(AccommodationPreferencesDto dto) => new()
    {
        RoomType = dto.RoomType,
        BedType = dto.BedType,
        CompanionId = dto.CompanionId
    };
}
