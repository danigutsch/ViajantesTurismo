using System.ComponentModel.DataAnnotations;
using ViajantesTurismo.AdminApi.Contracts;

namespace ViajantesTurismo.Web.Models;

internal sealed class PhysicalInfoFormModel
{
    [Required(ErrorMessage = "Weight is required")]
    [Range(1, 500, ErrorMessage = "Weight must be between 1 and 500 kg")]
    public decimal? WeightKg { get; set; }

    [Required(ErrorMessage = "Height is required")]
    [Range(50, 300, ErrorMessage = "Height must be between 50 and 300 cm")]
    public int? HeightCentimeters { get; set; }

    [Required(ErrorMessage = "Bike type is required")]
    public BikeTypeDto? BikeType { get; set; }

    public PhysicalInfoDto ToDto() => new()
    {
        WeightKg = WeightKg!.Value,
        HeightCentimeters = HeightCentimeters!.Value,
        BikeType = BikeType!.Value
    };

    public static PhysicalInfoFormModel FromDto(PhysicalInfoDto dto) => new()
    {
        WeightKg = dto.WeightKg,
        HeightCentimeters = dto.HeightCentimeters,
        BikeType = dto.BikeType
    };
}
